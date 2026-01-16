# 统一监控 - 结合多种方式持续监控群聊
# 1. CDP Network WebSocket 帧
# 2. NIM SDK 回调钩子
# 3. Pinia store 变化

$ErrorActionPreference = 'Stop'
$cdpPort = 9333
$targetTeamId = "21654357327"
$targetGroupName = "天谕2.8"

$logDir = Join-Path $PSScriptRoot "Data\监控日志"
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir -Force | Out-Null }
$logFile = Join-Path $logDir "统一监控_$(Get-Date -Format 'yyyy-MM-dd_HHmmss').log"

function Write-Log {
    param([string]$Message, [string]$Color = "White")
    $timestamp = Get-Date -Format "HH:mm:ss.fff"
    $line = "[$timestamp] $Message"
    Write-Host $line -ForegroundColor $Color
    Add-Content -Path $logFile -Value $line -Encoding UTF8
}

function Decode-B64 {
    param([string]$b64)
    try {
        $std = $b64.Replace('-', '+').Replace('_', '/')
        $mod = $std.Length % 4
        if ($mod -gt 0) { $std += '=' * (4 - $mod) }
        $bytes = [Convert]::FromBase64String($std)
        $text = [System.Text.Encoding]::UTF8.GetString($bytes)
        $m = [regex]::Matches($text, '[\u4e00-\u9fff]+|[a-zA-Z0-9_]+|\{[^}]+\}')
        return ($m | ForEach-Object { $_.Value }) -join " "
    } catch { return "" }
}

Write-Host ""
Write-Host "╔═══════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                  统一群聊监控 - 多层次监听                                 ║" -ForegroundColor Cyan
Write-Host "║  目标群: $targetGroupName (teamId: $targetTeamId)                              ║" -ForegroundColor Yellow
Write-Host "╚═══════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "日志: $logFile" -ForegroundColor DarkGray
Write-Host "按 Ctrl+C 停止" -ForegroundColor Gray
Write-Host ""

Write-Log "========== 统一监控启动 =========="

# CDP 连接
$response = Invoke-RestMethod -Uri "http://127.0.0.1:${cdpPort}/json" -TimeoutSec 5
$wsUrl = ($response | Where-Object { $_.type -eq 'page' } | Select-Object -First 1).webSocketDebuggerUrl
Write-Log "CDP: $wsUrl" "Cyan"

$ws = New-Object System.Net.WebSockets.ClientWebSocket
$ct = [System.Threading.CancellationToken]::None
$ws.ConnectAsync([Uri]$wsUrl, $ct).Wait(30000)

$cmdId = 0
function Invoke-Cdp {
    param([string]$Method, [hashtable]$Params = @{})
    $script:cmdId++
    $cmd = @{ id = $script:cmdId; method = $Method; params = $Params }
    $json = $cmd | ConvertTo-Json -Depth 10 -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    $ws.SendAsync([ArraySegment[byte]]::new($bytes), [System.Net.WebSockets.WebSocketMessageType]::Text, $true, $ct).Wait(10000) | Out-Null
    
    $buffer = New-Object byte[] 2097152
    $result = New-Object System.Text.StringBuilder
    do {
        $seg = [ArraySegment[byte]]::new($buffer)
        $task = $ws.ReceiveAsync($seg, $ct)
        $task.Wait(10000) | Out-Null
        $r = $task.Result
        $result.Append([System.Text.Encoding]::UTF8.GetString($buffer, 0, $r.Count)) | Out-Null
    } while (-not $r.EndOfMessage)
    
    return $result.ToString() | ConvertFrom-Json
}

# 启用域
Invoke-Cdp -Method "Network.enable" | Out-Null
Invoke-Cdp -Method "Runtime.enable" | Out-Null
Write-Log "CDP 域已启用" "Green"

# 安装统一钩子
$hookScript = @'
(function() {
    // 消息队列
    window.__unifiedMsgs = window.__unifiedMsgs || [];
    window.__unifiedId = window.__unifiedId || 0;
    
    function addMsg(source, data) {
        window.__unifiedMsgs.push({
            id: ++window.__unifiedId,
            time: Date.now(),
            source: source,
            data: typeof data === 'string' ? data : JSON.stringify(data)
        });
        if (window.__unifiedMsgs.length > 300) {
            window.__unifiedMsgs = window.__unifiedMsgs.slice(-150);
        }
    }
    
    // 1. Hook nim.options 回调
    if (window.nim && window.nim.options) {
        ['onmsg', 'onmsgs', 'onsysmsg', 'oncustomsysmsg', 'onbroadcastmsg'].forEach(function(cb) {
            var orig = window.nim.options[cb];
            if (orig && !window['__unified_' + cb]) {
                window.nim.options[cb] = function() {
                    addMsg('nim.' + cb, Array.from(arguments));
                    return orig.apply(this, arguments);
                };
                window['__unified_' + cb] = true;
            }
        });
    }
    
    // 2. Hook SDK store 方法
    try {
        var app = document.querySelector("#app");
        var pinia = app && app.__vue_app__ && app.__vue_app__.config.globalProperties.$pinia;
        var sdk = pinia && pinia._s.get("sdk");
        if (sdk && !window.__unified_sdk) {
            if (sdk.sendNimMsg) {
                var orig = sdk.sendNimMsg.bind(sdk);
                sdk.sendNimMsg = function() { addMsg('sdk.send', arguments); return orig.apply(this, arguments); };
            }
            window.__unified_sdk = true;
        }
    } catch(e) {}
    
    return { hooked: true, queueSize: window.__unifiedMsgs.length };
})()
'@

