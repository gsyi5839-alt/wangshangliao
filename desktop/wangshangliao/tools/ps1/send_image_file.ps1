# Send an actual image file via NIM SDK
# 发送实际图片文件
param(
    [Parameter(Mandatory=$false)]
    [string]$ImagePath,
    
    [Parameter(Mandatory=$false)]
    [string]$TargetId,
    
    [Parameter(Mandatory=$false)]
    [string]$Scene = "team"
)

$ErrorActionPreference = 'Stop'
$cdpPort = 9333

function Invoke-CdpCommand {
    param([hashtable]$Command, [int]$Timeout = 120000)
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
        $buffer = New-Object byte[] 4194304  # 4MB buffer for larger images
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

function Get-MimeType {
    param([string]$FilePath)
    $ext = [System.IO.Path]::GetExtension($FilePath).ToLower()
    switch ($ext) {
        ".jpg"  { return "image/jpeg" }
        ".jpeg" { return "image/jpeg" }
        ".png"  { return "image/png" }
        ".gif"  { return "image/gif" }
        ".webp" { return "image/webp" }
        ".bmp"  { return "image/bmp" }
        default { return "application/octet-stream" }
    }
}

Write-Host "=== SEND IMAGE FILE ===" -ForegroundColor Cyan
Write-Host ""

# Get target from route if not specified
if (-not $TargetId) {
    $scriptTarget = @'
(function() {
    var result = { target: null };
    try {
        var app = document.querySelector("#app");
        if (app && app.__vue_app__) {
            var router = app.__vue_app__.config.globalProperties.$router;
            if (router && router.currentRoute && router.currentRoute.value) {
                var route = router.currentRoute.value;
                if (route.query && route.query.sessionId) {
                    var sid = route.query.sessionId;
                    if (sid.startsWith("team-")) {
                        result.target = { scene: "team", to: sid.substring(5) };
                    } else if (sid.startsWith("p2p-")) {
                        result.target = { scene: "p2p", to: sid.substring(4) };
                    }
                }
            }
        }
    } catch(e) {
        result.error = e.message;
    }
    return JSON.stringify(result);
})()
'@
    
    $cmdTarget = @{
        method = "Runtime.evaluate"
        params = @{ expression = $scriptTarget; returnByValue = $true; awaitPromise = $false }
    }
    
    $responseTarget = Invoke-CdpCommand -Command $cmdTarget -Timeout 30000
    if ($responseTarget.result -and $responseTarget.result.result -and $responseTarget.result.result.value) {
        $targetResult = $responseTarget.result.result.value | ConvertFrom-Json
        if ($targetResult.target) {
            $Scene = $targetResult.target.scene
            $TargetId = $targetResult.target.to
            Write-Host "Auto-detected target: scene=$Scene, to=$TargetId" -ForegroundColor Green
        }
    }
}

if (-not $TargetId) {
    Write-Host "Usage: .\send_image_file.ps1 -ImagePath 'C:\path\to\image.png' [-TargetId '40821608989'] [-Scene 'team']" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "No target detected. Please open a chat in WangShangLiao or specify -TargetId" -ForegroundColor Red
    exit 1
}

# If no image path, use a sample test image
if (-not $ImagePath) {
    Write-Host "No image path specified, using test image..." -ForegroundColor Yellow
    # Create a small test PNG
    $testImageBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAoAAAAKCAYAAACNMs+9AAAAFklEQVQYV2NkYGD4z4ABGMEMDE4JAQATCgEBbVMCOQAAAABJRU5ErkJggg=="
    $fileName = "test_$(Get-Date -Format 'HHmmss').png"
    $mimeType = "image/png"
} else {
    if (-not (Test-Path $ImagePath)) {
        Write-Host "Image file not found: $ImagePath" -ForegroundColor Red
        exit 1
    }
    
    $fileBytes = [System.IO.File]::ReadAllBytes($ImagePath)
    $testImageBase64 = [Convert]::ToBase64String($fileBytes)
    $fileName = [System.IO.Path]::GetFileName($ImagePath)
    $mimeType = Get-MimeType $ImagePath
    
    Write-Host "Image: $fileName ($($fileBytes.Length) bytes)" -ForegroundColor Green
}

Write-Host "Target: scene=$Scene, to=$TargetId" -ForegroundColor Cyan
Write-Host ""

# Escape the base64 for JavaScript
$base64Escaped = $testImageBase64 -replace '\\', '\\\\'

$script = @"
(async function() {
    var result = { success: false, stages: [] };
    
    try {
        var base64Data = '$base64Escaped';
        var fileName = '$fileName';
        var mimeType = '$mimeType';
        var scene = '$Scene';
        var to = '$TargetId';
        
        result.stages.push('Converting base64 to File');
        
        var byteString = atob(base64Data);
        var ab = new ArrayBuffer(byteString.length);
        var ia = new Uint8Array(ab);
        for (var i = 0; i < byteString.length; i++) {
            ia[i] = byteString.charCodeAt(i);
        }
        var blob = new Blob([ab], { type: mimeType });
        var file = new File([blob], fileName, { type: mimeType, lastModified: Date.now() });
        
        result.fileSize = file.size;
        result.stages.push('File created: ' + file.size + ' bytes');
        
        if (!window.nim || typeof window.nim.previewFile !== 'function') {
            result.error = 'NIM SDK not available';
            return JSON.stringify(result);
        }
        
        result.stages.push('Uploading to NOS via previewFile...');
        
        var previewResult = await new Promise(function(resolve) {
            window.nim.previewFile({
                type: 'image',
                blob: file,
                uploadprogress: function(obj) {
                    var pct = obj.percentage || Math.round((obj.loaded / obj.total) * 100);
                    console.log('[BOT] Upload: ' + pct + '%');
                },
                done: function(err, fileObj) {
                    if (err) {
                        resolve({ success: false, error: err.message || JSON.stringify(err) });
                    } else {
                        resolve({ success: true, fileObj: fileObj });
                    }
                }
            });
            setTimeout(function() { resolve({ success: false, error: 'Upload timeout' }); }, 60000);
        });
        
        if (!previewResult.success) {
            result.error = 'Upload failed: ' + previewResult.error;
            return JSON.stringify(result);
        }
        
        result.uploadedUrl = previewResult.fileObj.url;
        result.stages.push('Uploaded successfully');
        result.stages.push('Sending message...');
        
        var sendResult = await new Promise(function(resolve) {
            window.nim.sendFile({
                scene: scene,
                to: to,
                type: 'image',
                file: previewResult.fileObj,
                done: function(err, msg) {
                    if (err) {
                        resolve({ success: false, error: err.message || JSON.stringify(err) });
                    } else {
                        resolve({ success: true, msgId: msg ? msg.idClient : null });
                    }
                }
            });
            setTimeout(function() { resolve({ success: false, error: 'Send timeout' }); }, 30000);
        });
        
        if (sendResult.success) {
            result.success = true;
            result.msgId = sendResult.msgId;
            result.stages.push('Message sent: ' + sendResult.msgId);
        } else {
            result.error = 'Send failed: ' + sendResult.error;
        }
        
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})()
"@

$cmd = @{
    method = "Runtime.evaluate"
    params = @{ expression = $script; returnByValue = $true; awaitPromise = $true }
}

Write-Host "Sending..." -ForegroundColor Yellow
$response = Invoke-CdpCommand -Command $cmd -Timeout 120000

if ($response.result -and $response.result.result -and $response.result.result.value) {
    $sendResult = $response.result.result.value | ConvertFrom-Json
    Write-Host ""
    Write-Host "Result:" -ForegroundColor $(if ($sendResult.success) { "Green" } else { "Red" })
    Write-Host $response.result.result.value -ForegroundColor $(if ($sendResult.success) { "Green" } else { "Red" })
}
Write-Host ""

Write-Host "=== COMPLETE ===" -ForegroundColor Cyan

