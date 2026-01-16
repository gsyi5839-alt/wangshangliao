# Trigger image selection and monitor the upload process
# 触发图片选择并监控上传过程
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

Write-Host "=== TRIGGER IMAGE SELECTION ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Find and click the image/file button in chat toolbar
Write-Host "Step 1: Looking for image button in toolbar..." -ForegroundColor Yellow

$script1 = @'
(function() {
    var result = {
        found: false,
        clicked: false,
        buttons: [],
        message: ""
    };
    
    try {
        // Find all clickable elements that might be the image button
        // Usually these have icons like camera, image, file, etc.
        
        // Pattern 1: Look for icon buttons with specific class names
        var iconPatterns = [
            '[class*="icon-image"]',
            '[class*="icon-picture"]',
            '[class*="icon-file"]',
            '[class*="icon-photo"]',
            '[class*="image-icon"]',
            '[class*="file-icon"]',
            '[class*="toolbar"] [class*="icon"]',
            '[class*="toolbar"] svg',
            '[class*="toolbar"] i',
            '[class*="action-bar"] [class*="icon"]',
            '[class*="chat-footer"] [class*="icon"]',
            '[class*="msg-input"] [class*="icon"]'
        ];
        
        var foundElement = null;
        
        for (var i = 0; i < iconPatterns.length; i++) {
            var els = document.querySelectorAll(iconPatterns[i]);
            for (var j = 0; j < els.length; j++) {
                var el = els[j];
                var parent = el.parentElement;
                var title = el.title || el.getAttribute("data-title") || 
                           (parent ? (parent.title || parent.getAttribute("data-title")) : "") || "";
                var cls = el.className || "";
                
                result.buttons.push({
                    selector: iconPatterns[i],
                    className: (cls + "").substring(0, 60),
                    title: title,
                    tag: el.tagName
                });
                
                // Check if this looks like an image/file button
                if (title.indexOf("图片") >= 0 || title.indexOf("图像") >= 0 ||
                    title.indexOf("文件") >= 0 || title.indexOf("image") >= 0 ||
                    cls.indexOf("image") >= 0 || cls.indexOf("picture") >= 0 ||
                    cls.indexOf("file") >= 0 || cls.indexOf("photo") >= 0) {
                    foundElement = el;
                    result.found = true;
                    result.matchedTitle = title;
                    result.matchedClass = cls;
                    break;
                }
            }
            if (foundElement) break;
        }
        
        // If found, try to click it
        if (foundElement) {
            // Get clickable parent if the element itself isn't clickable
            var clickTarget = foundElement;
            while (clickTarget && clickTarget.tagName !== "BUTTON" && 
                   !clickTarget.onclick && clickTarget.parentElement) {
                clickTarget = clickTarget.parentElement;
            }
            
            clickTarget.click();
            result.clicked = true;
            result.message = "Image button clicked!";
        } else {
            result.message = "Image button not found. Try clicking manually and check console.";
        }
        
        // Also look for any input[type=file] that might have been dynamically created
        var fileInputs = document.querySelectorAll('input[type="file"]');
        result.fileInputCount = fileInputs.length;
        
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})()
'@

$cmd1 = @{
    method = "Runtime.evaluate"
    params = @{ expression = $script1; returnByValue = $true; awaitPromise = $false }
}

$response1 = Invoke-CdpCommand -Command $cmd1 -Timeout 60000
if ($response1.result -and $response1.result.result -and $response1.result.result.value) {
    Write-Host $response1.result.result.value -ForegroundColor Green
}
Write-Host ""

# Wait a moment for file dialog to potentially open
Start-Sleep -Milliseconds 500

# Step 2: Check if file input was created
Write-Host "Step 2: Checking for dynamically created file input..." -ForegroundColor Yellow

$script2 = @'
(function() {
    var result = {
        fileInputs: [],
        total: 0
    };
    
    try {
        var inputs = document.querySelectorAll('input[type="file"]');
        result.total = inputs.length;
        
        for (var i = 0; i < inputs.length; i++) {
            var inp = inputs[i];
            result.fileInputs.push({
                id: inp.id || "(none)",
                accept: inp.accept || "*",
                multiple: inp.multiple,
                className: inp.className.substring(0, 50),
                style: inp.style.cssText.substring(0, 50),
                isVisible: inp.offsetParent !== null
            });
            
            // If we find a file input, hook it
            if (!inp.__botHooked) {
                inp.__botHooked = true;
                inp.addEventListener("change", function(e) {
                    window.__lastSelectedFiles = Array.from(e.target.files).map(function(f) {
                        return { name: f.name, size: f.size, type: f.type };
                    });
                    console.log("[BOT] Files selected:", JSON.stringify(window.__lastSelectedFiles));
                });
            }
        }
        
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})()
'@

$cmd2 = @{
    method = "Runtime.evaluate"
    params = @{ expression = $script2; returnByValue = $true; awaitPromise = $false }
}

$response2 = Invoke-CdpCommand -Command $cmd2 -Timeout 60000
if ($response2.result -and $response2.result.result -and $response2.result.result.value) {
    Write-Host $response2.result.result.value -ForegroundColor Green
}
Write-Host ""

Write-Host "=== INSTRUCTIONS ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "If a file dialog didn't open automatically:" -ForegroundColor Yellow
Write-Host "1. In WangShangLiao, click the image/file icon manually" -ForegroundColor White
Write-Host "2. Select an image file" -ForegroundColor White
Write-Host "3. Run .\get_file_ops.ps1 to see what was captured" -ForegroundColor White
Write-Host ""
Write-Host "The hooks will capture:" -ForegroundColor Yellow
Write-Host "- previewFile calls (when file is selected)" -ForegroundColor Gray
Write-Host "- sendFile calls (when send is clicked)" -ForegroundColor Gray
Write-Host ""

