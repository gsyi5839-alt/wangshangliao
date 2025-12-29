# 持续监控群聊消息 - 群名称：天谕2.8(24h)，群聊号：333338888
# 用于观察其他注入程序的行为，完善运行日记
$ErrorActionPreference = 'Stop'
$cdpPort = 9333
$targetGroupId = "21654357327"  # 内部teamId
$targetGroupAccount = "3333338888"  # 显示的群账号
$targetGroupName = "天谕2.8(24h)"
$pollIntervalMs = 800  # 轮询间隔（毫秒）

# 日志文件路径
$logDir = Join-Path $PSScriptRoot "Data\监控日志"
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir -Force | Out-Null }
$logFile = Join-Path $logDir "群聊监控_$(Get-Date -Format 'yyyy-MM-dd').log"
$detailLogFile = Join-Path $logDir "详细日志_$(Get-Date -Format 'yyyy-MM-dd_HHmmss').json"

function Write-Log {
    param([string]$Message, [string]$Color = "White")
    $timestamp = Get-Date -Format "HH:mm:ss.fff"
    $line = "[$timestamp] $Message"
    Write-Host $line -ForegroundColor $Color
    Add-Content -Path $logFile -Value $line -Encoding UTF8
}

function Invoke-CdpCommand {
    param([hashtable]$Command, [int]$Timeout = 15000)
    $ws = $null
    try {
        $response = Invoke-RestMethod -Uri "http://127.0.0.1:${cdpPort}/json" -TimeoutSec 5
        $wsUrl = ($response | Where-Object { $_.type -eq 'page' } | Select-Object -First 1).webSocketDebuggerUrl
        if (-not $wsUrl) { throw "No WebSocket URL found" }
        
        $ws = New-Object System.Net.WebSockets.ClientWebSocket
        $ws.Options.KeepAliveInterval = [TimeSpan]::FromSeconds(30)
        $ct = [System.Threading.CancellationToken]::None
        $ws.ConnectAsync([Uri]$wsUrl, $ct).Wait($Timeout)
        
        $Command['id'] = [System.Threading.Interlocked]::Increment([ref]$script:cmdId)
        $json = $Command | ConvertTo-Json -Depth 10 -Compress
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
        $segment = [ArraySegment[byte]]::new($bytes)
        $ws.SendAsync($segment, [System.Net.WebSockets.WebSocketMessageType]::Text, $true, $ct).Wait($Timeout)
        
        $buffer = New-Object byte[] 1048576
        $result = New-Object System.Text.StringBuilder
        do {
            $segment = [ArraySegment[byte]]::new($buffer)
            $receiveTask = $ws.ReceiveAsync($segment, $ct)
            if (-not $receiveTask.Wait($Timeout)) { throw "Receive timeout" }
            $received = $receiveTask.Result
            $result.Append([System.Text.Encoding]::UTF8.GetString($buffer, 0, $received.Count)) | Out-Null
        } while (-not $received.EndOfMessage)
        
        return $result.ToString() | ConvertFrom-Json
    } finally {
        if ($ws -and $ws.State -eq 'Open') { 
            try { $ws.CloseAsync([System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure, "", [System.Threading.CancellationToken]::None).Wait(3000) } catch {}
        }
        if ($ws) { $ws.Dispose() }
    }
}

function Install-MessageHook {
    Write-Log "正在安装 NIM SDK 消息钩子..." "Yellow"
    
    $script = @'
(function() {
    var result = { installed: false, message: '', hookedEvents: 0 };
    
    try {
        if (!window.nim) {
            result.message = 'NIM SDK not found';
            return JSON.stringify(result);
        }
        
        // 初始化消息存储
        window.__monitorMessages = window.__monitorMessages || [];
        window.__monitorLastClear = window.__monitorLastClear || Date.now();
        
        // Hook nim.options.onmsg
        if (window.nim.options && !window.__monitorHooked) {
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
                    idClient: msg.idClient || '',
                    target: msg.target || '',
                    sessionId: msg.sessionId || '',
                    attach: msg.attach ? JSON.stringify(msg.attach).substring(0, 800) : '',
                    content: msg.content ? JSON.stringify(msg.content).substring(0, 500) : '',
                    pushContent: msg.pushContent || ''
                };
                window.__monitorMessages.push(msgData);
                if (window.__monitorMessages.length > 500) {
                    window.__monitorMessages = window.__monitorMessages.slice(-300);
                }
                if (origOnmsg) origOnmsg(msg);
            };
            result.hookedEvents++;
        }
        
        // Hook nim.options.onmsgs
        if (window.nim.options && !window.__monitorHooked) {
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
                        idClient: msg.idClient || '',
                        target: msg.target || '',
                        sessionId: msg.sessionId || '',
                        attach: msg.attach ? JSON.stringify(msg.attach).substring(0, 800) : '',
                        content: msg.content ? JSON.stringify(msg.content).substring(0, 500) : '',
                        pushContent: msg.pushContent || ''
                    };
                    window.__monitorMessages.push(msgData);
                }
                if (window.__monitorMessages.length > 500) {
                    window.__monitorMessages = window.__monitorMessages.slice(-300);
                }
                if (origOnmsgs) origOnmsgs(msgs);
            };
            result.hookedEvents++;
        }
        
        window.__monitorHooked = true;
        result.installed = true;
        result.message = 'Message hook installed successfully';
        
    } catch(e) {
        result.message = 'Error: ' + e.message;
    }
    
    return JSON.stringify(result);
})()
'@
    
    $cmd = @{
        method = "Runtime.evaluate"
        params = @{ expression = $script; returnByValue = $true }
    }
    
    $response = Invoke-CdpCommand -Command $cmd
    $resultJson = $null
    if ($response.result -and $response.result.result -and $response.result.result.value) {
        $resultJson = $response.result.result.value | ConvertFrom-Json
    }
    
    if ($resultJson -and $resultJson.installed) {
        Write-Log "✓ 消息钩子安装成功！(hooked events: $($resultJson.hookedEvents))" "Green"
        return $true
    } else {
        Write-Log "✗ 消息钩子安装失败: $($resultJson.message)" "Red"
        return $false
    }
}

