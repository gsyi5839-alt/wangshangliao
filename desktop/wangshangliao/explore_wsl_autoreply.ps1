# Explore WangShangLiao internal auto-reply mechanism
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

Write-Host "=== EXPLORE WSL AUTO-REPLY MECHANISM ===" -ForegroundColor Cyan

# Check for NIM SDK message listeners and handlers
$script = '(function() {
    var result = { nimEvents: [], messageHandlers: [], stores: {} };
    
    try {
        // Check NIM SDK for message events
        if (window.nim) {
            // Get all event listeners on nim
            var nimKeys = Object.keys(window.nim);
            result.nimKeys = nimKeys.filter(function(k) {
                return k.toLowerCase().indexOf("msg") >= 0 || 
                       k.toLowerCase().indexOf("message") >= 0 ||
                       k.toLowerCase().indexOf("receive") >= 0 ||
                       k.toLowerCase().indexOf("send") >= 0 ||
                       k.toLowerCase().indexOf("text") >= 0;
            });
            
            // Check nim.on* handlers
            result.nimOnHandlers = nimKeys.filter(function(k) {
                return k.indexOf("on") === 0;
            });
            
            // Check if nim has event emitter
            if (window.nim._events) {
                result.nimEventNames = Object.keys(window.nim._events);
            }
        }
        
        // Check Pinia stores for message handling
        var app = document.querySelector("#app");
        var gp = app && app.__vue_app__ && app.__vue_app__.config && app.__vue_app__.config.globalProperties;
        var pinia = gp && gp.$pinia;
        
        if (pinia && pinia._s) {
            pinia._s.forEach(function(store, key) {
                var storeKeys = Object.keys(store);
                var msgRelated = storeKeys.filter(function(k) {
                    return k.toLowerCase().indexOf("msg") >= 0 || 
                           k.toLowerCase().indexOf("message") >= 0 ||
                           k.toLowerCase().indexOf("send") >= 0 ||
                           k.toLowerCase().indexOf("receive") >= 0;
                });
                if (msgRelated.length > 0) {
                    result.stores[key] = msgRelated;
                }
            });
        }
        
        // Check for global message event bus
        if (window.$bus || window.eventBus || window.EventBus) {
            result.hasEventBus = true;
        }
        
        // Check for global message callback
        if (window.onMsg || window.onMessage || window.handleMessage) {
            result.hasGlobalHandler = true;
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
Write-Host "NIM SDK & Message Handlers:" -ForegroundColor Yellow
Write-Host $result

