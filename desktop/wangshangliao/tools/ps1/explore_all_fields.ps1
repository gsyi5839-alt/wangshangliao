# Comprehensive WangShangLiao Field Explorer
# Explores all available NIM SDK methods, Vue stores, and data structures

$debugPort = 9222
$wsUri = $null

Write-Host "=== WangShangLiao Complete Field Explorer ===" -ForegroundColor Cyan
Write-Host "Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray

# Get WebSocket URL
try {
    $response = Invoke-WebRequest -Uri "http://localhost:$debugPort/json" -UseBasicParsing -TimeoutSec 5
    $targets = $response.Content | ConvertFrom-Json
    $target = $targets | Where-Object { $_.type -eq "page" } | Select-Object -First 1
    if ($target) {
        $wsUri = $target.webSocketDebuggerUrl
        Write-Host "Connected to WangShangLiao" -ForegroundColor Green
    }
} catch {
    Write-Host "Cannot connect to WangShangLiao at port $debugPort" -ForegroundColor Red
    Write-Host "Please ensure WangShangLiao is running with --remote-debugging-port=9222" -ForegroundColor Yellow
    exit 1
}

# Connect via WebSocket
Add-Type -AssemblyName System.Net.WebSockets
$ws = New-Object System.Net.WebSockets.ClientWebSocket
$cts = New-Object System.Threading.CancellationTokenSource

try {
    $ws.ConnectAsync([Uri]$wsUri, $cts.Token).Wait()
} catch {
    Write-Host "WebSocket connection failed: $_" -ForegroundColor Red
    exit 1
}

function Send-CDP {
    param([string]$method, [hashtable]$params = @{})
    
    $id = Get-Random -Minimum 1 -Maximum 99999
    $cmd = @{ id = $id; method = $method; params = $params } | ConvertTo-Json -Depth 10 -Compress
    
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($cmd)
    $segment = New-Object System.ArraySegment[byte] -ArgumentList @(,$bytes)
    $ws.SendAsync($segment, [System.Net.WebSockets.WebSocketMessageType]::Text, $true, $cts.Token).Wait()
    
    $buffer = New-Object byte[] 131072
    $result = ""
    do {
        $segment = New-Object System.ArraySegment[byte] -ArgumentList @(,$buffer)
        $received = $ws.ReceiveAsync($segment, $cts.Token).Result
        $result += [System.Text.Encoding]::UTF8.GetString($buffer, 0, $received.Count)
    } while (-not $received.EndOfMessage)
    
    return $result | ConvertFrom-Json
}

$allResults = @{}

# ============================================
# PART 1: NIM SDK Complete Method List
# ============================================
Write-Host "`n=== PART 1: NIM SDK Methods ===" -ForegroundColor Yellow

$nimScript = @'
(function() {
    var result = {
        available: false,
        version: null,
        allMethods: [],
        categorized: {}
    };
    
    if (!window.nim) return JSON.stringify(result);
    
    result.available = true;
    result.version = window.nim.version || 'unknown';
    
    var methods = Object.keys(window.nim).filter(function(k) {
        return typeof window.nim[k] === 'function';
    }).sort();
    
    result.allMethods = methods;
    
    // Categorize methods
    var categories = {
        team: [], member: [], group: [], msg: [], session: [],
        friend: [], user: [], file: [], system: [], other: []
    };
    
    methods.forEach(function(m) {
        var lower = m.toLowerCase();
        if (lower.includes('team')) categories.team.push(m);
        else if (lower.includes('member')) categories.member.push(m);
        else if (lower.includes('group')) categories.group.push(m);
        else if (lower.includes('msg') || lower.includes('message')) categories.msg.push(m);
        else if (lower.includes('session')) categories.session.push(m);
        else if (lower.includes('friend')) categories.friend.push(m);
        else if (lower.includes('user') || lower.includes('account')) categories.user.push(m);
        else if (lower.includes('file') || lower.includes('image') || lower.includes('upload')) categories.file.push(m);
        else if (lower.includes('system') || lower.includes('sys')) categories.system.push(m);
        else categories.other.push(m);
    });
    
    result.categorized = categories;
    return JSON.stringify(result);
})();
'@