function Get-MonitoredMessages {
    $script = @'
(function() {
    var msgs = window.__monitorMessages || [];
    window.__monitorMessages = [];
    return JSON.stringify(msgs);
})()
'@
    
    $cmd = @{
        method = "Runtime.evaluate"
        params = @{ expression = $script; returnByValue = $true }
    }
    
    try {
        $response = Invoke-CdpCommand -Command $cmd -Timeout 10000
        if ($response.result -and $response.result.result -and $response.result.result.value) {
            return $response.result.result.value | ConvertFrom-Json
        }
    } catch {
        # 静默处理错误
    }
    return @()
}

function Get-GroupList {
    Write-Log "正在获取群列表..." "Yellow"
    
    # 使用正确的 appStore.groupList 路径
    $script = @'
(function() {
    var result = { groups: [], error: null };
    try {
        var app = document.querySelector("#app");
        var gp = app && app.__vue_app__ && app.__vue_app__.config && app.__vue_app__.config.globalProperties;
        var pinia = gp && gp.$pinia;
        var appStore = pinia && pinia._s && pinia._s.get && pinia._s.get("app");
        
        if (appStore && appStore.groupList) {
            var list = appStore.groupList;
            for (var i = 0; i < list.length; i++) {
                var g = list[i];
                result.groups.push({
                    teamId: g.groupCloudId || g.teamId || g.id || '',
                    name: g.groupName || g.name || g.teamName || '',
                    memberNum: g.memberNum || g.memberCount || 0,
                    owner: g.owner || ''
                });
            }
        }
        
        // 也检查 currSession
        if (appStore && appStore.currSession) {
            result.currSession = {
                scene: appStore.currSession.scene || '',
                to: appStore.currSession.to || '',
                id: appStore.currSession.id || ''
            };
        }
    } catch(e) {
        result.error = e.message;
    }
    return JSON.stringify(result);
})()
'@
    
    $cmd = @{
        method = "Runtime.evaluate"
        params = @{ expression = $script; returnByValue = $true }
    }
    
    try {
        $response = Invoke-CdpCommand -Command $cmd
        if ($response.result -and $response.result.result -and $response.result.result.value) {
            $data = $response.result.result.value | ConvertFrom-Json
            return $data
        }
    } catch {}
    return @{ groups = @(); currSession = $null }
}

