# 高级群聊监控 - 使用 CDP Network/Runtime 直接监听
# 直接拦截 WebSocket 帧 + NIM protocol 层
$ErrorActionPreference = 'Stop'
$cdpPort = 9333
$targetTeamId = "21654357327"

$logDir = Join-Path $PSScriptRoot "Data\监控日志"
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir -Force | Out-Null }
$logFile = Join-Path $logDir "高级监控_$(Get-Date -Format 'yyyy-MM-dd_HHmmss').log"

function Write-Log {
    param([string]$Message, [string]$Color = "White")
    $timestamp = Get-Date -Format "HH:mm:ss.fff"
    $line = "[$timestamp] $Message"
    Write-Host $line -ForegroundColor $Color
    Add-Content -Path $logFile -Value $line -Encoding UTF8
}

function Decode-Base64 {
    param([string]$Base64Content)
    try {
        $standardB64 = $Base64Content.Replace('-', '+').Replace('_', '/')
        $mod = $standardB64.Length % 4
        if ($mod -gt 0) { $standardB64 += '=' * (4 - $mod) }
        $bytes = [Convert]::FromBase64String($standardB64)
        $text = [System.Text.Encoding]::UTF8.GetString($bytes)
        $matches = [regex]::Matches($text, '[\u4e00-\u9fff\u3000-\u303f\uff00-\uffef]+|[a-zA-Z0-9_]+|\{[^}]+\}')
        return ($matches | ForEach-Object { $_.Value }) -join " "
    } catch { return "" }
}

Write-Host ""
Write-Host "╔═══════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║             高级群聊监控 - CDP WebSocket + NIM Protocol               ║" -ForegroundColor Cyan
Write-Host "║  目标群: 天谕2.8(24h) (teamId: $targetTeamId)                    ║" -ForegroundColor Yellow
Write-Host "╚═══════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "日志: $logFile" -ForegroundColor DarkGray
Write-Host ""

Write-Log "========== 高级监控启动 =========="

# 获取 CDP WebSocket URL
$response = Invoke-RestMethod -Uri "http://127.0.0.1:${cdpPort}/json" -TimeoutSec 5
$wsUrl = ($response | Where-Object { $_.type -eq 'page' } | Select-Object -First 1).webSocketDebuggerUrl
Write-Log "CDP WebSocket: $wsUrl" "Cyan"

# 建立持久 WebSocket 连接
$ws = New-Object System.Net.WebSockets.ClientWebSocket
$ws.Options.KeepAliveInterval = [TimeSpan]::FromSeconds(30)
$ct = [System.Threading.CancellationToken]::None
$ws.ConnectAsync([Uri]$wsUrl, $ct).Wait(30000)

