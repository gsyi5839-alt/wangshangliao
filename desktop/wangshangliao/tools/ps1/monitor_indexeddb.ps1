# 终极群聊监控 - 直接从 IndexedDB 实时读取消息
# 这是最高级的方式 - 直接访问 NIM SDK 的本地数据库
$ErrorActionPreference = 'Stop'
$cdpPort = 9333
$targetTeamId = "21654357327"

$logDir = Join-Path $PSScriptRoot "Data\监控日志"
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir -Force | Out-Null }
$logFile = Join-Path $logDir "IndexedDB监控_$(Get-Date -Format 'yyyy-MM-dd_HHmmss').log"

function Write-Log {
    param([string]$Message, [string]$Color = "White")
    $timestamp = Get-Date -Format "HH:mm:ss.fff"
    $line = "[$timestamp] $Message"
    Write-Host $line -ForegroundColor $Color
    Add-Content -Path $logFile -Value $line -Encoding UTF8
}

Write-Host ""
Write-Host "╔═══════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
Write-Host "║          终极群聊监控 - IndexedDB 直接读取                                 ║" -ForegroundColor Magenta
Write-Host "║  目标群: 天谕2.8(24h) (teamId: $targetTeamId)                        ║" -ForegroundColor Yellow
Write-Host "║  数据源: nim-1948408648 -> msg1                                           ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Magenta
Write-Host ""
Write-Host "日志: $logFile" -ForegroundColor DarkGray
Write-Host "按 Ctrl+C 停止" -ForegroundColor Gray
Write-Host ""

Write-Log "========== IndexedDB 监控启动 =========="

# CDP 连接
$response = Invoke-RestMethod -Uri "http://127.0.0.1:${cdpPort}/json" -TimeoutSec 5
$wsUrl = ($response | Where-Object { $_.type -eq 'page' } | Select-Object -First 1).webSocketDebuggerUrl
Write-Log "CDP: $wsUrl" "Cyan"

$ws = New-Object System.Net.WebSockets.ClientWebSocket
$ct = [System.Threading.CancellationToken]::None
$ws.ConnectAsync([Uri]$wsUrl, $ct).Wait(30000)

$cmdId = 0
function Invoke-CdpAsync {
    param([string]$Method, [hashtable]$Params = @{}, [switch]$AwaitPromise)
    $script:cmdId++
    $cmd = @{ id = $script:cmdId; method = $Method; params = $Params }
    if ($AwaitPromise) { $cmd.params['awaitPromise'] = $true }
    $json = $cmd | ConvertTo-Json -Depth 10 -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    $ws.SendAsync([ArraySegment[byte]]::new($bytes), [System.Net.WebSockets.WebSocketMessageType]::Text, $true, $ct).Wait(10000) | Out-Null
    
    $buffer = New-Object byte[] 4194304
    $result = New-Object System.Text.StringBuilder
    do {
        $seg = [ArraySegment[byte]]::new($buffer)
        $task = $ws.ReceiveAsync($seg, $ct)
        $task.Wait(60000) | Out-Null
        $r = $task.Result
        $result.Append([System.Text.Encoding]::UTF8.GetString($buffer, 0, $r.Count)) | Out-Null
    } while (-not $r.EndOfMessage)
    
    return $result.ToString() | ConvertFrom-Json
}

