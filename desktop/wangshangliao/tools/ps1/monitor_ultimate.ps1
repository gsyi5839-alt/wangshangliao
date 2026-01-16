# ████████████████████████████████████████████████████████████████████████████
# █                 终极群聊监控 V2.0 - IndexedDB 直读                        █
# █  技术: CDP + IndexedDB + TextDecoder + UTF-8 解码                          █
# █  数据源: nim-{account} -> msg1                                             █
# ████████████████████████████████████████████████████████████████████████████
$ErrorActionPreference = 'Stop'
$cdpPort = 9333
$targetTeamId = "21654357327"

$logDir = Join-Path $PSScriptRoot "Data\监控日志"
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir -Force | Out-Null }
$logFile = Join-Path $logDir "终极监控_$(Get-Date -Format 'yyyy-MM-dd_HHmmss').log"
$botFile = Join-Path $logDir "机器人账号.txt"

function Write-Log {
    param([string]$Message, [string]$Color = "White")
    $timestamp = Get-Date -Format "HH:mm:ss.fff"
    $line = "[$timestamp] $Message"
    Write-Host $line -ForegroundColor $Color
    Add-Content -Path $logFile -Value $line -Encoding UTF8
}

Write-Host ""
Write-Host "██████████████████████████████████████████████████████████████████████████" -ForegroundColor Magenta
Write-Host "██                                                                      ██" -ForegroundColor Magenta
Write-Host "██          终极群聊监控 V2.0 - IndexedDB 直读                          ██" -ForegroundColor Yellow
Write-Host "██                                                                      ██" -ForegroundColor Magenta
Write-Host "██  目标群: 天谕2.8(24h)                                                ██" -ForegroundColor Cyan
Write-Host "██  teamId: $targetTeamId                                         ██" -ForegroundColor Cyan
Write-Host "██  数据源: IndexedDB nim-* -> msg1                                     ██" -ForegroundColor Green
Write-Host "██                                                                      ██" -ForegroundColor Magenta
Write-Host "██████████████████████████████████████████████████████████████████████████" -ForegroundColor Magenta
Write-Host ""
Write-Host "日志: $logFile" -ForegroundColor DarkGray
Write-Host "机器人记录: $botFile" -ForegroundColor DarkGray
Write-Host "按 Ctrl+C 停止" -ForegroundColor Gray
Write-Host ""

Write-Log "========== 终极监控 V2.0 启动 =========="

# CDP 连接
$response = Invoke-RestMethod -Uri "http://127.0.0.1:${cdpPort}/json" -TimeoutSec 5
$wsUrl = ($response | Where-Object { $_.type -eq 'page' } | Select-Object -First 1).webSocketDebuggerUrl
Write-Log "CDP: $wsUrl" "Cyan"

$ws = New-Object System.Net.WebSockets.ClientWebSocket
$ct = [System.Threading.CancellationToken]::None
$ws.ConnectAsync([Uri]$wsUrl, $ct).Wait(30000)

$cmdId = 0
function Invoke-Cdp {
    param([string]$Script)
    $script:cmdId++
    $cmd = @{ id = $script:cmdId; method = "Runtime.evaluate"; params = @{ expression = $Script; returnByValue = $true; awaitPromise = $true } }
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
    
    $resp = $result.ToString() | ConvertFrom-Json
    if ($resp.result -and $resp.result.result -and $resp.result.result.value) {
        return $resp.result.result.value
    }
    return $null
}

# 初始化：获取数据库名和设置lastTime
$initScript = @'
(function() {
    return new Promise(function(resolve) {
        indexedDB.databases().then(function(dbs) {
            var nimDb = dbs.find(function(db) { return db.name && db.name.indexOf('nim-') === 0; });
            if (nimDb) {
                window.__nimDbName = nimDb.name;
                window.__lastMsgTime = 0;
                resolve(JSON.stringify({ dbName: nimDb.name }));
            } else {
                resolve(JSON.stringify({ error: 'No NIM database found' }));
            }
        });
    });
})()
'@

$initResult = Invoke-Cdp -Script $initScript | ConvertFrom-Json
if ($initResult.error) {
    Write-Log "错误: $($initResult.error)" "Red"
    exit
}
Write-Log "数据库: $($initResult.dbName)" "Green"

# 读取新消息的脚本
$readScript = @'
(function() {
    return new Promise(function(resolve) {
        var targetTeamId = '###TARGET###';
        var lastTime = window.__lastMsgTime || 0;
        var result = { msgs: [], newCount: 0 };
        
        var request = indexedDB.open(window.__nimDbName);
        request.onsuccess = function(event) {
            var db = event.target.result;
            var tx = db.transaction('msg1', 'readonly');
            var store = tx.objectStore('msg1');
            var index = store.index('time');
            var range = lastTime > 0 ? IDBKeyRange.lowerBound(lastTime, true) : null;
            var cursor = index.openCursor(range, 'next');
            var msgs = [];
            var maxTime = lastTime;
            
            cursor.onsuccess = function(e) {
                var c = e.target.result;
                if (c) {
                    var msg = c.value;
                    if (msg.time > maxTime) maxTime = msg.time;
                    
                    if (msg.to === targetTeamId) {
                        var textContent = '';
                        
                        // 解码 content
                        if (msg.content) {
                            try {
                                var contentObj = typeof msg.content === 'string' ? 
                                    JSON.parse(msg.content) : msg.content;
                                
                                if (contentObj.b) {
                                    var b = contentObj.b.replace(/-/g, '+').replace(/_/g, '/');
                                    while (b.length % 4) b += '=';
                                    var decoded = atob(b);
                                    
                                    var bytes = new Uint8Array(decoded.length);
                                    for (var i = 0; i < decoded.length; i++) {
                                        bytes[i] = decoded.charCodeAt(i);
                                    }
                                    
                                    // TextDecoder 解码
                                    var text = new TextDecoder('utf-8', {fatal: false}).decode(bytes);
                                    var chineseMatch = text.match(/[\u4e00-\u9fff]+/g);
                                    if (chineseMatch) {
                                        textContent = chineseMatch.join(' ');
                                    }
                                }
                            } catch(e) {}
                        }
                        
                        if (!textContent && msg.text) textContent = msg.text;
                        
                        msgs.push({
                            time: msg.time,
                            from: msg.from,
                            nick: msg.fromNick || '',
                            type: msg.type,
                            text: textContent.substring(0, 300),
                            flow: msg.flow,
                            idClient: msg.idClient
                        });
                    }
                    c.continue();
                } else {
                    window.__lastMsgTime = maxTime;
                    result.msgs = msgs;
                    result.newCount = msgs.length;
                    db.close();
                    resolve(JSON.stringify(result));
                }
            };
        };
        
        request.onerror = function() {
            resolve(JSON.stringify({ error: 'DB error' }));
        };
    });
})()
'@.Replace('###TARGET###', $targetTeamId)

