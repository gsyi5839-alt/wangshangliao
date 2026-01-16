# Get captured file operations from WangShangLiao
# 获取捕获的文件操作
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

Write-Host "=== GET CAPTURED FILE OPERATIONS ===" -ForegroundColor Cyan
Write-Host ""

$script = @'
(function() {
    var result = {
        operations: [],
        count: 0
    };
    
    try {
        if (window.__botFileOps && window.__botFileOps.length > 0) {
            result.operations = window.__botFileOps;
            result.count = window.__botFileOps.length;
            // Clear after reading
            // window.__botFileOps = [];
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
$resultValue = $null
if ($response.result -and $response.result.result -and $response.result.result.value) {
    $resultValue = $response.result.result.value
}

Write-Host "Captured File Operations:" -ForegroundColor Yellow
Write-Host $resultValue
Write-Host ""

# Also get console logs
Write-Host "Getting Console Logs..." -ForegroundColor Yellow

$scriptLogs = @'
(function() {
    var logs = [];
    if (window.__botConsoleLogs) {
        logs = window.__botConsoleLogs.filter(function(l) {
            return l.indexOf("[BOT]") >= 0;
        });
    }
    return JSON.stringify(logs.slice(-20), null, 2);
})()
'@

$cmdLogs = @{
    method = "Runtime.evaluate"
    params = @{ expression = $scriptLogs; returnByValue = $true; awaitPromise = $false }
}

$responseLogs = Invoke-CdpCommand -Command $cmdLogs -Timeout 60000
if ($responseLogs.result -and $responseLogs.result.result -and $responseLogs.result.result.value) {
    Write-Host "Console Logs:" -ForegroundColor Green
    Write-Host $responseLogs.result.result.value
}