$nimResp = Send-CDP -method "Runtime.evaluate" -params @{ expression = $nimScript; returnByValue = $true }
if ($nimResp.result.result.value) {
    $nimData = $nimResp.result.result.value | ConvertFrom-Json
    $allResults.nim = $nimData
    
    Write-Host "NIM SDK Available: $($nimData.available)" -ForegroundColor $(if($nimData.available){"Green"}else{"Red"})
    Write-Host "Version: $($nimData.version)" -ForegroundColor Cyan
    Write-Host "Total Methods: $($nimData.allMethods.Count)" -ForegroundColor Cyan
    
    foreach ($cat in @("team", "member", "group", "msg", "session", "friend", "user", "file", "system")) {
        $methods = $nimData.categorized.$cat
        if ($methods -and $methods.Count -gt 0) {
            Write-Host "`n  [$cat] ($($methods.Count) methods):" -ForegroundColor Yellow
            $methods | ForEach-Object { Write-Host "    - $_" -ForegroundColor White }
        }
    }
}

# ============================================
# PART 2: Vue Store Complete Structure
# ============================================
Write-Host "`n=== PART 2: Vue Store Structure ===" -ForegroundColor Yellow

$vueScript = @'
(function() {
    var result = {
        hasVue: false,
        hasStore: false,
        storeModules: [],
        moduleDetails: {}
    };
    
    var app = document.querySelector('#app');
    if (!app || !app.__vue__) return JSON.stringify(result);
    
    result.hasVue = true;
    var vue = app.__vue__;
    
    if (!vue.$store) return JSON.stringify(result);
    result.hasStore = true;
    
    var state = vue.$store.state || {};
    result.storeModules = Object.keys(state);
    
    // Get details for each module
    result.storeModules.forEach(function(mod) {
        var modState = state[mod];
        if (modState && typeof modState === 'object') {
            result.moduleDetails[mod] = {
                keys: Object.keys(modState),
                types: {}
            };
            Object.keys(modState).forEach(function(k) {
                var val = modState[k];
                var type = Array.isArray(val) ? 'array[' + val.length + ']' : typeof val;
                if (type === 'object' && val !== null) {
                    type = 'object{' + Object.keys(val).slice(0,5).join(',') + (Object.keys(val).length > 5 ? '...' : '') + '}';
                }
                result.moduleDetails[mod].types[k] = type;
            });
        }
    });
    
    return JSON.stringify(result);
})();
'@

$vueResp = Send-CDP -method "Runtime.evaluate" -params @{ expression = $vueScript; returnByValue = $true }
if ($vueResp.result.result.value) {
    $vueData = $vueResp.result.result.value | ConvertFrom-Json
    $allResults.vue = $vueData
    
    Write-Host "Vue Available: $($vueData.hasVue)" -ForegroundColor $(if($vueData.hasVue){"Green"}else{"Red"})
    Write-Host "Store Available: $($vueData.hasStore)" -ForegroundColor $(if($vueData.hasStore){"Green"}else{"Red"})
    
    if ($vueData.storeModules) {
        Write-Host "`nStore Modules ($($vueData.storeModules.Count)):" -ForegroundColor Cyan
        foreach ($mod in $vueData.storeModules) {
            $details = $vueData.moduleDetails.$mod
            if ($details) {
                Write-Host "`n  [$mod]:" -ForegroundColor Yellow
                foreach ($key in $details.keys) {
                    $type = $details.types.$key
                    Write-Host "    - $key : $type" -ForegroundColor White
                }
            }
        }
    }
}

# ============================================
# PART 3: Current Session Details
# ============================================
Write-Host "`n=== PART 3: Current Session Details ===" -ForegroundColor Yellow

$sessionScript = @'
(function() {
    var result = {
        hasSession: false,
        sessionFields: [],
        sessionData: null
    };
    
    var app = document.querySelector('#app');
    if (!app || !app.__vue__ || !app.__vue__.$store) return JSON.stringify(result);
    
    var store = app.__vue__.$store.state;
    
    // Try sessionStore
    if (store.sessionStore && store.sessionStore.currentSession) {
        result.hasSession = true;
        var cs = store.sessionStore.currentSession;
        result.sessionFields = Object.keys(cs);
        result.sessionData = {};
        
        Object.keys(cs).forEach(function(k) {
            var val = cs[k];
            if (typeof val === 'string' || typeof val === 'number' || typeof val === 'boolean') {
                result.sessionData[k] = val;
            } else if (Array.isArray(val)) {
                result.sessionData[k] = '[array:' + val.length + ']';
            } else if (val && typeof val === 'object') {
                result.sessionData[k] = '{object:' + Object.keys(val).join(',') + '}';
            } else {
                result.sessionData[k] = String(val);
            }
        });
    }
    
    return JSON.stringify(result);
})();
'@