Write-Log ""
Write-Log "========== 开始实时监控 ==========" "Green"
Write-Log ""

$msgCount = 0
$botAccounts = @{}
$startTime = Get-Date
$lastStatus = Get-Date
$processed = @{}

try {
    while ($true) {
        $json = Invoke-Cdp -Script $readScript
        if ($json) {
            $data = $json | ConvertFrom-Json
            
            if ($data.msgs -and $data.msgs.Count -gt 0) {
                foreach ($msg in $data.msgs) {
                    if ($msg.idClient -and $processed.ContainsKey($msg.idClient)) { continue }
                    if ($msg.idClient) { $processed[$msg.idClient] = $true }
                    if ($processed.Count -gt 5000) { $processed.Clear() }
                    
                    $msgCount++
                    
                    $time = [DateTimeOffset]::FromUnixTimeMilliseconds($msg.time).LocalDateTime.ToString("HH:mm:ss")
                    $nick = $msg.nick
                    $from = $msg.from
                    $text = $msg.text
                    $flow = if ($msg.flow -eq 'out') { "→发" } else { "←收" }
                    
                    # 特征检测
                    $feat = @()
                    $isBot = $false
                    
                    if ($nick -match '^[0-9a-f]{32}$') { 
                        $feat += "🤖机器人"
                        $isBot = $true
                        $botAccounts[$from] = @{ nick = $nick; lastSeen = (Get-Date) }
                    }
                    
                    if ($text -match '禁言') { $feat += "🔇禁言" }
                    if ($text -match '管理员') { $feat += "👮管理" }
                    if ($text -match '机器|客服|进群') { $feat += "🤖欢迎" }
                    if ($text -match '\d+\+\d+\+\d+=\d+') { $feat += "🎰开奖" }
                    if ($text -match '大小单双|龙虎') { $feat += "📊下注" }
                    
                    # 颜色
                    $color = "White"
                    if ($isBot) { $color = "Magenta" }
                    if ($feat -contains "🎰开奖") { $color = "Cyan" }
                    if ($feat -contains "🔇禁言" -or $feat -contains "👮管理") { $color = "Yellow" }
                    
                    $featTag = if ($feat.Count -gt 0) { " " + ($feat -join " ") } else { "" }
                    $preview = if ($text.Length -gt 100) { $text.Substring(0, 100) + "..." } else { $text }
                    
                    Write-Log "★ [$time] $flow [$nick] ($from): $preview$featTag" $color
                }
            }
        }
        
        # 状态报告
        if (((Get-Date) - $lastStatus).TotalSeconds -gt 30) {
            $dur = (Get-Date) - $startTime
            Write-Log ""
            Write-Log "═══════════════════════════════════════════════════" "DarkYellow"
            Write-Log "  状态报告 - 运行 $($dur.ToString('hh\:mm\:ss'))" "DarkYellow"
            Write-Log "  消息总数: $msgCount" "DarkYellow"
            Write-Log "  识别机器人: $($botAccounts.Count) 个" "DarkYellow"
            
            if ($botAccounts.Count -gt 0) {
                Write-Log "  机器人账号:" "DarkYellow"
                foreach ($bot in $botAccounts.GetEnumerator()) {
                    Write-Log "    - $($bot.Key) (昵称哈希: $($bot.Value.nick))" "Magenta"
                }
                
                # 保存机器人信息
                $botInfo = $botAccounts.GetEnumerator() | ForEach-Object { "$($_.Key),$($_.Value.nick)" }
                $botInfo | Set-Content -Path $botFile -Encoding UTF8
            }
            
            Write-Log "═══════════════════════════════════════════════════" "DarkYellow"
            Write-Log ""
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
    Write-Log "████████████████████████████████████████████████████████████████" "Yellow"
    Write-Log "█                     监控结束                                  █" "Yellow"
    Write-Log "█  运行时长: $($dur.ToString('hh\:mm\:ss'))                                         █" "Yellow"
    Write-Log "█  消息总数: $msgCount                                              █" "Yellow"
    Write-Log "█  机器人数: $($botAccounts.Count)                                               █" "Yellow"
    Write-Log "████████████████████████████████████████████████████████████████" "Yellow"
    
    if ($botAccounts.Count -gt 0) {
        Write-Log ""
        Write-Log "发现的机器人账号:"
        foreach ($bot in $botAccounts.GetEnumerator()) {
            Write-Log "  账号: $($bot.Key)"
            Write-Log "  昵称哈希: $($bot.Value.nick)"
        }
    }
}

