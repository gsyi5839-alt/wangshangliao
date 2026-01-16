# Check current session state in WangShangLiao
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

Write-Host "=== CHECK SESSION STATE ===" -ForegroundColor Cyan

$script = @'
(function() {
    var result = {
        nimExists: false,
        piniaExists: false,
        appStoreExists: false,
        currentSession: null,
        sessionList: [],
        nimAccount: null,
        error: null
    };
    
    try {
        // Check NIM
        result.nimExists = !!window.nim;
        if (window.nim && window.nim.options) {
            result.nimAccount = window.nim.options.account;
        }
        
        // Check Pinia and stores
        var app = document.querySelector("#app");
        if (app && app.__vue_app__) {
            var gp = app.__vue_app__.config && app.__vue_app__.config.globalProperties;
            var pinia = gp && gp.$pinia;
            result.piniaExists = !!pinia;
            
            if (pinia && pinia._s) {
                var appStore = pinia._s.get("app");
                result.appStoreExists = !!appStore;
                
                if (appStore) {
                    // Current session
                    if (appStore.currentSession) {
                        var s = appStore.currentSession;
                        result.currentSession = {
                            scene: s.scene,
                            to: s.to,
                            hasGroup: !!s.group,
                            groupName: s.group ? s.group.name : null,
                            teamId: s.group ? (s.group.groupCloudId || s.group.teamId) : null
                        };
                    }
                    
                    // Session list
                    if (appStore.sessions && appStore.sessions.length > 0) {
                        for (var i = 0; i < Math.min(appStore.sessions.length, 5); i++) {
                            var sess = appStore.sessions[i];
                            result.sessionList.push({
                                scene: sess.scene,
                                to: sess.to,
                                name: sess.name || (sess.group ? sess.group.name : null)
                            });
                        }
                    }
                }
                
                // Also check SDK store
                var sdkStore = pinia._s.get("sdk");
                if (sdkStore && sdkStore.nim) {
                    result.sdkNimAccount = sdkStore.nim.options ? sdkStore.nim.options.account : null;
                }
            }
        }
        
        // Alternative: try to get from chat area elements
        var chatHeader = document.querySelector('[class*="chat-header"], [class*="session-header"]');
        if (chatHeader) {
            result.chatHeaderText = chatHeader.innerText.substring(0, 50);
        }
        
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})()
'@

$cmd = @{
    method = "Runtime.evaluate"
    params = @{ expression = $script; returnByValue = $true; awaitPromise = $false }
}

$response = Invoke-CdpCommand -Command $cmd -Timeout 60000
if ($response.result -and $response.result.result -and $response.result.result.value) {
    Write-Host $response.result.result.value -ForegroundColor Green
}

