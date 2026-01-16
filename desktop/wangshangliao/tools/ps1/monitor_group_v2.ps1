# 增强版群聊监控 - 解析自定义消息内容
# 天谕2.8(24h) - teamId: 21654357327
$ErrorActionPreference = 'Stop'
$cdpPort = 9333
$targetTeamId = "21654357327"
$pollIntervalMs = 800

$logDir = Join-Path $PSScriptRoot "Data\监控日志"
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir -Force | Out-Null }
$logFile = Join-Path $logDir "增强监控_$(Get-Date -Format 'yyyy-MM-dd_HHmmss').log"

function Write-Log {
    param([string]$Message, [string]$Color = "White")
    $timestamp = Get-Date -Format "HH:mm:ss.fff"
    $line = "[$timestamp] $Message"
    Write-Host $line -ForegroundColor $Color
    Add-Content -Path $logFile -Value $line -Encoding UTF8
}

function Decode-CustomMessage {
    param([string]$Base64Content)
    
    try {
        # URL-safe base64 转标准
        $standardB64 = $Base64Content.Replace('-', '+').Replace('_', '/')
        $mod = $standardB64.Length % 4
        if ($mod -gt 0) { $standardB64 += '=' * (4 - $mod) }
        
        $bytes = [Convert]::FromBase64String($standardB64)
        $text = [System.Text.Encoding]::UTF8.GetString($bytes)
        
        # 提取可读的中文和英文
        $matches = [regex]::Matches($text, '[\u4e00-\u9fff\u3000-\u303f\uff00-\uffef]+|[a-zA-Z0-9_]+|\{[^}]+\}')
        $readable = ($matches | ForEach-Object { $_.Value }) -join " "
        
        return $readable
    } catch {
        return ""
    }
}