$cmdId = 0
function Send-CdpCommand {
    param([hashtable]$Command)
    $script:cmdId++
    $Command['id'] = $script:cmdId
    $json = $Command | ConvertTo-Json -Depth 10 -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    $ws.SendAsync([ArraySegment[byte]]::new($bytes), [System.Net.WebSockets.WebSocketMessageType]::Text, $true, $ct).Wait(15000)
    
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

# 启用 Network 域来监听 WebSocket 帧
Write-Log "启用 CDP Network 域..." "Yellow"
$r = Send-CdpCommand -Command @{ method = "Network.enable" }

# 启用 Runtime 域
$r = Send-CdpCommand -Command @{ method = "Runtime.enable" }

# 注入高级消息监听器 - 直接 hook NIM protocol 层
Write-Log "注入高级消息监听器..." "Yellow"
$hookScript = @'
(function() {
    var result = { hooked: [], error: null };
    
    try {
        // 清理旧的监听
        window.__advancedMsgs = [];
        window.__advancedMsgId = 0;
        
        // 方法1: Hook nim.protocol 层 (最底层)
        if (window.nim && window.nim.protocol) {
            var origEmit = window.nim.protocol.emit;
            if (origEmit && !window.__protocolHooked) {
                window.nim.protocol.emit = function(event, data) {
                    if (event === 'msg' || event === 'msgs' || event === 'sysmsg' || 
                        event === 'customsysmsg' || event === 'notification') {
                        window.__advancedMsgs.push({
                            id: ++window.__advancedMsgId,
                            time: Date.now(),
                            source: 'protocol.emit',
                            event: event,
                            data: JSON.stringify(data).substring(0, 2000)
                        });
                    }
                    return origEmit.apply(this, arguments);
                };
                window.__protocolHooked = true;
                result.hooked.push('nim.protocol.emit');
            }
        }
        
        // 方法2: Hook nim.message 层
        if (window.nim && window.nim.message) {
            var msgObj = window.nim.message;
            var origOnMsg = msgObj.onMsg || msgObj.onmsg;
            if (origOnMsg && !window.__messageHooked) {
                var hookFn = function(msg) {
                    window.__advancedMsgs.push({
                        id: ++window.__advancedMsgId,
                        time: Date.now(),
                        source: 'nim.message',
                        event: 'onMsg',
                        data: JSON.stringify(msg).substring(0, 2000)
                    });
                    return origOnMsg.apply(this, arguments);
                };
                if (msgObj.onMsg) msgObj.onMsg = hookFn;
                if (msgObj.onmsg) msgObj.onmsg = hookFn;
                window.__messageHooked = true;
                result.hooked.push('nim.message.onMsg');
            }
        }
        
        // 方法3: Hook EventEmitter (如果 nim 是 EventEmitter)
        if (window.nim && window.nim.on && !window.__emitterHooked) {
            var events = ['msg', 'msgs', 'sysmsg', 'customsysmsg', 'notification', 'teamMsg', 'p2pMsg'];
            events.forEach(function(evt) {
                window.nim.on(evt, function(data) {
                    window.__advancedMsgs.push({
                        id: ++window.__advancedMsgId,
                        time: Date.now(),
                        source: 'nim.on',
                        event: evt,
                        data: JSON.stringify(data).substring(0, 2000)
                    });
                });
            });
            window.__emitterHooked = true;
            result.hooked.push('nim.on(events)');
        }
        
        // 方法4: Proxy 包装 nim.options 回调
        if (window.nim && window.nim.options && !window.__optionsProxied) {
            var callbacks = ['onmsg', 'onmsgs', 'onsysmsg', 'oncustomsysmsg', 'onbroadcastmsg', 'onProxyMsg'];
            callbacks.forEach(function(cb) {
                var orig = window.nim.options[cb];
                if (orig) {
                    window.nim.options[cb] = function() {
                        window.__advancedMsgs.push({
                            id: ++window.__advancedMsgId,
                            time: Date.now(),
                            source: 'nim.options.' + cb,
                            event: cb,
                            data: JSON.stringify(Array.from(arguments)).substring(0, 2000)
                        });
                        return orig.apply(this, arguments);
                    };
                    result.hooked.push('nim.options.' + cb);
                }
            });
            window.__optionsProxied = true;
        }
        
        // 方法5: Hook Pinia SDK store 的消息发送方法
        var app = document.querySelector("#app");
        var gp = app && app.__vue_app__ && app.__vue_app__.config && app.__vue_app__.config.globalProperties;
        var pinia = gp && gp.$pinia;
        var sdkStore = pinia && pinia._s && pinia._s.get && pinia._s.get("sdk");
        
        if (sdkStore && !window.__sdkStoreHooked) {
            // Hook sendNimMsg
            if (sdkStore.sendNimMsg) {
                var origSendNimMsg = sdkStore.sendNimMsg.bind(sdkStore);
                sdkStore.sendNimMsg = function() {
                    window.__advancedMsgs.push({
                        id: ++window.__advancedMsgId,
                        time: Date.now(),
                        source: 'sdkStore.sendNimMsg',
                        event: 'send',
                        data: JSON.stringify(Array.from(arguments)).substring(0, 2000)
                    });
                    return origSendNimMsg.apply(this, arguments);
                };
                result.hooked.push('sdkStore.sendNimMsg');
            }
            
            // Hook sendNimAutoReplyMsg
            if (sdkStore.sendNimAutoReplyMsg) {
                var origAutoReply = sdkStore.sendNimAutoReplyMsg.bind(sdkStore);
                sdkStore.sendNimAutoReplyMsg = function() {
                    window.__advancedMsgs.push({
                        id: ++window.__advancedMsgId,
                        time: Date.now(),
                        source: 'sdkStore.sendNimAutoReplyMsg',
                        event: 'autoReply',
                        data: JSON.stringify(Array.from(arguments)).substring(0, 2000)
                    });
                    return origAutoReply.apply(this, arguments);
                };
                result.hooked.push('sdkStore.sendNimAutoReplyMsg');
            }
            window.__sdkStoreHooked = true;
        }
        
        result.totalHooks = result.hooked.length;
        
    } catch(e) {
        result.error = e.message + '\n' + e.stack;
    }
    
    return JSON.stringify(result, null, 2);
})()
'@

$hookResult = Send-CdpCommand -Command @{
    method = "Runtime.evaluate"
    params = @{ expression = $hookScript; returnByValue = $true }
}

if ($hookResult.result -and $hookResult.result.result -and $hookResult.result.result.value) {
    $hookData = $hookResult.result.result.value | ConvertFrom-Json
    Write-Log "已安装 $($hookData.totalHooks) 个高级钩子:" "Green"
    foreach ($h in $hookData.hooked) {
        Write-Log "  ✓ $h" "Cyan"
    }
    if ($hookData.error) {
        Write-Log "  ⚠ 错误: $($hookData.error)" "Yellow"
    }
}

Write-Log ""
Write-Log "========== 开始高级监控 ==========" "Green"
Write-Log "(监听: protocol层 + message层 + EventEmitter + options回调 + SDK store)" "DarkGray"
Write-Log ""

# 获取消息的脚本
$getScript = @'
(function() {
    var msgs = window.__advancedMsgs || [];
    window.__advancedMsgs = [];
    return JSON.stringify(msgs);
})()
'@

$processedIds = @{}
$messageCount = 0
$startTime = Get-Date
$lastStatusTime = Get-Date

try {
    while ($true) {
        # 获取高级监听捕获的消息
        $getResult = Send-CdpCommand -Command @{
            method = "Runtime.evaluate"
            params = @{ expression = $getScript; returnByValue = $true }
        }
        
        if ($getResult.result -and $getResult.result.result -and $getResult.result.result.value) {
            $msgsJson = $getResult.result.result.value
            if ($msgsJson -and $msgsJson -ne "[]") {
                try {
                    $messages = $msgsJson | ConvertFrom-Json
                    
                    foreach ($msg in $messages) {
                        # 去重
                        if ($processedIds.ContainsKey($msg.id)) { continue }
                        $processedIds[$msg.id] = $true
                        if ($processedIds.Count -gt 5000) { $processedIds.Clear() }
                        
                        $messageCount++
                        
                        $time = [DateTimeOffset]::FromUnixTimeMilliseconds($msg.time).LocalDateTime.ToString("HH:mm:ss")
                        
                        # 解析消息数据
                        $content = ""
                        $from = ""
                        $to = ""
                        $msgType = ""
                        $features = @()
                        
                        try {
                            $data = $msg.data | ConvertFrom-Json
                            
                            # 处理数组格式 (如 onmsgs)
                            if ($data -is [array] -and $data.Count -gt 0) {
                                $data = $data[0]
                            }
                            
                            # 提取字段
                            if ($data.from) { $from = $data.from }
                            if ($data.to) { $to = $data.to }
                            if ($data.type) { $msgType = $data.type }
                            if ($data.text) { $content = $data.text }
                            
                            # 处理 content 字段 (base64)
                            if (-not $content -and $data.content) {
                                $contentObj = $data.content
                                if ($contentObj -is [string]) {
                                    try { $contentObj = $contentObj | ConvertFrom-Json } catch {}
                                }
                                if ($contentObj.b) {
                                    $decoded = Decode-Base64 -Base64Content $contentObj.b
                                    if ($decoded) { 
                                        $content = "[解码] $decoded"
                                        $features += "base64"
                                    }
                                }
                            }
                            
                            # 处理 attach 字段
                            if (-not $content -and $data.attach) {
                                $attachObj = $data.attach
                                if ($attachObj -is [string]) {
                                    try { $attachObj = $attachObj | ConvertFrom-Json } catch {}
                                }
                                if ($attachObj.b) {
                                    $decoded = Decode-Base64 -Base64Content $attachObj.b
                                    if ($decoded) { 
                                        $content = "[解码] $decoded"
                                        $features += "base64"
                                    }
                                }
                            }
                            
                            # fromNick
                            $fromNick = ""
                            if ($data.fromNick) { $fromNick = $data.fromNick }
                            elseif ($data.user -and $data.user.groupMemberNick) { $fromNick = $data.user.groupMemberNick }
                            
                        } catch {
                            $content = $msg.data.Substring(0, [Math]::Min(200, $msg.data.Length))
                        }
                        
                        # 检测特征
                        if ($fromNick -match '^[0-9a-f]{32}$') { $features += "哈希昵称" }
                        if ($content -match '禁言|封盘|开盘|管理员') { $features += "管理" }
                        if ($content -match '机器|客服|自动') { $features += "机器人" }
                        if ($content -match '\d+\+\d+\+\d+=\d+') { $features += "开奖" }
                        
                        $isTarget = ($to -eq $targetTeamId -or $from -eq $targetTeamId)
                        $marker = if ($isTarget) { "★" } else { " " }
                        
                        # 颜色
                        $color = "Gray"
                        if ($isTarget) { $color = "Green" }
                        if ($features -contains "机器人" -or $features -contains "哈希昵称") { $color = "Magenta" }
                        if ($features -contains "管理") { $color = "Yellow" }
                        if ($features -contains "开奖") { $color = "Cyan" }
                        
                        $featureTag = if ($features.Count -gt 0) { " [" + ($features -join ",") + "]" } else { "" }
                        $contentPreview = if ($content.Length -gt 120) { $content.Substring(0, 120) + "..." } else { $content }
                        
                        Write-Log "$marker [$time] [$($msg.source)] [$($msg.event)] $fromNick ($from) -> $to [$msgType]: $contentPreview$featureTag" $color
                    }
                } catch {
                    # 静默
                }
            }
        }
        
        # 状态报告
        if (((Get-Date) - $lastStatusTime).TotalSeconds -gt 60) {
            $duration = (Get-Date) - $startTime
            Write-Log "--- 状态: 运行 $($duration.ToString('hh\:mm\:ss')), 捕获消息 $messageCount ---" "DarkYellow"
            $lastStatusTime = Get-Date
        }
        
        Start-Sleep -Milliseconds 500
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
    Write-Log "捕获消息: $messageCount"
}