$sessionResp = Send-CDP -method "Runtime.evaluate" -params @{ expression = $sessionScript; returnByValue = $true }
if ($sessionResp.result.result.value) {
    $sessionData = $sessionResp.result.result.value | ConvertFrom-Json
    $allResults.session = $sessionData
    
    Write-Host "Has Current Session: $($sessionData.hasSession)" -ForegroundColor $(if($sessionData.hasSession){"Green"}else{"Red"})
    
    if ($sessionData.sessionData) {
        Write-Host "`nSession Fields:" -ForegroundColor Cyan
        $sessionData.sessionData.PSObject.Properties | ForEach-Object {
            Write-Host "  $($_.Name) = $($_.Value)" -ForegroundColor White
        }
    }
}

# ============================================
# PART 4: Group/Team Details
# ============================================
Write-Host "`n=== PART 4: Group/Team Details ===" -ForegroundColor Yellow

$groupScript = @'
(function() {
    var result = {
        groups: [],
        currentTeamId: null,
        teamInfo: null,
        teamFields: []
    };
    
    var app = document.querySelector('#app');
    if (!app || !app.__vue__ || !app.__vue__.$store) return JSON.stringify(result);
    
    var store = app.__vue__.$store.state;
    
    // Get groups from appStore
    if (store.appStore && store.appStore.groupList) {
        var gl = store.appStore.groupList;
        
        if (gl.owner) {
            gl.owner.forEach(function(g) {
                result.groups.push({
                    type: 'owner',
                    groupId: g.groupId,
                    groupAccount: g.groupAccount,
                    groupName: g.groupName,
                    nimGroupId: g.nimGroupId,
                    memberCount: g.groupMemberNum || g.memberCount,
                    allFields: Object.keys(g)
                });
            });
        }
        
        if (gl.member) {
            gl.member.forEach(function(g) {
                result.groups.push({
                    type: 'member',
                    groupId: g.groupId,
                    groupAccount: g.groupAccount,
                    groupName: g.groupName,
                    nimGroupId: g.nimGroupId,
                    memberCount: g.groupMemberNum || g.memberCount,
                    allFields: Object.keys(g)
                });
            });
        }
    }
    
    // Get current team ID
    if (store.sessionStore && store.sessionStore.currentSession) {
        result.currentTeamId = store.sessionStore.currentSession.to || store.sessionStore.currentSession.teamId;
    }
    
    return JSON.stringify(result);
})();
'@

$groupResp = Send-CDP -method "Runtime.evaluate" -params @{ expression = $groupScript; returnByValue = $true }
if ($groupResp.result.result.value) {
    $groupData = $groupResp.result.result.value | ConvertFrom-Json
    $allResults.groups = $groupData
    
    Write-Host "Total Groups: $($groupData.groups.Count)" -ForegroundColor Cyan
    Write-Host "Current Team ID: $($groupData.currentTeamId)" -ForegroundColor Cyan
    
    if ($groupData.groups.Count -gt 0) {
        Write-Host "`nGroups:" -ForegroundColor Yellow
        foreach ($g in $groupData.groups) {
            Write-Host "  [$($g.type)] $($g.groupName)" -ForegroundColor White
            Write-Host "    GroupID: $($g.groupId)" -ForegroundColor Gray
            Write-Host "    Account: $($g.groupAccount)" -ForegroundColor Gray
            Write-Host "    NimID: $($g.nimGroupId)" -ForegroundColor Gray
            Write-Host "    Members: $($g.memberCount)" -ForegroundColor Gray
            Write-Host "    Fields: $($g.allFields -join ', ')" -ForegroundColor DarkGray
        }
    }
}

# ============================================
# PART 5: Message Structure
# ============================================
Write-Host "`n=== PART 5: Message Structure ===" -ForegroundColor Yellow