function Invoke-Cdp {
    param([string]$Script)
    
    $response = Invoke-RestMethod -Uri "http://127.0.0.1:${cdpPort}/json" -TimeoutSec 5
    $wsUrl = ($response | Where-Object { $_.type -eq 'page' } | Select-Object -First 1).webSocketDebuggerUrl
    
    $ws = New-Object System.Net.WebSockets.ClientWebSocket
    $ct = [System.Threading.CancellationToken]::None
    $ws.ConnectAsync([Uri]$wsUrl, $ct).Wait(15000)
    
    $cmd = @{ id = 1; method = "Runtime.evaluate"; params = @{ expression = $Script; returnByValue = $true } }
    $json = $cmd | ConvertTo-Json -Depth 10 -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    $ws.SendAsync([ArraySegment[byte]]::new($bytes), [System.Net.WebSockets.WebSocketMessageType]::Text, $true, $ct).Wait(15000)
    
    $buffer = New-Object byte[] 2097152
    $result = New-Object System.Text.StringBuilder
    do {
        $segment = [ArraySegment[byte]]::new($buffer)
        $receiveTask = $ws.ReceiveAsync($segment, $ct)
        $receiveTask.Wait(15000) | Out-Null
        $received = $receiveTask.Result
        $result.Append([System.Text.Encoding]::UTF8.GetString($buffer, 0, $received.Count)) | Out-Null
    } while (-not $received.EndOfMessage)
    
    $ws.CloseAsync([System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure, "", $ct).Wait(5000)
    $ws.Dispose()
    
    $resp = $result.ToString() | ConvertFrom-Json
    if ($resp.result -and $resp.result.result) {
        return $resp.result.result.value
    }
    return $null
}

# 安装增强钩子
$installScript = @'
(function() {
    window.__monitorHooked = false;
    window.__monitorMessages = [];
    
    if (window.nim && window.nim.options) {
        var origOnmsg = window.nim.options.onmsg;
        window.nim.options.onmsg = function(msg) {
            var msgData = {
                time: Date.now(),
                scene: msg.scene || '',
                from: msg.from || '',
                to: msg.to || '',
                type: msg.type || '',
                text: msg.text || '',
                fromNick: (msg.user && (msg.user.groupMemberNick || msg.user.userNick)) || msg.fromNick || '',
                flow: msg.flow || '',
                attach: msg.attach ? JSON.stringify(msg.attach) : '',
                content: msg.content ? JSON.stringify(msg.content) : '',
                pushContent: msg.pushContent || ''
            };
            window.__monitorMessages.push(msgData);
            if (window.__monitorMessages.length > 200) {
                window.__monitorMessages.shift();
            }
            if (origOnmsg) origOnmsg(msg);
        };
        
        var origOnmsgs = window.nim.options.onmsgs;
        window.nim.options.onmsgs = function(msgs) {
            for (var i = 0; i < msgs.length; i++) {
                var msg = msgs[i];
                var msgData = {
                    time: Date.now(),
                    scene: msg.scene || '',
                    from: msg.from || '',
                    to: msg.to || '',
                    type: msg.type || '',
                    text: msg.text || '',
                    fromNick: (msg.user && (msg.user.groupMemberNick || msg.user.userNick)) || msg.fromNick || '',
                    flow: msg.flow || '',
                    attach: msg.attach ? JSON.stringify(msg.attach) : '',
                    content: msg.content ? JSON.stringify(msg.content) : '',
                    pushContent: msg.pushContent || ''
                };
                window.__monitorMessages.push(msgData);
            }
            if (window.__monitorMessages.length > 200) {
                window.__monitorMessages = window.__monitorMessages.slice(-100);
            }
            if (origOnmsgs) origOnmsgs(msgs);
        };
        
        window.__monitorHooked = true;
        return 'INSTALLED';
    }
    return 'FAIL';
})()
'@

$getScript = "(function() { var m = window.__monitorMessages || []; window.__monitorMessages = []; return JSON.stringify(m); })()"

# ========== 主程序 ==========
Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║            增强版群聊监控 - 解析自定义消息                          ║" -ForegroundColor Cyan
Write-Host "║  目标群: 天谕2.8(24h) (teamId: $targetTeamId)                  ║" -ForegroundColor Yellow
Write-Host "╚════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "日志: $logFile" -ForegroundColor DarkGray
Write-Host "按 Ctrl+C 停止" -ForegroundColor Gray
Write-Host ""

Write-Log "========== 增强监控启动 =========="

# 安装钩子
Write-Log "安装消息钩子..." "Yellow"
$result = Invoke-Cdp -Script $installScript
Write-Log "钩子安装结果: $result" $(if ($result -eq "INSTALLED") { "Green" } else { "Red" })

if ($result -ne "INSTALLED") {
    Write-Log "钩子安装失败，退出" "Red"
    exit 1
}

Write-Log ""
Write-Log "========== 开始监控 ==========" "Green"
Write-Log ""

$processedHashes = @{}
$messageCount = 0
$botAccounts = @{}
$startTime = Get-Date
$lastStatusTime = Get-Date

try {
    while ($true) {
        $data = Invoke-Cdp -Script $getScript
        
        if ($data -and $data -ne "[]") {
            try {
                $messages = $data | ConvertFrom-Json
                
                foreach ($msg in $messages) {
                    if ($msg.scene -ne "team") { continue }
                    
                    # 去重
                    $textPart = if ($msg.text.Length -gt 30) { $msg.text.Substring(0, 30) } elseif ($msg.content) { $msg.content.Substring(0, [Math]::Min(30, $msg.content.Length)) } else { "" }
                    $hash = "$($msg.time)|$($msg.from)|$textPart"
                    if ($processedHashes.ContainsKey($hash)) { continue }
                    $processedHashes[$hash] = $true
                    if ($processedHashes.Count -gt 3000) { $processedHashes.Clear() }
                    
                    $messageCount++
                    
                    # 格式化时间
                    $time = [DateTimeOffset]::FromUnixTimeMilliseconds($msg.time).LocalDateTime.ToString("HH:mm:ss")
                    $isTarget = ($msg.to -eq $targetTeamId)
                    $marker = if ($isTarget) { "★" } else { " " }
                    
                    # 解析消息内容
                    $displayContent = ""
                    $features = @()
                    
                    if ($msg.text) {
                        $displayContent = $msg.text
                    } elseif ($msg.content) {
                        # 尝试解析 content 中的 base64
                        if ($msg.content -match '"b"\s*:\s*"([^"]+)"') {
                            $b64 = $matches[1]
                            $decoded = Decode-CustomMessage -Base64Content $b64
                            if ($decoded) {
                                $displayContent = "[解码] $decoded"
                                $features += "自定义"
                            } else {
                                $displayContent = "[自定义消息 b64长度:$($b64.Length)]"
                            }
                        } else {
                            $displayContent = "[Content: $($msg.content.Substring(0, [Math]::Min(100, $msg.content.Length)))]"
                        }
                    } elseif ($msg.attach) {
                        if ($msg.attach -match '"b"\s*:\s*"([^"]+)"') {
                            $b64 = $matches[1]
                            $decoded = Decode-CustomMessage -Base64Content $b64
                            if ($decoded) {
                                $displayContent = "[解码] $decoded"
                                $features += "自定义"
                            } else {
                                $displayContent = "[Attach b64长度:$($b64.Length)]"
                            }
                        } else {
                            $displayContent = "[Attach: $($msg.attach.Substring(0, [Math]::Min(100, $msg.attach.Length)))]"
                        }
                    } else {
                        $displayContent = "[$($msg.type)]"
                    }
                    
                    # 检测特征
                    if ($msg.fromNick -match '^[0-9a-f]{32}$') { $features += "哈希昵称(机器人?)"; $botAccounts[$msg.from] = $msg.fromNick }
                    if ($displayContent -match '禁言|封盘|开盘|管理员') { $features += "管理操作" }
                    if ($displayContent -match '机器|客服') { $features += "机器人标识" }
                    if ($displayContent -match '\d+\+\d+\+\d+=\d+') { $features += "开奖" }
                    if ($displayContent -match '大单|小单|大双|小双|XD|DD|XS|DS') { $features += "下注" }
                    if ($displayContent -match '上分|下分|加分|减分') { $features += "分数" }
                    
                    $featureTag = if ($features.Count -gt 0) { " [" + ($features -join ",") + "]" } else { "" }
                    
                    # 截断显示
                    if ($displayContent.Length -gt 150) {
                        $displayContent = $displayContent.Substring(0, 150) + "..."
                    }
                    
                    # 颜色
                    $color = "Gray"
                    if ($isTarget) { $color = "Green" }
                    if ($features -contains "机器人标识" -or $features -contains "哈希昵称(机器人?)") { $color = "Magenta" }
                    if ($features -contains "管理操作") { $color = "Yellow" }
                    if ($features -contains "开奖") { $color = "Cyan" }
                    
                    Write-Log "$marker [$time] [$($msg.type)] $($msg.fromNick) ($($msg.from)): $displayContent$featureTag" $color
                }
            } catch {
                # 静默忽略解析错误
            }
        }
        
        # 状态报告
        if (((Get-Date) - $lastStatusTime).TotalSeconds -gt 60) {
            $duration = (Get-Date) - $startTime
            Write-Log "--- 状态: 运行 $($duration.ToString('hh\:mm\:ss')), 消息 $messageCount, 检测到机器人账号: $($botAccounts.Count) ---" "DarkYellow"
            if ($botAccounts.Count -gt 0) {
                Write-Log "    机器人账号: $($botAccounts.Keys -join ', ')" "DarkGray"
            }
            $lastStatusTime = Get-Date
        }
        
        Start-Sleep -Milliseconds $pollIntervalMs
    }
} catch {
    Write-Log "异常: $_" "Red"
} finally {
    $duration = (Get-Date) - $startTime
    Write-Log ""
    Write-Log "========== 监控结束 ==========" "Yellow"
    Write-Log "运行时长: $($duration.ToString('hh\:mm\:ss'))"
    Write-Log "总消息数: $messageCount"
    Write-Log "检测到机器人账号: $($botAccounts.Keys -join ', ')"
}

