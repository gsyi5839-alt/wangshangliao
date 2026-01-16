# Deep WangShangLiao Field Explorer - Using HTTP CDP
# Explores all available fields without WebSocket dependency

$debugPort = 9222

Write-Host "=== WangShangLiao Deep Field Explorer ===" -ForegroundColor Cyan
Write-Host "Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray

# Get target info
try {
    $targets = Invoke-RestMethod -Uri "http://localhost:$debugPort/json" -TimeoutSec 5
    $target = $targets | Where-Object { $_.type -eq "page" } | Select-Object -First 1
    if (-not $target) {
        Write-Host "No page target found" -ForegroundColor Red
        exit 1
    }
    Write-Host "Target: $($target.title)" -ForegroundColor Green
    Write-Host "URL: $($target.url)" -ForegroundColor Gray
    $targetId = $target.id
} catch {
    Write-Host "Cannot connect to WangShangLiao at port $debugPort" -ForegroundColor Red
    exit 1
}

# Function to execute JavaScript via CDP HTTP endpoint
function Invoke-CDP {
    param(
        [string]$expression,
        [bool]$awaitPromise = $false
    )
    
    $body = @{
        expression = $expression
        returnByValue = $true
        awaitPromise = $awaitPromise
    } | ConvertTo-Json -Compress
    
    try {
        # Use the devtools protocol URL
        $wsUrl = $target.webSocketDebuggerUrl
        
        # Since we can't use WebSocket directly, let's use a simple approach
        # by creating a temporary page and injecting script
        
        # Alternative: Use the /json/evaluate endpoint if available
        # or fall back to a different method
        
        return $null
    } catch {
        return $null
    }
}

# Let's use a different approach - create a simple TCP client
Add-Type -TypeDefinition @"
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Security.Cryptography;
using System.Threading;

public class SimpleWebSocket
{
    private TcpClient client;
    private NetworkStream stream;
    
    public bool Connect(string host, int port, string path)
    {
        try
        {
            client = new TcpClient();
            client.Connect(host, port);
            stream = client.GetStream();
            
            // WebSocket handshake
            string key = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            string handshake = "GET " + path + " HTTP/1.1\r\n" +
                              "Host: " + host + ":" + port + "\r\n" +
                              "Upgrade: websocket\r\n" +
                              "Connection: Upgrade\r\n" +
                              "Sec-WebSocket-Key: " + key + "\r\n" +
                              "Sec-WebSocket-Version: 13\r\n\r\n";
            
            byte[] handshakeBytes = Encoding.UTF8.GetBytes(handshake);
            stream.Write(handshakeBytes, 0, handshakeBytes.Length);
            
            // Read response
            byte[] buffer = new byte[4096];
            int read = stream.Read(buffer, 0, buffer.Length);
            string response = Encoding.UTF8.GetString(buffer, 0, read);
            
            return response.Contains("101");
        }
        catch
        {
            return false;
        }
    }
    
    public void Send(string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        byte[] frame;
        
        if (data.Length < 126)
        {
            frame = new byte[6 + data.Length];
            frame[0] = 0x81;
            frame[1] = (byte)(0x80 | data.Length);
        }
        else if (data.Length < 65536)
        {
            frame = new byte[8 + data.Length];
            frame[0] = 0x81;
            frame[1] = 0xFE;
            frame[2] = (byte)(data.Length >> 8);
            frame[3] = (byte)(data.Length & 0xFF);
        }
        else
        {
            frame = new byte[14 + data.Length];
            frame[0] = 0x81;
            frame[1] = 0xFF;
            for (int i = 0; i < 8; i++)
            {
                frame[9 - i] = (byte)((data.Length >> (i * 8)) & 0xFF);
            }
        }
        
        // Mask key (4 random bytes)
        byte[] mask = new byte[4];
        new Random().NextBytes(mask);
        int headerLen = frame.Length - data.Length;
        Array.Copy(mask, 0, frame, headerLen - 4, 4);
        
        // Mask data
        for (int i = 0; i < data.Length; i++)
        {
            frame[headerLen + i] = (byte)(data[i] ^ mask[i % 4]);
        }
        
        stream.Write(frame, 0, frame.Length);
    }
    