$msgScript = @'
(function() {
    var result = {
        msgFields: [],
        sampleMsg: null,
        msgTypes: []
    };
    
    var app = document.querySelector('#app');
    if (!app || !app.__vue__ || !app.__vue__.$store) return JSON.stringify(result);
    
    var store = app.__vue__.$store.state;
    
    // Try to find messages in messageStore or similar
    var messages = null;
    
    if (store.messageStore && store.messageStore.messages) {
        var msgStore = store.messageStore.messages;
        var keys = Object.keys(msgStore);
        if (keys.length > 0) {
            var firstKey = keys[0];
            var msgArray = msgStore[firstKey];
            if (Array.isArray(msgArray) && msgArray.length > 0) {
                messages = msgArray;
            }
        }
    }
    
    // Try sessionStore messages
    if (!messages && store.sessionStore) {
        if (store.sessionStore.currentMessages && Array.isArray(store.sessionStore.currentMessages)) {
            messages = store.sessionStore.currentMessages;
        }
    }
    
    if (messages && messages.length > 0) {
        var sample = messages[messages.length - 1];
        result.msgFields = Object.keys(sample);
        
        // Get safe sample (no sensitive data)
        result.sampleMsg = {};
        Object.keys(sample).forEach(function(k) {
            var val = sample[k];
            if (k === 'text' || k === 'content') {
                result.sampleMsg[k] = '[content hidden]';
            } else if (typeof val === 'string') {
                result.sampleMsg[k] = val.length > 50 ? val.substring(0, 50) + '...' : val;
            } else if (typeof val === 'number' || typeof val === 'boolean') {
                result.sampleMsg[k] = val;
            } else if (Array.isArray(val)) {
                result.sampleMsg[k] = '[array:' + val.length + ']';
            } else if (val && typeof val === 'object') {
                result.sampleMsg[k] = '{' + Object.keys(val).slice(0,3).join(',') + '}';
            }
        });
        
        // Collect unique message types
        var types = {};
        messages.forEach(function(m) {
            if (m.type) types[m.type] = true;
        });
        result.msgTypes = Object.keys(types);
    }
    
    return JSON.stringify(result);
})();
'@

$msgResp = Send-CDP -method "Runtime.evaluate" -params @{ expression = $msgScript; returnByValue = $true }
if ($msgResp.result.result.value) {
    $msgData = $msgResp.result.result.value | ConvertFrom-Json
    $allResults.message = $msgData
    
    Write-Host "Message Fields Found: $($msgData.msgFields.Count)" -ForegroundColor Cyan
    
    if ($msgData.msgFields.Count -gt 0) {
        Write-Host "`nMessage Fields:" -ForegroundColor Yellow
        $msgData.msgFields | ForEach-Object { Write-Host "  - $_" -ForegroundColor White }
    }
    
    if ($msgData.msgTypes.Count -gt 0) {
        Write-Host "`nMessage Types: $($msgData.msgTypes -join ', ')" -ForegroundColor Cyan
    }
    
    if ($msgData.sampleMsg) {
        Write-Host "`nSample Message Structure:" -ForegroundColor Yellow
        $msgData.sampleMsg.PSObject.Properties | ForEach-Object {
            Write-Host "  $($_.Name) = $($_.Value)" -ForegroundColor White
        }
    }
}

# ============================================
# PART 6: Team Members (NIM SDK)
# ============================================
Write-Host "`n=== PART 6: Team Members via NIM SDK ===" -ForegroundColor Yellow

$memberScript = @'
(async function() {
    var result = {
        success: false,
        teamId: null,
        memberCount: 0,
        memberFields: [],
        sampleMember: null,
        memberTypes: []
    };
    
    // Get team ID
    var app = document.querySelector('#app');
    if (app && app.__vue__ && app.__vue__.$store) {
        var store = app.__vue__.$store.state;
        if (store.sessionStore && store.sessionStore.currentSession) {
            result.teamId = store.sessionStore.currentSession.to;
        }
        if (!result.teamId && store.appStore && store.appStore.groupList) {
            var gl = store.appStore.groupList;
            var first = (gl.owner && gl.owner[0]) || (gl.member && gl.member[0]);
            if (first) result.teamId = first.nimGroupId || first.groupId;
        }
    }
    
    if (!result.teamId || !window.nim) return JSON.stringify(result);
    
    // Get members via NIM SDK
    if (typeof window.nim.getTeamMembers === 'function') {
        var members = await new Promise(function(resolve) {
            window.nim.getTeamMembers({
                teamId: result.teamId,
                done: function(err, obj) {
                    if (err) resolve([]);
                    else resolve(obj.members || obj || []);
                }
            });
            setTimeout(function() { resolve([]); }, 10000);
        });
        
        if (members.length > 0) {
            result.success = true;
            result.memberCount = members.length;
            result.memberFields = Object.keys(members[0]);
            
            // Safe sample member
            var sample = members[0];
            result.sampleMember = {};
            Object.keys(sample).forEach(function(k) {
                var val = sample[k];
                if (typeof val === 'string') {
                    result.sampleMember[k] = val.length > 30 ? val.substring(0,30) + '...' : val;
                } else if (typeof val === 'number' || typeof val === 'boolean') {
                    result.sampleMember[k] = val;
                } else if (val && typeof val === 'object') {
                    result.sampleMember[k] = '{object}';
                }
            });
            
            // Collect member types
            var types = {};
            members.forEach(function(m) {
                if (m.type !== undefined) types[m.type] = true;
            });
            result.memberTypes = Object.keys(types);
        }
    }
    
    return JSON.stringify(result);
})();
'@

