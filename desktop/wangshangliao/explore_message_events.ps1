# Explore message events and sending
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

Write-Host "=== EXPLORE MESSAGE EVENTS ===" -ForegroundColor Cyan

$script = '(function() {
    var result = {};
    
    try {
        // Check NIM SDK message object
        if (window.nim && window.nim.message) {
            result.nimMessage = typeof window.nim.message;
            if (typeof window.nim.message === "object") {
                result.nimMessageKeys = Object.keys(window.nim.message);
            }
        }
        
        // Check all nim prototype methods related to messages
        if (window.nim) {
            var proto = Object.getPrototypeOf(window.nim);
            if (proto) {
                var protoKeys = Object.getOwnPropertyNames(proto);
                result.msgMethods = protoKeys.filter(function(k) {
                    return k.toLowerCase().indexOf("msg") >= 0 || 
                           k.toLowerCase().indexOf("text") >= 0 ||
                           k.toLowerCase().indexOf("onmsg") >= 0;
                });
            }
            
            // Check for event emitter style handlers
            if (window.nim.on) {
                result.hasOnMethod = true;
            }
            if (window.nim.emit) {
                result.hasEmitMethod = true;
            }
            
            // Check _events for registered listeners
            if (window.nim._events) {
                result.registeredEvents = Object.keys(window.nim._events);
            }
            
            // Check if nim has sendText method
            result.hasSendText = typeof window.nim.sendText === "function";
            result.hasSendMsg = typeof window.nim.sendMsg === "function";
        }
        
        // Check for global message hooks
        result.globalHooks = [];
        if (window.__messageCallbacks) result.globalHooks.push("__messageCallbacks");
        if (window.__onMessage) result.globalHooks.push("__onMessage");
        if (window.__nimOnMsg) result.globalHooks.push("__nimOnMsg");
        
        // Check Pinia sdk store for nim instance
        var app = document.querySelector("#app");
        var gp = app && app.__vue_app__ && app.__vue_app__.config && app.__vue_app__.config.globalProperties;
        var pinia = gp && gp.$pinia;
        var sdkStore = pinia && pinia._s && pinia._s.get && pinia._s.get("sdk");
        
        if (sdkStore && sdkStore.nim) {
            result.sdkStoreNim = true;
            result.sdkNimSameAsWindow = sdkStore.nim === window.nim;
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
Write-Host "Message Events:" -ForegroundColor Yellow
Write-Host $result