# 获取最新消息的脚本 - 使用 time 索引获取最新的
$readScript = @'
(function() {
    return new Promise(function(resolve) {
        var targetTeamId = '21654357327';
        var lastTime = window.__lastMsgTime || 0;
        var result = { msgs: [], lastTime: lastTime, error: null };
        
        var request = indexedDB.open('nim-1948408648');
        request.onsuccess = function(event) {
            var db = event.target.result;
            
            try {
                var tx = db.transaction('msg1', 'readonly');
                var store = tx.objectStore('msg1');
                var index = store.index('time');
                
                // 使用 time 索引，获取 lastTime 之后的消息
                var range = lastTime > 0 ? IDBKeyRange.lowerBound(lastTime, true) : null;
                var cursor = index.openCursor(range, 'next');
                var msgs = [];
                var maxTime = lastTime;
                
                cursor.onsuccess = function(e) {
                    var c = e.target.result;
                    if (c) {
                        var msg = c.value;
                        if (msg.time > maxTime) maxTime = msg.time;
                        
                        // 只收集目标群的消息
                        if (msg.to === targetTeamId || msg.target === targetTeamId) {
                            // 解析 content 字段
                            var content = '';
                            var contentRaw = null;
                            
                            if (msg.content) {
                                try {
                                    contentRaw = typeof msg.content === 'string' ? 
                                        JSON.parse(msg.content) : msg.content;
                                    
                                    if (contentRaw.b) {
                                        // Base64 解码
                                        try {
                                            var b = contentRaw.b.replace(/-/g, '+').replace(/_/g, '/');
                                            while (b.length % 4) b += '=';
                                            var decoded = atob(b);
                                            
                                            // 提取可读字符
                                            var readable = [];
                                            var i = 0;
                                            while (i < decoded.length) {
                                                var c = decoded.charCodeAt(i);
                                                // UTF-8 解码
                                                if (c < 128) {
                                                    if (c >= 32 && c < 127) readable.push(decoded[i]);
                                                } else if (c >= 192 && c < 224 && i + 1 < decoded.length) {
                                                    var c2 = decoded.charCodeAt(i+1);
                                                    var codePoint = ((c & 0x1F) << 6) | (c2 & 0x3F);
                                                    readable.push(String.fromCharCode(codePoint));
                                                    i++;
                                                } else if (c >= 224 && c < 240 && i + 2 < decoded.length) {
                                                    var c2 = decoded.charCodeAt(i+1);
                                                    var c3 = decoded.charCodeAt(i+2);
                                                    var codePoint = ((c & 0x0F) << 12) | ((c2 & 0x3F) << 6) | (c3 & 0x3F);
                                                    readable.push(String.fromCharCode(codePoint));
                                                    i += 2;
                                                }
                                                i++;
                                            }
                                            content = readable.join('');
                                        } catch(e) {}
                                    }
                                } catch(e) {}
                            }
                            
                            if (!content && msg.text) content = msg.text;
                            
                            msgs.push({
                                time: msg.time,
                                from: msg.from,
                                to: msg.to,
                                type: msg.type,
                                text: content.substring(0, 300),
                                fromNick: msg.fromNick,
                                idClient: msg.idClient,
                                flow: msg.flow
                            });
                        }
                        c.continue();
                    } else {
                        window.__lastMsgTime = maxTime;
                        result.msgs = msgs;
                        result.lastTime = maxTime;
                        result.newMsgsCount = msgs.length;
                        db.close();
                        resolve(JSON.stringify(result));
                    }
                };
                
                cursor.onerror = function(e) {
                    result.error = 'Cursor error';
                    db.close();
                    resolve(JSON.stringify(result));
                };
            } catch(e) {
                result.error = e.message;
                db.close();
                resolve(JSON.stringify(result));
            }
        };
        
        request.onerror = function(e) {
            result.error = 'DB open error';
            resolve(JSON.stringify(result));
        };
    });
})()
'@

# 同时安装 JS 钩子作为补充
$hookScript = @'
(function() {
    window.__idbMsgs = [];
    if (window.nim && window.nim.options && !window.__idbHooked) {
        var orig = window.nim.options.onmsg;
        if (orig) {
            window.nim.options.onmsg = function(msg) {
                window.__idbMsgs.push({
                    time: Date.now(),
                    from: msg.from,
                    to: msg.to,
                    type: msg.type,
                    text: msg.text || '',
                    fromNick: (msg.user && msg.user.groupMemberNick) || msg.fromNick || '',
                    content: msg.content
                });
                if (window.__idbMsgs.length > 100) window.__idbMsgs.shift();
                return orig.apply(this, arguments);
            };
        }
        window.__idbHooked = true;
    }
    return 'OK';
})()
'@