function Format-MessageOutput {
    param($msg)
    
    $timeStr = ""
    if ($msg.time) {
        $epoch = [DateTimeOffset]::FromUnixTimeMilliseconds($msg.time)
        $timeStr = $epoch.LocalDateTime.ToString("HH:mm:ss")
    }
    
    $flowIcon = if ($msg.flow -eq "out") { "→发" } else { "←收" }
    $nick = if ($msg.fromNick) { $msg.fromNick } else { $msg.from }
    
    # 处理不同消息类型
    $content = $msg.text
    $typeTag = ""
    
    if ([string]::IsNullOrEmpty($content)) {
        # 消息内容为空，根据类型显示
        switch ($msg.type) {
            "image" { $content = "[图片]"; $typeTag = " 📷" }
            "audio" { $content = "[语音]"; $typeTag = " 🎤" }
            "video" { $content = "[视频]"; $typeTag = " 🎬" }
            "file" { $content = "[文件]"; $typeTag = " 📎" }
            "custom" { 
                if ($msg.attach) {
                    $content = "[自定义: $($msg.attach)]"
                } elseif ($msg.content) {
                    $content = "[自定义: $($msg.content)]"
                } elseif ($msg.pushContent) {
                    $content = "[自定义: $($msg.pushContent)]"
                } else {
                    $content = "[自定义消息]"
                }
                $typeTag = " 🔧"
            }
            "notification" { $content = "[通知]"; $typeTag = " 📢" }
            default { 
                $content = if ($msg.attach) { "[附件: $($msg.attach)]" } else { "[空消息 type=$($msg.type)]" }
            }
        }
    }
    
    $textPreview = if ($content.Length -gt 120) { $content.Substring(0, 120) + "..." } else { $content }
    
    # 检测可能的机器人特征
    $botIndicators = @()
    if ($content -match '^\[.*?\]') { $botIndicators += "模板头" }
    if ($content -match '\d+\+\d+\+\d+=\d+') { $botIndicators += "开奖格式" }
    if ($content -match '(大单|小单|大双|小双|XD|DD|XS|DS|xd|dd|xs|ds)') { $botIndicators += "下注关键词" }
    if ($content -match '(上分|下分|加分|减分|充值|提现)') { $botIndicators += "分数操作" }
    if ($content -match '(账单|结算|汇总|盈亏|流水)') { $botIndicators += "账单" }
    if ($content -match '(封盘|开盘|停止下注)') { $botIndicators += "封盘" }
    if ($content -match '第\d+期') { $botIndicators += "期号" }
    if ($content -match '倒计时|\d+秒') { $botIndicators += "倒计时" }
    if ($content -match '(机器人|自动|BOT)') { $botIndicators += "机器人" }
    if ($content.Length -gt 200) { $botIndicators += "长消息" }
    
    $botTag = if ($botIndicators.Count -gt 0) { " [特征:$($botIndicators -join ',')]" } else { "" }
    
    return "[$timeStr] $flowIcon [$nick] ($($msg.from))$typeTag : $textPreview$botTag"
}

function Save-MessageDetail {
    param($msg)
    $entry = @{
        timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss.fff")
        raw = $msg
    }
    $json = $entry | ConvertTo-Json -Depth 5 -Compress
    Add-Content -Path $detailLogFile -Value $json -Encoding UTF8
}

# ========== 主程序 ==========
$script:cmdId = 0
$processedHashes = @{}

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║              群聊消息监控器 - 观察注入程序行为                    ║" -ForegroundColor Cyan
Write-Host "╠══════════════════════════════════════════════════════════════════╣" -ForegroundColor Cyan
Write-Host "║  目标群名: $targetGroupName                                              ║" -ForegroundColor Yellow
Write-Host "║  目标群号: $targetGroupId                                            ║" -ForegroundColor Yellow
Write-Host "║  CDP端口:  $cdpPort                                                     ║" -ForegroundColor Yellow
Write-Host "║  轮询间隔: ${pollIntervalMs}ms                                                   ║" -ForegroundColor Yellow
Write-Host "╚══════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "日志文件: $logFile" -ForegroundColor DarkGray
Write-Host "详细日志: $detailLogFile" -ForegroundColor DarkGray
Write-Host ""
Write-Host "按 Ctrl+C 停止监控" -ForegroundColor Gray
Write-Host ""

