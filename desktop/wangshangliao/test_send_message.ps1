# Test sending message via NIM SDK
$ErrorActionPreference = 'Stop'
$cdpPort = 9333

function Invoke-CdpCommand {
    param([hashtable]$Command, [int]$Timeout = 90000)
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

Write-Host "=== TEST SEND MESSAGE ===" -ForegroundColor Cyan

# First explore sendText signature
$exploreScript = '(function() {
    var result = {};
    
    try {
        if (window.nim && window.nim.sendText) {
            // Get current session info
            var app = document.querySelector("#app");
            var gp = app && app.__vue_app__ && app.__vue_app__.config && app.__vue_app__.config.globalProperties;
            var pinia = gp && gp.$pinia;
            var appStore = pinia && pinia._s && pinia._s.get && pinia._s.get("app");
            
            var currSession = appStore ? appStore.currSession : null;
            
            if (currSession) {
                result.scene = currSession.scene;  // "team" for group
                result.to = currSession.to;  // teamId / groupCloudId
                result.groupName = currSession.group ? currSession.group.name : null;
            }
            
            // Test sendText - we need to understand its parameters
            // NIM SDK sendText typically needs: { scene, to, text, done }
            result.sendTextLength = window.nim.sendText.length;
        }
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})()'

$cmd = @{
    method = "Runtime.evaluate"
    params = @{ expression = $exploreScript; returnByValue = $true; awaitPromise = $false }
}

Write-Host "Getting session info..." -ForegroundColor Yellow
$response = Invoke-CdpCommand -Command $cmd -Timeout 60000

$result = $null
if ($response -is [array]) {
    foreach ($item in $response) {
        if ($item.result -and $item.result.result -and $item.result.result.value) {
            $result = $item.result.result.value
            break
        }
    }
}

Write-Host "Session Info:" -ForegroundColor Yellow
Write-Host $result

# Now test sending a test message (commented out for safety)
Write-Host ""
Write-Host "To send a message, use this script format:" -ForegroundColor Cyan

$sendScript = @'
(async function() {
    var result = {};
    
    try {
        var app = document.querySelector("#app");
        var gp = app && app.__vue_app__ && app.__vue_app__.config && app.__vue_app__.config.globalProperties;
        var pinia = gp && gp.$pinia;
        var appStore = pinia && pinia._s && pinia._s.get && pinia._s.get("app");
        var sdkStore = pinia && pinia._s && pinia._s.get && pinia._s.get("sdk");
        
        var currSession = appStore ? appStore.currSession : null;
        
        if (currSession && window.nim) {
            var scene = currSession.scene;  // "team"
            var to = currSession.to;  // teamId
            var text = "测试消息";  // Message text
            
            // Method 1: Direct nim.sendText
            window.__sendResult = null;
            window.nim.sendText({
                scene: scene,
                to: to,
                text: text,
                done: function(err, msg) {
                    window.__sendResult = {
                        success: !err,
                        error: err ? err.message : null,
                        msg: msg
                    };
                }
            });
            
            result.called = true;
            result.scene = scene;
            result.to = to;
        } else {
            result.error = "No session or nim";
        }
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result);
})()
'@

Write-Host $sendScript