    public string Receive()
    {
        byte[] header = new byte[2];
        stream.Read(header, 0, 2);
        
        int length = header[1] & 0x7F;
        if (length == 126)
        {
            byte[] lenBytes = new byte[2];
            stream.Read(lenBytes, 0, 2);
            length = (lenBytes[0] << 8) | lenBytes[1];
        }
        else if (length == 127)
        {
            byte[] lenBytes = new byte[8];
            stream.Read(lenBytes, 0, 8);
            length = 0;
            for (int i = 0; i < 8; i++)
            {
                length = (length << 8) | lenBytes[i];
            }
        }
        
        byte[] data = new byte[length];
        int totalRead = 0;
        while (totalRead < length)
        {
            int read = stream.Read(data, totalRead, length - totalRead);
            if (read == 0) break;
            totalRead += read;
        }
        
        return Encoding.UTF8.GetString(data, 0, totalRead);
    }
    
    public void Close()
    {
        try { stream?.Close(); } catch { }
        try { client?.Close(); } catch { }
    }
}
"@

# Parse WebSocket URL
$wsUrl = $target.webSocketDebuggerUrl
if ($wsUrl -match "ws://([^:]+):(\d+)(.*)") {
    $wsHost = $Matches[1]
    $wsPort = [int]$Matches[2]
    $wsPath = $Matches[3]
    
    Write-Host "Connecting to WebSocket: $wsHost`:$wsPort$wsPath" -ForegroundColor Gray
}

$ws = New-Object SimpleWebSocket
if (-not $ws.Connect($wsHost, $wsPort, $wsPath)) {
    Write-Host "WebSocket connection failed" -ForegroundColor Red
    exit 1
}
Write-Host "WebSocket connected!" -ForegroundColor Green