$r = Invoke-Cdp -Method "Runtime.evaluate" -Params @{ expression = $hookScript; returnByValue = $true }
Write-Log "钩子已安装" "Green"

Write-Log ""
Write-Log "========== 开始监控 ==========" "Green"
Write-Log ""

$getScript = "(function(){var m=window.__unifiedMsgs||[];window.__unifiedMsgs=[];return JSON.stringify(m);})()"

$msgCount = 0
$botAccounts = @{}
$startTime = Get-Date
$lastStatus = Get-Date
$processed = @{}

try {
    while ($true) {
        $r = Invoke-Cdp -Method "Runtime.evaluate" -Params @{ expression = $getScript; returnByValue = $true }
        
        if ($r.result -and $r.result.result -and $r.result.result.value) {
            $json = $r.result.result.value
            if ($json -and $json -ne "[]") {
                $msgs = $json | ConvertFrom-Json
                
                foreach ($m in $msgs) {
                    if ($processed.ContainsKey($m.id)) { continue }
                    $processed[$m.id] = $true
                    if ($processed.Count -gt 3000) { $processed.Clear() }
                    
                    $msgCount++
                    $time = [DateTimeOffset]::FromUnixTimeMilliseconds($m.time).LocalDateTime.ToString("HH:mm:ss")
                    
                    # 解析
                    $from = ""; $to = ""; $text = ""; $nick = ""; $type = ""
                    try {
                        $d = $m.data | ConvertFrom-Json
                        if ($d -is [array] -and $d.Count -gt 0) { $d = $d[0] }
                        
                        $from = $d.from
                        $to = $d.to
                        $type = $d.type
                        $text = $d.text
                        $nick = if ($d.user) { $d.user.groupMemberNick } else { $d.fromNick }
                        
                        # 解码 content.b
                        if (-not $text -and $d.content -and $d.content.b) {
                            $decoded = Decode-B64 -b64 $d.content.b
                            if ($decoded) { $text = "[解码] $decoded" }
                        }
                    } catch {}
                    
                    # 特征检测
                    $feat = @()
                    if ($nick -match '^[0-9a-f]{32}$') { $feat += "哈希昵称"; $botAccounts[$from] = $nick }
                    if ($text -match '禁言|封盘|管理') { $feat += "管理" }
                    if ($text -match '机器|客服') { $feat += "机器人" }
                    if ($text -match '\d+\+\d+\+\d+=\d+') { $feat += "开奖" }
                    
                    $isTarget = ($to -eq $targetTeamId -or $from -eq $targetTeamId)
                    $marker = if ($isTarget) { "★" } else { " " }
                    
                    $color = "Gray"
                    if ($isTarget) { $color = "Green" }
                    if ($feat -contains "机器人" -or $feat -contains "哈希昵称") { $color = "Magenta" }
                    if ($feat -contains "开奖") { $color = "Cyan" }
                    
                    $featTag = if ($feat.Count -gt 0) { " [" + ($feat -join ",") + "]" } else { "" }
                    $preview = if ($text.Length -gt 100) { $text.Substring(0, 100) + "..." } else { $text }
                    
                    Write-Log "$marker [$time] [$($m.source)] $nick ($from) -> $to [$type]: $preview$featTag" $color
                }
            }
        }
        
        # 状态
        if (((Get-Date) - $lastStatus).TotalSeconds -gt 30) {
            $dur = (Get-Date) - $startTime
            Write-Log "--- 运行 $($dur.ToString('hh\:mm\:ss')), 消息 $msgCount, 机器人 $($botAccounts.Count) ---" "DarkYellow"
            $lastStatus = Get-Date
        }
        
        Start-Sleep -Milliseconds 500
    }
} catch {
    Write-Log "异常: $_" "Red"
} finally {
    try { $ws.CloseAsync([System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure, "", $ct).Wait(5000) } catch {}
    $ws.Dispose()
    
    Write-Log ""
    Write-Log "========== 监控结束 ==========" "Yellow"
    Write-Log "消息: $msgCount, 机器人: $($botAccounts.Keys -join ', ')"
}

