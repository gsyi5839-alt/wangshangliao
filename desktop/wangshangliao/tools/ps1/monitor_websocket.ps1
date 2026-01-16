# 终极监控 - 直接监听 CDP Network.webSocketFrameReceived 事件
# 这是最底层的方式，能捕获所有 WebSocket 通信
$ErrorActionPreference = 'Stop'
$cdpPort = 9333
$targetTeamId = "21654357327"

$logDir = Join-Path $PSScriptRoot "Data\监控日志"
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir -Force | Out-Null }
$logFile = Join-Path $logDir "WebSocket监控_$(Get-Date -Format 'yyyy-MM-dd_HHmmss').log"

function Write-Log {
    param([string]$Message, [string]$Color = "White")
    $timestamp = Get-Date -Format "HH:mm:ss.fff"
    $line = "[$timestamp] $Message"
    Write-Host $line -ForegroundColor $Color
    Add-Content -Path $logFile -Value $line -Encoding UTF8
}

Write-Host ""
Write-Host "╔═══════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║        终极监控 - CDP WebSocket Frame 直接监听                        ║" -ForegroundColor Cyan
Write-Host "║  目标群: 天谕2.8(24h) (teamId: $targetTeamId)                    ║" -ForegroundColor Yellow
Write-Host "╚═══════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "日志: $logFile" -ForegroundColor DarkGray
Write-Host ""

Write-Log "========== WebSocket帧监控启动 =========="

# 获取 CDP WebSocket URL
$response = Invoke-RestMethod -Uri "http://127.0.0.1:${cdpPort}/json" -TimeoutSec 5
$wsUrl = ($response | Where-Object { $_.type -eq 'page' } | Select-Object -First 1).webSocketDebuggerUrl
Write-Log "CDP WebSocket: $wsUrl" "Cyan"

# 建立持久 WebSocket 连接
$ws = New-Object System.Net.WebSockets.ClientWebSocket
$ws.Options.KeepAliveInterval = [TimeSpan]::FromSeconds(30)
$ct = [System.Threading.CancellationToken]::None
$ws.ConnectAsync([Uri]$wsUrl, $ct).Wait(30000)
Write-Log "CDP 连接已建立" "Green"

$cmdId = 0

function Send-CdpCommand {
    param([hashtable]$Command, [switch]$NoWait)
    $script:cmdId++
    $Command['id'] = $script:cmdId
    $json = $Command | ConvertTo-Json -Depth 10 -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    $ws.SendAsync([ArraySegment[byte]]::new($bytes), [System.Net.WebSockets.WebSocketMessageType]::Text, $true, $ct).Wait(15000)
    
    if ($NoWait) { return $null }
    
    # 等待响应
    $buffer = New-Object byte[] 1048576
    $result = New-Object System.Text.StringBuilder
    do {
        $segment = [ArraySegment[byte]]::new($buffer)
        $receiveTask = $ws.ReceiveAsync($segment, $ct)
        $receiveTask.Wait(15000) | Out-Null
        $received = $receiveTask.Result
        $result.Append([System.Text.Encoding]::UTF8.GetString($buffer, 0, $received.Count)) | Out-Null
    } while (-not $received.EndOfMessage)
    
    return $result.ToString() | ConvertFrom-Json
}

# 启用 Network 域
Write-Log "启用 CDP Network 域..." "Yellow"
$r = Send-CdpCommand -Command @{ method = "Network.enable" }
Write-Log "Network 域已启用" "Green"

# 启用 Runtime 域
$r = Send-CdpCommand -Command @{ method = "Runtime.enable" }
Write-Log "Runtime 域已启用" "Green"

# 同时安装 JS 钩子作为备份
Write-Log "安装 JS 消息钩子..." "Yellow"
$hookScript = @'
(function() {
    window.__wsFrameMsgs = [];
    
    // Hook nim.options callbacks
    if (window.nim && window.nim.options) {
        var callbacks = ['onmsg', 'onmsgs', 'onsysmsg', 'oncustomsysmsg'];
        callbacks.forEach(function(cb) {
            var orig = window.nim.options[cb];
            if (orig && !window['__hooked_' + cb]) {
                window.nim.options[cb] = function() {
                    var args = Array.from(arguments);
                    window.__wsFrameMsgs.push({
                        time: Date.now(),
                        source: cb,
                        data: JSON.stringify(args).substring(0, 3000)
                    });
                    if (window.__wsFrameMsgs.length > 200) {
                        window.__wsFrameMsgs = window.__wsFrameMsgs.slice(-100);
                    }
                    return orig.apply(this, arguments);
                };
                window['__hooked_' + cb] = true;
            }
        });
    }
    
    return 'OK';
})()
'@
$r = Send-CdpCommand -Command @{ method = "Runtime.evaluate"; params = @{ expression = $hookScript; returnByValue = $true } }
Write-Log "JS 钩子已安装" "Green"