function Send-CDP {
    param([string]$method, [hashtable]$params = @{}, [bool]$awaitPromise = $false)
    
    $id = Get-Random -Minimum 1 -Maximum 99999
    $cmd = @{ id = $id; method = $method; params = $params }
    if ($awaitPromise) {
        $cmd.params.awaitPromise = $true
    }
    $json = $cmd | ConvertTo-Json -Depth 10 -Compress
    
    $ws.Send($json)
    
    # Read response (may need to skip events)
    $maxAttempts = 50
    for ($i = 0; $i -lt $maxAttempts; $i++) {
        $response = $ws.Receive()
        $obj = $response | ConvertFrom-Json
        if ($obj.id -eq $id) {
            return $obj
        }
    }
    return $null
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

$resp = Send-CDP -method "Runtime.evaluate" -params @{ expression = $nimScript; returnByValue = $true }
if ($resp.result.result.value) {
    $nimData = $resp.result.result.value | ConvertFrom-Json
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

$resp = Send-CDP -method "Runtime.evaluate" -params @{ expression = $vueScript; returnByValue = $true }
if ($resp.result.result.value) {
    $vueData = $resp.result.result.value | ConvertFrom-Json
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

$resp = Send-CDP -method "Runtime.evaluate" -params @{ expression = $sessionScript; returnByValue = $true }
if ($resp.result.result.value) {
    $sessionData = $resp.result.result.value | ConvertFrom-Json
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

$resp = Send-CDP -method "Runtime.evaluate" -params @{ expression = $groupScript; returnByValue = $true }
if ($resp.result.result.value) {
    $groupData = $resp.result.result.value | ConvertFrom-Json
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

$resp = Send-CDP -method "Runtime.evaluate" -params @{ expression = $msgScript; returnByValue = $true }
if ($resp.result.result.value) {
    $msgData = $resp.result.result.value | ConvertFrom-Json
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
# PART 6: Team Members (Critical for OnlyMemberBet)
# ============================================
Write-Host "`n=== PART 6: Team Members (for OnlyMemberBet) ===" -ForegroundColor Yellow

$memberScript = @'
(async function() {
    var result = {
        success: false,
        teamId: null,
        memberCount: 0,
        memberFields: [],
        sampleMembers: [],
        memberTypes: [],
        allMembers: []
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
                    if (err) {
                        console.log('getTeamMembers error:', err);
                        resolve([]);
                    } else {
                        console.log('getTeamMembers success:', obj);
                        resolve(obj.members || obj || []);
                    }
                }
            });
            setTimeout(function() { resolve([]); }, 10000);
        });
        
        if (members.length > 0) {
            result.success = true;
            result.memberCount = members.length;
            result.memberFields = Object.keys(members[0]);
            
            // Get all members with safe data
            result.allMembers = members.map(function(m) {
                var safe = {};
                Object.keys(m).forEach(function(k) {
                    var val = m[k];
                    if (typeof val === 'string') {
                        safe[k] = val.length > 50 ? val.substring(0,50) + '...' : val;
                    } else if (typeof val === 'number' || typeof val === 'boolean') {
                        safe[k] = val;
                    } else if (val && typeof val === 'object') {
                        safe[k] = '{object}';
                    }
                });
                return safe;
            });
            
            // Get first 3 as samples
            result.sampleMembers = result.allMembers.slice(0, 3);
            
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

$resp = Send-CDP -method "Runtime.evaluate" -params @{ expression = $memberScript; returnByValue = $true; awaitPromise = $true }
if ($resp.result.result.value) {
    $memberData = $resp.result.result.value | ConvertFrom-Json
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
    
    if ($memberData.sampleMembers.Count -gt 0) {
        Write-Host "`nSample Members:" -ForegroundColor Yellow
        foreach ($m in $memberData.sampleMembers) {
            Write-Host "  ---" -ForegroundColor Gray
            $m.PSObject.Properties | ForEach-Object {
                Write-Host "    $($_.Name) = $($_.Value)" -ForegroundColor White
            }
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

$resp = Send-CDP -method "Runtime.evaluate" -params @{ expression = $userScript; returnByValue = $true }
if ($resp.result.result.value) {
    $userData = $resp.result.result.value | ConvertFrom-Json
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
# PART 8: Deep Dive into ALL Store States
# ============================================
Write-Host "`n=== PART 8: Deep Dive into ALL Store States ===" -ForegroundColor Yellow

$deepScript = @'
(function() {
    var result = {};
    
    var app = document.querySelector('#app');
    if (!app || !app.__vue__ || !app.__vue__.$store) return JSON.stringify(result);
    
    var state = app.__vue__.$store.state;
    
    // Iterate all store modules
    Object.keys(state).forEach(function(mod) {
        var modState = state[mod];
        if (!modState || typeof modState !== 'object') return;
        
        result[mod] = {};
        
        Object.keys(modState).forEach(function(key) {
            var val = modState[key];
            
            if (val === null || val === undefined) {
                result[mod][key] = String(val);
            } else if (typeof val === 'string') {
                result[mod][key] = 'string(' + val.length + ')';
            } else if (typeof val === 'number') {
                result[mod][key] = 'number: ' + val;
            } else if (typeof val === 'boolean') {
                result[mod][key] = 'boolean: ' + val;
            } else if (Array.isArray(val)) {
                if (val.length === 0) {
                    result[mod][key] = 'array[0]';
                } else {
                    var first = val[0];
                    var fields = typeof first === 'object' && first ? Object.keys(first).slice(0, 10).join(',') : typeof first;
                    result[mod][key] = 'array[' + val.length + '] of {' + fields + '}';
                }
            } else if (typeof val === 'object') {
                var objKeys = Object.keys(val);
                if (objKeys.length === 0) {
                    result[mod][key] = 'object{}';
                } else {
                    // Check if it's a map of items
                    var firstObjKey = objKeys[0];
                    var firstObj = val[firstObjKey];
                    if (typeof firstObj === 'object' && firstObj) {
                        var objFields = Object.keys(firstObj).slice(0, 10).join(',');
                        result[mod][key] = 'map[' + objKeys.length + '] of {' + objFields + '}';
                    } else {
                        result[mod][key] = 'object{' + objKeys.slice(0, 10).join(',') + '}';
                    }
                }
            }
        });
    });
    
    return JSON.stringify(result);
})();
'@

$resp = Send-CDP -method "Runtime.evaluate" -params @{ expression = $deepScript; returnByValue = $true }
if ($resp.result.result.value) {
    $deepData = $resp.result.result.value | ConvertFrom-Json
    $allResults.deepDive = $deepData
    
    $deepData.PSObject.Properties | ForEach-Object {
        Write-Host "`n  [$($_.Name)]:" -ForegroundColor Yellow
        $_.Value.PSObject.Properties | ForEach-Object {
            Write-Host "    $($_.Name): $($_.Value)" -ForegroundColor White
        }
    }
}

# ============================================
# PART 9: Team Info from NIM SDK
# ============================================
Write-Host "`n=== PART 9: Team Info from NIM SDK ===" -ForegroundColor Yellow

$teamInfoScript = @'
(async function() {
    var result = {
        success: false,
        teamId: null,
        teamInfo: null,
        teamFields: []
    };
    
    // Get team ID
    var app = document.querySelector('#app');
    if (app && app.__vue__ && app.__vue__.$store) {
        var store = app.__vue__.$store.state;
        if (store.sessionStore && store.sessionStore.currentSession) {
            result.teamId = store.sessionStore.currentSession.to;
        }
    }
    
    if (!result.teamId || !window.nim) return JSON.stringify(result);
    
    // Get team info via NIM SDK
    if (typeof window.nim.getTeam === 'function') {
        var team = await new Promise(function(resolve) {
            window.nim.getTeam({
                teamId: result.teamId,
                done: function(err, obj) {
                    if (err) resolve(null);
                    else resolve(obj);
                }
            });
            setTimeout(function() { resolve(null); }, 5000);
        });
        
        if (team) {
            result.success = true;
            result.teamFields = Object.keys(team);
            result.teamInfo = {};
            
            Object.keys(team).forEach(function(k) {
                var val = team[k];
                if (typeof val === 'string') {
                    result.teamInfo[k] = val.length > 100 ? val.substring(0,100) + '...' : val;
                } else if (typeof val === 'number' || typeof val === 'boolean') {
                    result.teamInfo[k] = val;
                } else if (Array.isArray(val)) {
                    result.teamInfo[k] = '[array:' + val.length + ']';
                } else if (val && typeof val === 'object') {
                    result.teamInfo[k] = '{' + Object.keys(val).slice(0,5).join(',') + '}';
                }
            });
        }
    }
    
    return JSON.stringify(result);
})();
'@

$resp = Send-CDP -method "Runtime.evaluate" -params @{ expression = $teamInfoScript; returnByValue = $true; awaitPromise = $true }
if ($resp.result.result.value) {
    $teamInfoData = $resp.result.result.value | ConvertFrom-Json
    $allResults.teamInfo = $teamInfoData
    
    Write-Host "Team ID: $($teamInfoData.teamId)" -ForegroundColor Cyan
    Write-Host "Success: $($teamInfoData.success)" -ForegroundColor $(if($teamInfoData.success){"Green"}else{"Red"})
    
    if ($teamInfoData.teamFields.Count -gt 0) {
        Write-Host "`nTeam Fields:" -ForegroundColor Yellow
        $teamInfoData.teamFields | ForEach-Object { Write-Host "  - $_" -ForegroundColor White }
    }
    
    if ($teamInfoData.teamInfo) {
        Write-Host "`nTeam Info:" -ForegroundColor Cyan
        $teamInfoData.teamInfo.PSObject.Properties | ForEach-Object {
            Write-Host "  $($_.Name) = $($_.Value)" -ForegroundColor White
        }
    }
}

# ============================================
# Save Results
# ============================================
Write-Host "`n=== Saving Results ===" -ForegroundColor Yellow

$outputFile = "wangshangliao_deep_$(Get-Date -Format 'yyyyMMdd_HHmmss').json"
$allResults | ConvertTo-Json -Depth 15 | Out-File $outputFile -Encoding UTF8
Write-Host "Full results saved to: $outputFile" -ForegroundColor Green

# Cleanup
$ws.Close()
Write-Host "`n=== Deep Exploration Complete ===" -ForegroundColor Cyan

