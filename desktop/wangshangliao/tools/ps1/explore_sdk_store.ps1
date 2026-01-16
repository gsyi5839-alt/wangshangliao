# Explore SDK store and auto-reply methods
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

Write-Host "=== EXPLORE SDK STORE ===" -ForegroundColor Cyan

$script = '(function() {
    var result = {};
    
    try {
        var app = document.querySelector("#app");
        var gp = app && app.__vue_app__ && app.__vue_app__.config && app.__vue_app__.config.globalProperties;
        var pinia = gp && gp.$pinia;
        
        // Get SDK store
        var sdkStore = pinia && pinia._s && pinia._s.get && pinia._s.get("sdk");
        
        if (sdkStore) {
            // List all methods in sdk store
            result.sdkMethods = [];
            var keys = Object.keys(sdkStore);
            for (var i = 0; i < keys.length; i++) {
                var k = keys[i];
                var type = typeof sdkStore[k];
                if (type === "function") {
                    result.sdkMethods.push(k);
                }
            }
            
            // Check sendNimAutoReplyMsg function signature
            if (typeof sdkStore.sendNimAutoReplyMsg === "function") {
                result.hasSendNimAutoReplyMsg = true;
                result.sendNimAutoReplyMsgStr = sdkStore.sendNimAutoReplyMsg.toString().substring(0, 500);
            }
            
            // Check sendNimMsg
            if (typeof sdkStore.sendNimMsg === "function") {
                result.hasSendNimMsg = true;
                result.sendNimMsgStr = sdkStore.sendNimMsg.toString().substring(0, 500);
            }
            
            // Check customMsg state
            result.customMsg = sdkStore.customMsg;
        }
        
        // Also check app store for replyMsg
        var appStore = pinia && pinia._s && pinia._s.get && pinia._s.get("app");
        if (appStore && typeof appStore.replyMsg === "function") {
            result.hasReplyMsg = true;
            result.replyMsgStr = appStore.replyMsg.toString().substring(0, 500);
        }
        
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})()'

$cmd = @{
    method = "Runtime.evaluate"
    params = @{ expression = $script; returnByValue = $true; awaitPromise = $false }
}

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

Write-Host ""
Write-Host "SDK Store Methods:" -ForegroundColor Yellow
Write-Host $result