Write-Log ""
Write-Log "========== 开始监听 ==========" "Green"
Write-Log "(监听 CDP 事件 + JS 回调钩子)" "DarkGray"
Write-Log ""

$getScript = "(function() { var m = window.__wsFrameMsgs || []; window.__wsFrameMsgs = []; return JSON.stringify(m); })()"

$messageCount = 0
$frameCount = 0
$startTime = Get-Date
$lastStatusTime = Get-Date

# 接收缓冲区
$buffer = New-Object byte[] 4194304

try {
    while ($true) {
        # 检查是否有数据可读
        $segment = [ArraySegment[byte]]::new($buffer)
        $receiveTask = $ws.ReceiveAsync($segment, $ct)
        
        # 非阻塞等待 500ms
        if ($receiveTask.Wait(500)) {
            $received = $receiveTask.Result
            $data = [System.Text.Encoding]::UTF8.GetString($buffer, 0, $received.Count)
            
            try {
                $cdpEvent = $data | ConvertFrom-Json
                
                # 处理 CDP 事件
                if ($cdpEvent.method) {
                    $method = $cdpEvent.method
                    
                    # WebSocket 帧接收事件
                    if ($method -eq "Network.webSocketFrameReceived") {
                        $frameCount++
                        $params = $cdpEvent.params
                        $payload = $params.response.payloadData
                        
                        if ($payload -and $payload.Length -gt 0) {
                            # 检查是否包含消息关键词
                            if ($payload -match '"type"\s*:\s*"(text|custom|image|notification)"' -or
                                $payload -match '"scene"\s*:\s*"(team|p2p)"' -or
                                $payload -match '"cmd"\s*:\s*\d+') {
                                
                                $messageCount++
                                $time = Get-Date -Format "HH:mm:ss"
                                
                                # 提取关键信息
                                $from = if ($payload -match '"from"\s*:\s*"([^"]+)"') { $matches[1] } else { "" }
                                $to = if ($payload -match '"to"\s*:\s*"([^"]+)"') { $matches[1] } else { "" }
                                $text = if ($payload -match '"text"\s*:\s*"([^"]*)"') { $matches[1] } else { "" }
                                $msgType = if ($payload -match '"type"\s*:\s*"([^"]+)"') { $matches[1] } else { "" }
                                
                                $isTarget = ($to -eq $targetTeamId -or $from -eq $targetTeamId)
                                $marker = if ($isTarget) { "★" } else { " " }
                                $color = if ($isTarget) { "Green" } else { "Gray" }
                                
                                $preview = $payload.Substring(0, [Math]::Min(200, $payload.Length))
                                Write-Log "$marker [$time] [WS帧] $from -> $to [$msgType]: $preview..." $color
                            }
                        }
                    }
                    # WebSocket 创建事件
                    elseif ($method -eq "Network.webSocketCreated") {
                        Write-Log "[CDP] WebSocket 创建: $($cdpEvent.params.url)" "Cyan"
                    }
                    # WebSocket 关闭事件
                    elseif ($method -eq "Network.webSocketClosed") {
                        Write-Log "[CDP] WebSocket 关闭" "Yellow"
                    }
                }
            } catch {
                # 非 JSON 数据，忽略
            }
        }
        
        # 同时检查 JS 钩子捕获的消息
        try {
            # 发送获取消息命令
            $script:cmdId++
            $getCmd = @{ id = $script:cmdId; method = "Runtime.evaluate"; params = @{ expression = $getScript; returnByValue = $true } }
            $json = $getCmd | ConvertTo-Json -Depth 10 -Compress
            $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
            $ws.SendAsync([ArraySegment[byte]]::new($bytes), [System.Net.WebSockets.WebSocketMessageType]::Text, $true, $ct).Wait(5000)
        } catch {}
        
        # 状态报告
        if (((Get-Date) - $lastStatusTime).TotalSeconds -gt 30) {
            $duration = (Get-Date) - $startTime
            Write-Log "--- 状态: 运行 $($duration.ToString('hh\:mm\:ss')), WS帧 $frameCount, 消息 $messageCount ---" "DarkYellow"
            $lastStatusTime = Get-Date
        }
    }
} catch {
    Write-Log "异常: $_" "Red"
} finally {
    try { $ws.CloseAsync([System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure, "", $ct).Wait(5000) } catch {}
    try { $ws.Dispose() } catch {}
    
    $duration = (Get-Date) - $startTime
    Write-Log ""
    Write-Log "========== 监控结束 ==========" "Yellow"
    Write-Log "运行时长: $($duration.ToString('hh\:mm\:ss'))"
    Write-Log "WS帧数: $frameCount"
    Write-Log "消息数: $messageCount"
}

