# Explore Team Members API
$debugPort = 9222
$wsUri = $null

Write-Host "=== Exploring Team Members API ===" -ForegroundColor Cyan

# Get WebSocket URL
try {
    $response = Invoke-WebRequest -Uri "http://localhost:$debugPort/json" -UseBasicParsing
    $targets = $response.Content | ConvertFrom-Json
    $target = $targets | Where-Object { $_.type -eq "page" } | Select-Object -First 1
    if ($target) {
        $wsUri = $target.webSocketDebuggerUrl
        Write-Host "WebSocket URL: $wsUri" -ForegroundColor Green
    }
} catch {
    Write-Host "Cannot connect to WangShangLiao. Make sure it is running with debug port 9222" -ForegroundColor Red
    exit
}

# Connect via WebSocket
Add-Type -AssemblyName System.Net.WebSockets
$ws = New-Object System.Net.WebSockets.ClientWebSocket
$cts = New-Object System.Threading.CancellationTokenSource

try {
    $ws.ConnectAsync([Uri]$wsUri, $cts.Token).Wait()
    Write-Host "WebSocket connected" -ForegroundColor Green
} catch {
    Write-Host "WebSocket connection failed: $_" -ForegroundColor Red
    exit
}

function Send-CDPCommand {
    param([string]$method, [hashtable]$params = @{})
    
    $id = Get-Random -Minimum 1 -Maximum 99999
    $cmd = @{
        id = $id
        method = $method
        params = $params
    } | ConvertTo-Json -Depth 10 -Compress
    
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($cmd)
    $segment = New-Object System.ArraySegment[byte] -ArgumentList @(,$bytes)
    $ws.SendAsync($segment, [System.Net.WebSockets.WebSocketMessageType]::Text, $true, $cts.Token).Wait()
    
    $buffer = New-Object byte[] 65536
    $result = ""
    do {
        $segment = New-Object System.ArraySegment[byte] -ArgumentList @(,$buffer)
        $received = $ws.ReceiveAsync($segment, $cts.Token).Result
        $result += [System.Text.Encoding]::UTF8.GetString($buffer, 0, $received.Count)
    } while (-not $received.EndOfMessage)
    
    return $result | ConvertFrom-Json
}

# Step 1: Explore NIM SDK APIs
Write-Host "`n=== Step 1: NIM SDK Team APIs ===" -ForegroundColor Yellow