$memberResp = Send-CDP -method "Runtime.evaluate" -params @{ expression = $memberScript; returnByValue = $true; awaitPromise = $true }
if ($memberResp.result.result.value) {
    $memberData = $memberResp.result.result.value | ConvertFrom-Json
    $allResults.members = $memberData
    
    Write-Host "Team ID: $($memberData.teamId)" -ForegroundColor Cyan
    Write-Host "Success: $($memberData.success)" -ForegroundColor $(if($memberData.success){"Green"}else{"Red"})
    Write-Host "Member Count: $($memberData.memberCount)" -ForegroundColor Cyan
    
    if ($memberData.memberFields.Count -gt 0) {
        Write-Host "`nMember Fields:" -ForegroundColor Yellow
        $memberData.memberFields | ForEach-Object { Write-Host "  - $_" -ForegroundColor White }
    }
    
    if ($memberData.memberTypes.Count -gt 0) {
        Write-Host "`nMember Types: $($memberData.memberTypes -join ', ')" -ForegroundColor Cyan
    }
    
    if ($memberData.sampleMember) {
        Write-Host "`nSample Member:" -ForegroundColor Yellow
        $memberData.sampleMember.PSObject.Properties | ForEach-Object {
            Write-Host "  $($_.Name) = $($_.Value)" -ForegroundColor White
        }
    }
}

# ============================================
# PART 7: User/Contact Fields
# ============================================
Write-Host "`n=== PART 7: User/Contact Fields ===" -ForegroundColor Yellow

$userScript = @'
(function() {
    var result = {
        currentUser: null,
        userFields: [],
        friendFields: []
    };
    
    var app = document.querySelector('#app');
    if (!app || !app.__vue__ || !app.__vue__.$store) return JSON.stringify(result);
    
    var store = app.__vue__.$store.state;
    
    // Get current user info
    if (store.appStore && store.appStore.userInfo) {
        var user = store.appStore.userInfo;
        result.userFields = Object.keys(user);
        result.currentUser = {};
        Object.keys(user).forEach(function(k) {
            var val = user[k];
            if (typeof val === 'string') {
                result.currentUser[k] = val.length > 30 ? val.substring(0,30) + '...' : val;
            } else if (typeof val === 'number' || typeof val === 'boolean') {
                result.currentUser[k] = val;
            }
        });
    }
    
    // Get friend fields
    if (store.friendStore && store.friendStore.friends) {
        var friends = store.friendStore.friends;
        if (Array.isArray(friends) && friends.length > 0) {
            result.friendFields = Object.keys(friends[0]);
        } else if (typeof friends === 'object') {
            var keys = Object.keys(friends);
            if (keys.length > 0) {
                var first = friends[keys[0]];
                if (first) result.friendFields = Object.keys(first);
            }
        }
    }
    
    return JSON.stringify(result);
})();
'@

$userResp = Send-CDP -method "Runtime.evaluate" -params @{ expression = $userScript; returnByValue = $true }
if ($userResp.result.result.value) {
    $userData = $userResp.result.result.value | ConvertFrom-Json
    $allResults.user = $userData
    
    if ($userData.userFields.Count -gt 0) {
        Write-Host "`nUser Fields:" -ForegroundColor Yellow
        $userData.userFields | ForEach-Object { Write-Host "  - $_" -ForegroundColor White }
    }
    
    if ($userData.currentUser) {
        Write-Host "`nCurrent User:" -ForegroundColor Cyan
        $userData.currentUser.PSObject.Properties | ForEach-Object {
            Write-Host "  $($_.Name) = $($_.Value)" -ForegroundColor White
        }
    }
    
    if ($userData.friendFields.Count -gt 0) {
        Write-Host "`nFriend Fields:" -ForegroundColor Yellow
        $userData.friendFields | ForEach-Object { Write-Host "  - $_" -ForegroundColor White }
    }
}

# ============================================
# Save Results
# ============================================
Write-Host "`n=== Saving Results ===" -ForegroundColor Yellow

$outputFile = "wangshangliao_fields_$(Get-Date -Format 'yyyyMMdd_HHmmss').json"
$allResults | ConvertTo-Json -Depth 10 | Out-File $outputFile -Encoding UTF8
Write-Host "Full results saved to: $outputFile" -ForegroundColor Green

# Cleanup
$ws.CloseAsync([System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure, "Done", $cts.Token).Wait()
Write-Host "`n=== Exploration Complete ===" -ForegroundColor Cyan

