# Test message hook injection
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

Write-Host "=== TEST MESSAGE HOOK ===" -ForegroundColor Cyan

# Install message hook
$hookScript = '(function() {
    var result = { installed: false };
    
    try {
        if (!window.nim) {
            result.error = "nim not found";
            return JSON.stringify(result);
        }
        
        // Store received messages
        window.__receivedMessages = window.__receivedMessages || [];
        
        // Check existing event names
        result.existingEvents = window.nim._events ? Object.keys(window.nim._events) : [];
        
        // Try to hook into message events
        // NIM SDK uses "msg" event for receiving messages
        var events = ["msg", "msgs", "onmsg", "onMsg", "message", "teamMsg", "recvMsg"];
        result.hookedEvents = [];
        
        for (var i = 0; i < events.length; i++) {
            var evt = events[i];
            try {
                window.nim.on(evt, function(msg) {
                    window.__receivedMessages.push({
                        event: evt,
                        msg: msg,
                        time: Date.now()
                    });
                    // Keep only last 50 messages
                    if (window.__receivedMessages.length > 50) {
                        window.__receivedMessages.shift();
                    }
                });
                result.hookedEvents.push(evt);
            } catch(e) {
                // Event may not exist
            }
        }
        
        // Also try to hook nim.options.onmsg if it exists
        if (window.nim.options) {
            result.nimOptionsKeys = Object.keys(window.nim.options).filter(function(k) {
                return k.toLowerCase().indexOf("on") === 0;
            });
            
            var origOnmsg = window.nim.options.onmsg;
            window.nim.options.onmsg = function(msg) {
                window.__receivedMessages.push({
                    event: "options.onmsg",
                    msg: msg,
                    time: Date.now()
                });
                if (origOnmsg) origOnmsg(msg);
            };
            result.hookedOptionsOnmsg = true;
        }
        
        result.installed = true;
        
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})()'

$cmd = @{
    method = "Runtime.evaluate"
    params = @{ expression = $hookScript; returnByValue = $true; awaitPromise = $false }
}

Write-Host "Installing message hook..." -ForegroundColor Yellow
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

Write-Host "Hook result:" -ForegroundColor Yellow
Write-Host $result

Write-Host ""
Write-Host "Waiting 10 seconds for new messages..." -ForegroundColor Cyan
Start-Sleep -Seconds 10

# Check received messages
$checkScript = '(function() {
    return JSON.stringify({
        count: window.__receivedMessages ? window.__receivedMessages.length : 0,
        messages: window.__receivedMessages ? window.__receivedMessages.slice(-5) : []
    }, null, 2);
})()'

$checkCmd = @{
    method = "Runtime.evaluate"
    params = @{ expression = $checkScript; returnByValue = $true; awaitPromise = $false }
}

$checkResponse = Invoke-CdpCommand -Command $checkCmd -Timeout 60000

$checkResult = $null
if ($checkResponse -is [array]) {
    foreach ($item in $checkResponse) {
        if ($item.result -and $item.result.result -and $item.result.result.value) {
            $checkResult = $item.result.result.value
            break
        }
    }
}

Write-Host ""
Write-Host "Received Messages:" -ForegroundColor Yellow
Write-Host $checkResult