$script1 = @'
(function() {
    var result = {
        nimAvailable: false,
        teamMethods: [],
        sampleGroup: null
    };
    
    try {
        if (window.nim) {
            result.nimAvailable = true;
            var methods = Object.keys(window.nim).filter(function(k) {
                return typeof window.nim[k] === 'function';
            });
            result.teamMethods = methods.filter(function(m) {
                return m.toLowerCase().includes('team') || 
                       m.toLowerCase().includes('member') ||
                       m.toLowerCase().includes('group');
            });
        }
        
        var app = document.querySelector('#app');
        if (app && app.__vue__ && app.__vue__.$store) {
            var store = app.__vue__.$store;
            if (store.state.appStore && store.state.appStore.groupList) {
                var gl = store.state.appStore.groupList;
                var firstGroup = (gl.owner && gl.owner[0]) || (gl.member && gl.member[0]);
                if (firstGroup) {
                    result.sampleGroup = {
                        groupId: firstGroup.groupId,
                        groupAccount: firstGroup.groupAccount,
                        groupName: firstGroup.groupName,
                        nimGroupId: firstGroup.nimGroupId,
                        memberCount: firstGroup.groupMemberNum || firstGroup.memberCount
                    };
                }
            }
        }
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})();
'@

$response1 = Send-CDPCommand -method "Runtime.evaluate" -params @{
    expression = $script1
    returnByValue = $true
}

if ($response1.result -and $response1.result.result -and $response1.result.result.value) {
    $data1 = $response1.result.result.value | ConvertFrom-Json
    
    Write-Host "NIM SDK Available: $($data1.nimAvailable)" -ForegroundColor Green
    
    if ($data1.teamMethods -and $data1.teamMethods.Count -gt 0) {
        Write-Host "`nTeam/Member related methods:" -ForegroundColor Cyan
        $data1.teamMethods | ForEach-Object { Write-Host "  - $_" -ForegroundColor White }
    }
    
    if ($data1.sampleGroup) {
        Write-Host "`nSample Group:" -ForegroundColor Cyan
        Write-Host "  GroupID: $($data1.sampleGroup.groupId)"
        Write-Host "  Account: $($data1.sampleGroup.groupAccount)"
        Write-Host "  Name: $($data1.sampleGroup.groupName)"
        Write-Host "  NimGroupId: $($data1.sampleGroup.nimGroupId)"
        Write-Host "  MemberCount: $($data1.sampleGroup.memberCount)"
    }
}

# Step 2: Get team members
Write-Host "`n=== Step 2: Get Team Members ===" -ForegroundColor Yellow

$script2 = @'
(async function() {
    var result = {
        success: false,
        members: [],
        methodsTried: [],
        teamId: null
    };
    
    try {
        var app = document.querySelector('#app');
        if (app && app.__vue__ && app.__vue__.$store) {
            var store = app.__vue__.$store;
            
            // Get team ID
            if (store.state.sessionStore && store.state.sessionStore.currentSession) {
                var cs = store.state.sessionStore.currentSession;
                result.teamId = cs.to || cs.teamId;
            }
            
            if (!result.teamId && store.state.appStore && store.state.appStore.groupList) {
                var gl = store.state.appStore.groupList;
                var firstGroup = (gl.owner && gl.owner[0]) || (gl.member && gl.member[0]);
                if (firstGroup) {
                    result.teamId = firstGroup.nimGroupId || firstGroup.groupId;
                }
            }
        }
        
        if (!result.teamId) {
            result.error = 'No team ID found';
            return JSON.stringify(result);
        }
        
        // Method 1: getTeamMembers
        if (window.nim && typeof window.nim.getTeamMembers === 'function') {
            result.methodsTried.push('getTeamMembers');
            
            var members = await new Promise(function(resolve) {
                window.nim.getTeamMembers({
                    teamId: result.teamId,
                    done: function(err, obj) {
                        if (err) resolve({ error: err.message });
                        else resolve({ members: obj.members || obj });
                    }
                });
                setTimeout(function() { resolve({ error: 'timeout' }); }, 10000);
            });
            
            if (members.members && Array.isArray(members.members)) {
                result.success = true;
                result.members = members.members.map(function(m) {
                    return {
                        account: m.account || m.id,
                        nick: m.nickInTeam || m.nick || m.alias,
                        type: m.type,
                        mute: m.mute,
                        joinTime: m.joinTime
                    };
                });
            }
        }
        
        // Method 2: getTeam
        if (window.nim && typeof window.nim.getTeam === 'function') {
            result.methodsTried.push('getTeam');
            
            var team = await new Promise(function(resolve) {
                window.nim.getTeam({
                    teamId: result.teamId,
                    done: function(err, obj) {
                        if (err) resolve({ error: err.message });
                        else resolve({ team: obj });
                    }
                });
                setTimeout(function() { resolve({ error: 'timeout' }); }, 5000);
            });
            
            if (team.team) {
                result.teamInfo = {
                    teamId: team.team.teamId,
                    name: team.team.name,
                    memberNum: team.team.memberNum,
                    owner: team.team.owner,
                    keys: Object.keys(team.team)
                };
            }
        }
        
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})();
'@

$response2 = Send-CDPCommand -method "Runtime.evaluate" -params @{
    expression = $script2
    returnByValue = $true
    awaitPromise = $true
}

if ($response2.result -and $response2.result.result -and $response2.result.result.value) {
    $data2 = $response2.result.result.value | ConvertFrom-Json
    
    Write-Host "TeamId: $($data2.teamId)" -ForegroundColor Cyan
    Write-Host "Methods tried: $($data2.methodsTried -join ', ')" -ForegroundColor White
    Write-Host "Success: $($data2.success)" -ForegroundColor $(if($data2.success){"Green"}else{"Red"})
    
    if ($data2.members -and $data2.members.Count -gt 0) {
        Write-Host "`nMembers (total $($data2.members.Count)):" -ForegroundColor Cyan
        $data2.members | Select-Object -First 30 | ForEach-Object {
            Write-Host "  $($_.account) - $($_.nick) [type=$($_.type)]" -ForegroundColor White
        }
    }
    
    if ($data2.teamInfo) {
        Write-Host "`nTeam Info:" -ForegroundColor Cyan
        Write-Host "  Name: $($data2.teamInfo.name)"
        Write-Host "  MemberNum: $($data2.teamInfo.memberNum)"
        Write-Host "  Owner: $($data2.teamInfo.owner)"
    }
    
    if ($data2.error) {
        Write-Host "Error: $($data2.error)" -ForegroundColor Red
    }
    
    # Save full result
    $data2 | ConvertTo-Json -Depth 10 | Out-File "team_members_result.json" -Encoding UTF8
    Write-Host "`nFull result saved to team_members_result.json" -ForegroundColor Green
}

# Cleanup
$ws.CloseAsync([System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure, "Done", $cts.Token).Wait()
Write-Host "`n=== Exploration Complete ===" -ForegroundColor Cyan
