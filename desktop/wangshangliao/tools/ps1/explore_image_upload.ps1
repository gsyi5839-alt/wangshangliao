# Explore image upload and sending functionality in WangShangLiao
# 探索旺商聊图片上传和发送功能
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

Write-Host "=== EXPLORE IMAGE UPLOAD & SEND ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Explore NIM SDK file/image related APIs
Write-Host "Step 1: Exploring NIM SDK file/image APIs..." -ForegroundColor Yellow

$script1 = @'
(function() {
    var result = {
        nimFileMethods: [],
        nimImageMethods: [],
        nimSendMethods: [],
        nimPreviewMethods: [],
        options: {}
    };
    
    try {
        if (!window.nim) {
            result.error = "NIM SDK not found";
            return JSON.stringify(result, null, 2);
        }
        
        // Get all NIM methods
        var allMethods = [];
        for (var key in window.nim) {
            if (typeof window.nim[key] === "function") {
                allMethods.push(key);
            }
        }
        
        // Filter file related methods
        result.nimFileMethods = allMethods.filter(function(k) {
            return k.toLowerCase().indexOf("file") >= 0;
        });
        
        // Filter image related methods
        result.nimImageMethods = allMethods.filter(function(k) {
            return k.toLowerCase().indexOf("image") >= 0 || 
                   k.toLowerCase().indexOf("img") >= 0 ||
                   k.toLowerCase().indexOf("pic") >= 0;
        });
        
        // Filter send methods
        result.nimSendMethods = allMethods.filter(function(k) {
            return k.toLowerCase().indexOf("send") >= 0;
        });
        
        // Filter preview/upload methods
        result.nimPreviewMethods = allMethods.filter(function(k) {
            return k.toLowerCase().indexOf("preview") >= 0 ||
                   k.toLowerCase().indexOf("upload") >= 0;
        });
        
        // Check options for file handlers
        if (window.nim.options) {
            var optKeys = Object.keys(window.nim.options);
            result.options.allKeys = optKeys;
            result.options.fileRelated = optKeys.filter(function(k) {
                return k.toLowerCase().indexOf("file") >= 0 ||
                       k.toLowerCase().indexOf("upload") >= 0 ||
                       k.toLowerCase().indexOf("image") >= 0;
            });
        }
        
        // Check for previewFile method signature
        if (typeof window.nim.previewFile === "function") {
            result.hasPreviewFile = true;
            result.previewFileStr = window.nim.previewFile.toString().substring(0, 200);
        }
        
        // Check for sendFile method signature
        if (typeof window.nim.sendFile === "function") {
            result.hasSendFile = true;
            result.sendFileStr = window.nim.sendFile.toString().substring(0, 200);
        }
        
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
$resultValue = $null
if ($response1.result -and $response1.result.result -and $response1.result.result.value) {
    $resultValue = $response1.result.result.value
}
Write-Host "NIM SDK File/Image APIs:" -ForegroundColor Green
Write-Host $resultValue
Write-Host ""

# Step 2: Explore DOM for file input and send button
Write-Host "Step 2: Exploring DOM for file input elements..." -ForegroundColor Yellow

$script2 = @'
(function() {
    var result = {
        fileInputs: [],
        sendButtons: [],
        imageButtons: [],
        chatInputArea: null
    };
    
    try {
        // Find all file input elements
        var inputs = document.querySelectorAll('input[type="file"]');
        for (var i = 0; i < inputs.length; i++) {
            var inp = inputs[i];
            result.fileInputs.push({
                id: inp.id || "",
                className: inp.className || "",
                accept: inp.accept || "",
                parentClass: inp.parentElement ? inp.parentElement.className : ""
            });
        }
        
        // Find image/file buttons in chat toolbar
        var buttons = document.querySelectorAll('button, div[role="button"], span[role="button"]');
        for (var i = 0; i < buttons.length; i++) {
            var btn = buttons[i];
            var text = btn.innerText || btn.textContent || "";
            var cls = btn.className || "";
            var title = btn.title || btn.getAttribute("data-tooltip") || "";
            
            // Look for image/file related buttons
            if (cls.indexOf("image") >= 0 || cls.indexOf("file") >= 0 || 
                cls.indexOf("upload") >= 0 || cls.indexOf("emoji") >= 0 ||
                title.indexOf("图片") >= 0 || title.indexOf("文件") >= 0) {
                result.imageButtons.push({
                    tag: btn.tagName,
                    className: cls.substring(0, 100),
                    title: title,
                    text: text.substring(0, 50)
                });
            }
        }
        
        // Find send button
        var sendBtns = document.querySelectorAll('[class*="send"], [class*="Send"]');
        for (var i = 0; i < Math.min(sendBtns.length, 5); i++) {
            result.sendButtons.push({
                tag: sendBtns[i].tagName,
                className: sendBtns[i].className.substring(0, 100),
                text: (sendBtns[i].innerText || "").substring(0, 30)
            });
        }
        
        // Find chat input area
        var chatInput = document.querySelector('[class*="chat-input"], [class*="message-input"], [class*="editor"]');
        if (chatInput) {
            result.chatInputArea = {
                tag: chatInput.tagName,
                className: chatInput.className.substring(0, 100)
            };
        }
        
        // Find toolbar icons (usually SVG or icon fonts)
        var toolbar = document.querySelector('[class*="toolbar"], [class*="Toolbar"]');
        if (toolbar) {
            result.toolbarFound = true;
            result.toolbarClass = toolbar.className;
            var toolbarChildren = toolbar.children;
            result.toolbarChildCount = toolbarChildren.length;
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
$resultValue2 = $null
if ($response2.result -and $response2.result.result -and $response2.result.result.value) {
    $resultValue2 = $response2.result.result.value
}
Write-Host "DOM File Elements:" -ForegroundColor Green
Write-Host $resultValue2
Write-Host ""

# Step 3: Explore Pinia store for image/file sending
Write-Host "Step 3: Exploring Pinia store for image sending..." -ForegroundColor Yellow

$script3 = @'
(function() {
    var result = {
        sdkStoreMethods: [],
        appStoreMethods: [],
        sendImageMethod: null
    };
    
    try {
        var app = document.querySelector("#app");
        var gp = app && app.__vue_app__ && app.__vue_app__.config && app.__vue_app__.config.globalProperties;
        var pinia = gp && gp.$pinia;
        
        if (pinia && pinia._s) {
            // Check SDK store
            var sdkStore = pinia._s.get("sdk");
            if (sdkStore) {
                for (var key in sdkStore) {
                    if (typeof sdkStore[key] === "function") {
                        var keyLower = key.toLowerCase();
                        if (keyLower.indexOf("send") >= 0 || 
                            keyLower.indexOf("file") >= 0 || 
                            keyLower.indexOf("image") >= 0 ||
                            keyLower.indexOf("upload") >= 0) {
                            result.sdkStoreMethods.push(key);
                        }
                    }
                }
            }
            
            // Check App store
            var appStore = pinia._s.get("app");
            if (appStore) {
                for (var key in appStore) {
                    if (typeof appStore[key] === "function") {
                        var keyLower = key.toLowerCase();
                        if (keyLower.indexOf("send") >= 0 || 
                            keyLower.indexOf("file") >= 0 || 
                            keyLower.indexOf("image") >= 0 ||
                            keyLower.indexOf("upload") >= 0) {
                            result.appStoreMethods.push(key);
                        }
                    }
                }
            }
            
            // Look for current session to understand context
            if (appStore && appStore.currentSession) {
                result.currentSession = {
                    scene: appStore.currentSession.scene,
                    to: appStore.currentSession.to,
                    hasGroup: !!appStore.currentSession.group
                };
            }
        }
        
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})()
'@

$cmd3 = @{
    method = "Runtime.evaluate"
    params = @{ expression = $script3; returnByValue = $true; awaitPromise = $false }
}

$response3 = Invoke-CdpCommand -Command $cmd3 -Timeout 60000
$resultValue3 = $null
if ($response3.result -and $response3.result.result -and $response3.result.result.value) {
    $resultValue3 = $response3.result.result.value
}
Write-Host "Pinia Store Methods:" -ForegroundColor Green
Write-Host $resultValue3
Write-Host ""

# Step 4: Try to find and test sendFile API
Write-Host "Step 4: Testing sendFile API parameters..." -ForegroundColor Yellow

$script4 = @'
(function() {
    var result = {
        sendFileExists: false,
        sendFileParams: null,
        previewFileExists: false,
        testResult: null
    };
    
    try {
        if (window.nim) {
            result.sendFileExists = typeof window.nim.sendFile === "function";
            result.previewFileExists = typeof window.nim.previewFile === "function";
            
            // Try to get function signature by calling with invalid params
            // This will show us what params are expected
            if (result.sendFileExists) {
                try {
                    // Don't actually call it, just check if it exists
                    result.sendFileParams = "Expected: { scene, to, file, done }";
                } catch(e) {
                    result.sendFileError = e.message;
                }
            }
            
            // Check for blob/file handling
            result.hasBlobSupport = typeof Blob !== "undefined";
            result.hasFileReader = typeof FileReader !== "undefined";
            
            // Check if there's a way to trigger file selection
            var fileInputs = document.querySelectorAll('input[type="file"]');
            result.fileInputCount = fileInputs.length;
            
            if (fileInputs.length > 0) {
                var firstInput = fileInputs[0];
                result.firstFileInput = {
                    accept: firstInput.accept,
                    multiple: firstInput.multiple,
                    id: firstInput.id,
                    name: firstInput.name
                };
            }
        }
        
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})()
'@

$cmd4 = @{
    method = "Runtime.evaluate"
    params = @{ expression = $script4; returnByValue = $true; awaitPromise = $false }
}

$response4 = Invoke-CdpCommand -Command $cmd4 -Timeout 60000
$resultValue4 = $null
if ($response4.result -and $response4.result.result -and $response4.result.result.value) {
    $resultValue4 = $response4.result.result.value
}
Write-Host "SendFile API Test:" -ForegroundColor Green
Write-Host $resultValue4
Write-Host ""

# Step 5: Monitor file upload events
Write-Host "Step 5: Install file upload monitor hook..." -ForegroundColor Yellow

$script5 = @'
(function() {
    var result = { installed: false, message: "" };
    
    try {
        // Create storage for captured file operations
        window.__botFileOps = window.__botFileOps || [];
        
        // Hook sendFile if exists
        if (window.nim && typeof window.nim.sendFile === "function" && !window.__origSendFile) {
            window.__origSendFile = window.nim.sendFile;
            window.nim.sendFile = function(options) {
                var opData = {
                    time: Date.now(),
                    type: "sendFile",
                    scene: options.scene,
                    to: options.to,
                    fileType: options.type,
                    fileName: options.blob ? options.blob.name : (options.file ? options.file.name : "unknown")
                };
                window.__botFileOps.push(opData);
                console.log("[BOT] sendFile called:", JSON.stringify(opData));
                
                // Wrap done callback
                var origDone = options.done;
                options.done = function(err, msg) {
                    opData.result = err ? { error: err.message } : { success: true, msgId: msg && msg.idClient };
                    window.__botFileOps.push({ type: "sendFileResult", data: opData });
                    console.log("[BOT] sendFile result:", JSON.stringify(opData.result));
                    if (origDone) origDone(err, msg);
                };
                
                return window.__origSendFile.call(window.nim, options);
            };
            result.hookedSendFile = true;
        }
        
        // Hook previewFile if exists
        if (window.nim && typeof window.nim.previewFile === "function" && !window.__origPreviewFile) {
            window.__origPreviewFile = window.nim.previewFile;
            window.nim.previewFile = function(options) {
                var opData = {
                    time: Date.now(),
                    type: "previewFile",
                    fileType: options.type,
                    blob: options.blob ? { name: options.blob.name, size: options.blob.size, type: options.blob.type } : null
                };
                window.__botFileOps.push(opData);
                console.log("[BOT] previewFile called:", JSON.stringify(opData));
                return window.__origPreviewFile.call(window.nim, options);
            };
            result.hookedPreviewFile = true;
        }
        
        // Monitor file input changes
        var fileInputs = document.querySelectorAll('input[type="file"]');
        for (var i = 0; i < fileInputs.length; i++) {
            (function(input, idx) {
                if (!input.__botHooked) {
                    input.__botHooked = true;
                    input.addEventListener("change", function(e) {
                        var files = e.target.files;
                        for (var j = 0; j < files.length; j++) {
                            var f = files[j];
                            var opData = {
                                time: Date.now(),
                                type: "fileInputChange",
                                inputIndex: idx,
                                file: { name: f.name, size: f.size, type: f.type }
                            };
                            window.__botFileOps.push(opData);
                            console.log("[BOT] File selected:", JSON.stringify(opData));
                        }
                    });
                }
            })(fileInputs[i], i);
        }
        result.monitoredInputs = fileInputs.length;
        
        result.installed = true;
        result.message = "File operation hooks installed";
        
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})()
'@

$cmd5 = @{
    method = "Runtime.evaluate"
    params = @{ expression = $script5; returnByValue = $true; awaitPromise = $false }
}

$response5 = Invoke-CdpCommand -Command $cmd5 -Timeout 60000
$resultValue5 = $null
if ($response5.result -and $response5.result.result -and $response5.result.result.value) {
    $resultValue5 = $response5.result.result.value
}
Write-Host "File Monitor Hook:" -ForegroundColor Green
Write-Host $resultValue5
Write-Host ""

Write-Host "=== EXPLORATION COMPLETE ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Now try selecting and sending an image in WangShangLiao," -ForegroundColor Yellow
Write-Host "then run the following to see captured operations:" -ForegroundColor Yellow
Write-Host ""
Write-Host 'To get captured file operations, run:' -ForegroundColor White
Write-Host '  .\get_file_ops.ps1' -ForegroundColor Gray
Write-Host ""

