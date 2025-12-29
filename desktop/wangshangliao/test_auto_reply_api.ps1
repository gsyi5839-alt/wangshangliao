# Test auto-reply API
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

Write-Host "=== TEST AUTO-REPLY API ===" -ForegroundColor Cyan

# First get current session info and try calling sendNimMsg
$script = '(async function() {
    var result = {};
    
    try {
        var app = document.querySelector("#app");
        var gp = app && app.__vue_app__ && app.__vue_app__.config && app.__vue_app__.config.globalProperties;
        var pinia = gp && gp.$pinia;
        
        var sdkStore = pinia && pinia._s && pinia._s.get && pinia._s.get("sdk");
        var appStore = pinia && pinia._s && pinia._s.get && pinia._s.get("app");
        
        // Get current session info
        var currSession = appStore ? appStore.currSession : null;
        var groupInfoRes = appStore ? appStore.groupInfoRes : null;
        
        result.currSession = currSession;
        result.groupInfoRes = groupInfoRes ? {
            groupId: groupInfoRes.groupInfo ? groupInfoRes.groupInfo.groupId : null,
            groupCloudId: groupInfoRes.groupInfo ? groupInfoRes.groupInfo.groupCloudId : null,
            name: groupInfoRes.groupInfo ? groupInfoRes.groupInfo.name : null
        } : null;
        
        // Try to understand sendNimMsg parameters by looking at how it is called
        // We can trace it by checking nim.sendText
        if (window.nim) {
            result.nimSendMethods = [];
            var keys = Object.keys(window.nim);
            for (var i = 0; i < keys.length; i++) {
                var k = keys[i];
                if (k.indexOf("send") >= 0) {
                    result.nimSendMethods.push(k);
                }
            }
            
            // Check prototype
            var proto = Object.getPrototypeOf(window.nim);
            if (proto) {
                var protoKeys = Object.getOwnPropertyNames(proto);
                result.nimProtoSendMethods = protoKeys.filter(function(k) {
                    return k.indexOf("send") >= 0 || k.indexOf("Send") >= 0;
                });
            }
        }
        
        // Check if there is a send function in sdkStore
        if (sdkStore) {
            // Try to find the actual implementation
            result.sdkStoreState = sdkStore.$state ? Object.keys(sdkStore.$state) : [];
        }
        
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})()'

$cmd = @{
    method = "Runtime.evaluate"
    params = @{ expression = $script; returnByValue = $true; awaitPromise = $true }
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
Write-Host "API Info:" -ForegroundColor Yellow
Write-Host $result