Invoke-CdpAsync -Method "Runtime.evaluate" -Params @{ expression = $hookScript; returnByValue = $true } | Out-Null
Write-Log "JS 钩子已安装" "Green"

Write-Log ""
Write-Log "========== 开始监控 ==========" "Green"
Write-Log "(数据源: IndexedDB + JS钩子)" "DarkGray"
Write-Log ""

$msgCount = 0
$botAccounts = @{}
$startTime = Get-Date
$lastStatus = Get-Date
$processed = @{}

try {
    while ($true) {
        # 从 IndexedDB 读取新消息
        $r = Invoke-CdpAsync -Method "Runtime.evaluate" -Params @{ 
            expression = $readScript
            returnByValue = $true
            awaitPromise = $true
        }
        
        if ($r.result -and $r.result.result -and $r.result.result.value) {
            $data = $r.result.result.value | ConvertFrom-Json
            
            if ($data.msgs -and $data.msgs.Count -gt 0) {
                foreach ($msg in $data.msgs) {
                    # 去重
                    if ($msg.idClient -and $processed.ContainsKey($msg.idClient)) { continue }
                    if ($msg.idClient) { $processed[$msg.idClient] = $true }
                    if ($processed.Count -gt 5000) { $processed.Clear() }
                    
                    $msgCount++
                    
                    $time = [DateTimeOffset]::FromUnixTimeMilliseconds($msg.time).LocalDateTime.ToString("HH:mm:ss")
                    $nick = $msg.fromNick
                    $from = $msg.from
                    $text = $msg.text
                    
                    # 检测特征
                    $feat = @()
                    if ($nick -match '^[0-9a-f]{32}$') { 
                        $feat += "哈希昵称"
                        $botAccounts[$from] = $nick
                    }
                    if ($text -match '禁言|封盘|管理|全体') { $feat += "管理" }
                    if ($text -match '机器|客服|自动|进群') { $feat += "机器人" }
                    if ($text -match '\d+\+\d+\+\d+=\d+') { $feat += "开奖" }
                    if ($text -match '大小单双|龙虎|下注|投注') { $feat += "下注" }
                    if ($msg.type -eq 'custom') { $feat += "自定义" }
                    
                    $flowTag = if ($msg.flow -eq 'out') { "→发" } else { "←收" }
                    
                    $color = "White"
                    if ($feat -contains "机器人" -or $feat -contains "哈希昵称") { $color = "Magenta" }
                    if ($feat -contains "开奖") { $color = "Cyan" }
                    if ($feat -contains "管理") { $color = "Yellow" }
                    if ($feat -contains "下注") { $color = "Green" }
                    
                    $featTag = if ($feat.Count -gt 0) { " [" + ($feat -join ",") + "]" } else { "" }
                    $preview = if ($text.Length -gt 120) { $text.Substring(0, 120) + "..." } else { $text }
                    
                    Write-Log "★ [$time] $flowTag [$nick] ($from): $preview$featTag" $color
                }
            }
        }
        
        # 状态报告
        if (((Get-Date) - $lastStatus).TotalSeconds -gt 30) {
            $dur = (Get-Date) - $startTime
            Write-Log "--- 状态: 运行 $($dur.ToString('hh\:mm\:ss')), 消息 $msgCount, 机器人账号 $($botAccounts.Count) ---" "DarkYellow"
            if ($botAccounts.Count -gt 0) {
                Write-Log "    机器人: $($botAccounts.Keys -join ', ')" "DarkGray"
            }
            $lastStatus = Get-Date
        }
        
        Start-Sleep -Milliseconds 1000
    }
} catch {
    Write-Log "异常: $_" "Red"
} finally {
    try { $ws.CloseAsync([System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure, "", $ct).Wait(5000) } catch {}
    $ws.Dispose()
    
    $dur = (Get-Date) - $startTime
    Write-Log ""
    Write-Log "========== 监控结束 ==========" "Yellow"
    Write-Log "运行时长: $($dur.ToString('hh\:mm\:ss'))"
    Write-Log "消息数: $msgCount"
    Write-Log "机器人账号: $($botAccounts.Keys -join ', ')"
}

