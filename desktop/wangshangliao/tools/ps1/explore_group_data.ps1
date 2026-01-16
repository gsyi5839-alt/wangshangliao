# 探索 Pinia Store 中的群列表和消息数据结构
$ErrorActionPreference = 'Stop'
$cdpPort = 9333

function Invoke-CdpCommand {
    param([hashtable]$Command, [int]$Timeout = 60000)
    $ws = $null
    try {
        $response = Invoke-RestMethod -Uri "http://127.0.0.1:${cdpPort}/json" -TimeoutSec 5
        $wsUrl = ($response | Where-Object { $_.type -eq 'page' } | Select-Object -First 1).webSocketDebuggerUrl
        if (-not $wsUrl) { throw "No WebSocket URL" }
        $ws = New-Object System.Net.WebSockets.ClientWebSocket
        $ws.Options.KeepAliveInterval = [TimeSpan]::FromSeconds(30)
        $ct = [System.Threading.CancellationToken]::None
        $ws.ConnectAsync([Uri]$wsUrl, $ct).Wait($Timeout)
        $Command['id'] = 1
        $json = $Command | ConvertTo-Json -Depth 10 -Compress
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
        $segment = [ArraySegment[byte]]::new($bytes)
        $ws.SendAsync($segment, [System.Net.WebSockets.WebSocketMessageType]::Text, $true, $ct).Wait($Timeout)
        $buffer = New-Object byte[] 1048576
        $result = New-Object System.Text.StringBuilder
        do {
            $segment = [ArraySegment[byte]]::new($buffer)
            $receiveTask = $ws.ReceiveAsync($segment, $ct)
            if (-not $receiveTask.Wait($Timeout)) { throw "Timeout" }
            $received = $receiveTask.Result
            $result.Append([System.Text.Encoding]::UTF8.GetString($buffer, 0, $received.Count)) | Out-Null
        } while (-not $received.EndOfMessage)
        return $result.ToString() | ConvertFrom-Json
    } finally {
        if ($ws -and $ws.State -eq 'Open') { $ws.CloseAsync([System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure, "", [System.Threading.CancellationToken]::None).Wait(5000) }
        if ($ws) { $ws.Dispose() }
    }
}

Write-Host "=== 探索 Pinia Store 数据结构 ===" -ForegroundColor Cyan
Write-Host ""

# 1. 列出所有 Pinia stores
Write-Host "【1】列出所有 Pinia Stores..." -ForegroundColor Yellow
$script1 = @'
(function() {
    var result = { stores: [], error: null };
    try {
        var app = document.querySelector("#app");
        var gp = app && app.__vue_app__ && app.__vue_app__.config && app.__vue_app__.config.globalProperties;
        var pinia = gp && gp.$pinia;
        
        if (pinia && pinia._s) {
            pinia._s.forEach(function(store, name) {
                var keys = Object.keys(store);
                var methods = keys.filter(function(k) { return typeof store[k] === 'function'; });
                var props = keys.filter(function(k) { return typeof store[k] !== 'function'; });
                result.stores.push({
                    name: name,
                    methodCount: methods.length,
                    propCount: props.length,
                    methods: methods.slice(0, 20),
                    props: props.slice(0, 20)
                });
            });
        }
    } catch(e) {
        result.error = e.message;
    }
    return JSON.stringify(result, null, 2);
})()
'@

$cmd = @{ method = "Runtime.evaluate"; params = @{ expression = $script1; returnByValue = $true } }
$response = Invoke-CdpCommand -Command $cmd
if ($response.result -and $response.result.result -and $response.result.result.value) {
    $data = $response.result.result.value | ConvertFrom-Json
    Write-Host "找到 $($data.stores.Count) 个 stores:" -ForegroundColor Green
    foreach ($s in $data.stores) {
        Write-Host "  - $($s.name): $($s.propCount) props, $($s.methodCount) methods" -ForegroundColor Gray
    }
}

Write-Host ""

# 2. 探索 team/group store
Write-Host "【2】探索群聊相关 Store..." -ForegroundColor Yellow
$script2 = @'
(function() {
    var result = { teamStore: null, groupStore: null, sessionStore: null, error: null };
    try {
        var app = document.querySelector("#app");
        var gp = app && app.__vue_app__ && app.__vue_app__.config && app.__vue_app__.config.globalProperties;
        var pinia = gp && gp.$pinia;
        
        // Team store
        var teamStore = pinia && pinia._s && pinia._s.get && pinia._s.get("team");
        if (teamStore) {
            result.teamStore = {
                keys: Object.keys(teamStore).filter(function(k) { return typeof teamStore[k] !== 'function'; }),
                teamListLength: teamStore.teamList ? teamStore.teamList.length : 0,
                sampleTeam: teamStore.teamList && teamStore.teamList[0] ? {
                    keys: Object.keys(teamStore.teamList[0])
                } : null
            };
            // 列出前5个群
            if (teamStore.teamList) {
                result.teamStore.teams = [];
                for (var i = 0; i < Math.min(10, teamStore.teamList.length); i++) {
                    var t = teamStore.teamList[i];
                    result.teamStore.teams.push({
                        teamId: t.teamId || t.groupCloudId || '',
                        name: t.name || t.teamName || '',
                        owner: t.owner || '',
                        memberNum: t.memberNum || 0
                    });
                }
            }
        }
        
        // Group store (可能是另一个名字)
        var groupStore = pinia && pinia._s && pinia._s.get && pinia._s.get("group");
        if (groupStore) {
            result.groupStore = {
                keys: Object.keys(groupStore).filter(function(k) { return typeof groupStore[k] !== 'function'; })
            };
        }
        
        // Session store
        var sessionStore = pinia && pinia._s && pinia._s.get && pinia._s.get("session");
        if (sessionStore) {
            result.sessionStore = {
                keys: Object.keys(sessionStore).filter(function(k) { return typeof sessionStore[k] !== 'function'; }),
                sessionListLength: sessionStore.sessionList ? sessionStore.sessionList.length : 0
            };
        }
        
        // App store
        var appStore = pinia && pinia._s && pinia._s.get && pinia._s.get("app");
        if (appStore) {
            result.appStore = {
                keys: Object.keys(appStore).filter(function(k) { return typeof appStore[k] !== 'function'; }),
                currentSession: appStore.currentSession ? {
                    scene: appStore.currentSession.scene,
                    to: appStore.currentSession.to,
                    id: appStore.currentSession.id
                } : null
            };
        }
        
    } catch(e) {
        result.error = e.message;
    }
    return JSON.stringify(result, null, 2);
})()
'@

$cmd = @{ method = "Runtime.evaluate"; params = @{ expression = $script2; returnByValue = $true } }
$response = Invoke-CdpCommand -Command $cmd
if ($response.result -and $response.result.result -and $response.result.result.value) {
    $data = $response.result.result.value | ConvertFrom-Json
    Write-Host $response.result.result.value -ForegroundColor Green
}

Write-Host ""

# 3. 尝试获取群消息历史
Write-Host "【3】获取当前会话的消息历史..." -ForegroundColor Yellow
$script3 = @'
(function() {
    var result = { messages: [], currentSession: null, error: null };
    try {
        var app = document.querySelector("#app");
        var gp = app && app.__vue_app__ && app.__vue_app__.config && app.__vue_app__.config.globalProperties;
        var pinia = gp && gp.$pinia;
        
        // Get message store
        var msgStore = pinia && pinia._s && pinia._s.get && pinia._s.get("msg");
        if (!msgStore) msgStore = pinia && pinia._s && pinia._s.get && pinia._s.get("message");
        
        if (msgStore) {
            result.msgStoreKeys = Object.keys(msgStore).filter(function(k) { return typeof msgStore[k] !== 'function'; });
            
            // Check msgList or messages
            var msgList = msgStore.msgList || msgStore.messages || msgStore.messageList;
            if (msgList && msgList.length) {
                result.msgListLength = msgList.length;
                // Get last 10 messages
                for (var i = Math.max(0, msgList.length - 10); i < msgList.length; i++) {
                    var m = msgList[i];
                    result.messages.push({
                        from: m.from || '',
                        to: m.to || '',
                        text: (m.text || '').substring(0, 100),
                        type: m.type || '',
                        time: m.time || '',
                        scene: m.scene || ''
                    });
                }
            }
        }
        
        // Also check app store currentSession messages
        var appStore = pinia && pinia._s && pinia._s.get && pinia._s.get("app");
        if (appStore && appStore.currentSession) {
            result.currentSession = {
                scene: appStore.currentSession.scene,
                to: appStore.currentSession.to
            };
        }
        
    } catch(e) {
        result.error = e.message;
    }
    return JSON.stringify(result, null, 2);
})()
'@

$cmd = @{ method = "Runtime.evaluate"; params = @{ expression = $script3; returnByValue = $true } }
$response = Invoke-CdpCommand -Command $cmd
if ($response.result -and $response.result.result -and $response.result.result.value) {
    Write-Host $response.result.result.value -ForegroundColor Green
}

Write-Host ""

# 4. 检查 window.nim 对象
Write-Host "【4】检查 window.nim SDK 对象..." -ForegroundColor Yellow
$script4 = @'
(function() {
    var result = { hasNim: false, nimMethods: [], nimOptions: null, error: null };
    try {
        if (window.nim) {
            result.hasNim = true;
            result.nimMethods = Object.keys(window.nim).filter(function(k) { 
                return typeof window.nim[k] === 'function'; 
            }).slice(0, 30);
            
            if (window.nim.options) {
                result.nimOptions = {
                    hasOnmsg: typeof window.nim.options.onmsg === 'function',
                    hasOnmsgs: typeof window.nim.options.onmsgs === 'function',
                    keys: Object.keys(window.nim.options).slice(0, 20)
                };
            }
            
            // Check if hook is installed
            result.hookInstalled = !!window.__monitorHooked;
            result.monitorMessagesCount = (window.__monitorMessages || []).length;
        }
    } catch(e) {
        result.error = e.message;
    }
    return JSON.stringify(result, null, 2);
})()
'@

$cmd = @{ method = "Runtime.evaluate"; params = @{ expression = $script4; returnByValue = $true } }
$response = Invoke-CdpCommand -Command $cmd
if ($response.result -and $response.result.result -and $response.result.result.value) {
    Write-Host $response.result.result.value -ForegroundColor Green
}

Write-Host ""
Write-Host "=== 探索完成 ===" -ForegroundColor Cyan