Write-Log "========== 监控启动 =========="
Write-Log "目标群: $targetGroupName ($targetGroupId)"

# 先列出所有群
$groupData = Get-GroupList
if ($groupData.groups -and $groupData.groups.Count -gt 0) {
    Write-Log "检测到 $($groupData.groups.Count) 个群:" "Cyan"
    foreach ($g in $groupData.groups) {
        $marker = if ($g.teamId -eq $targetGroupId -or $g.name -like "*$targetGroupName*" -or $g.name -like "*天谕*") { " ★ 目标群" } else { "" }
        Write-Log "  - $($g.name) (ID: $($g.teamId), 成员: $($g.memberNum))$marker" "Gray"
    }
    
    if ($groupData.currSession) {
        Write-Log "当前会话: scene=$($groupData.currSession.scene), to=$($groupData.currSession.to)" "Cyan"
    }
} else {
    Write-Log "未检测到群列表，将监控所有群消息" "Yellow"
}

# 安装消息钩子
$hookRetry = 0
while ($hookRetry -lt 3) {
    if (Install-MessageHook) { break }
    $hookRetry++
    Write-Log "钩子安装失败，3秒后重试 ($hookRetry/3)..." "Red"
    Start-Sleep -Seconds 3
}
if ($hookRetry -ge 3) {
    Write-Log "钩子安装多次失败，退出" "Red"
    exit 1
}

Write-Log ""
Write-Log "========== 开始监控消息 ==========" "Green"
Write-Log "(监控所有群聊消息，目标群用 ★ 标记)" "Gray"
Write-Log ""

$messageCount = 0
$targetGroupMsgCount = 0
$startTime = Get-Date
$lastStatusTime = Get-Date

try {
    while ($true) {
        $messages = Get-MonitoredMessages
        
        foreach ($msg in $messages) {
            # 只处理群聊消息
            if ($msg.scene -ne "team") { continue }
            
            # 生成去重哈希
            $textPart = if ($msg.text.Length -gt 50) { $msg.text.Substring(0, 50) } else { $msg.text }
            $hash = "$($msg.time)|$($msg.from)|$textPart"
            if ($processedHashes.ContainsKey($hash)) { continue }
            $processedHashes[$hash] = $true
            
            # 防止哈希表无限增长
            if ($processedHashes.Count -gt 5000) {
                $processedHashes.Clear()
            }
            
            $messageCount++
            
            # 保存详细日志
            Save-MessageDetail -msg $msg
            
            # 判断是否是目标群
            $isTargetGroup = ($msg.to -eq $targetGroupId) -or ($msg.from -eq $targetGroupId) -or ($msg.to -like "*$targetGroupId*")
            
            # 格式化输出
            $output = Format-MessageOutput -msg $msg
            
            if ($isTargetGroup) {
                $targetGroupMsgCount++
                # 目标群消息用高亮颜色
                $color = if ($msg.flow -eq "out") { "Cyan" } else { "Green" }
                Write-Log "★ $output" $color
            } else {
                # 其他群消息
                $groupInfo = "群:$($msg.to)"
                Write-Log "  [$groupInfo] $output" "DarkGray"
            }
        }
        
        # 每30秒输出一次状态
        if (((Get-Date) - $lastStatusTime).TotalSeconds -gt 30) {
            $duration = (Get-Date) - $startTime
            Write-Log "--- 状态: 运行 $($duration.ToString('hh\:mm\:ss')), 总消息 $messageCount, 目标群消息 $targetGroupMsgCount ---" "DarkYellow"
            $lastStatusTime = Get-Date
        }
        
        Start-Sleep -Milliseconds $pollIntervalMs
    }
} catch {
    Write-Log "监控异常: $_" "Red"
} finally {
    $duration = (Get-Date) - $startTime
    Write-Log ""
    Write-Log "========== 监控结束 ==========" "Yellow"
    Write-Log "运行时长: $($duration.ToString('hh\:mm\:ss'))"
    Write-Log "总消息数: $messageCount 条"
    Write-Log "目标群消息: $targetGroupMsgCount 条"
    Write-Log "详细日志已保存到: $detailLogFile"
}
