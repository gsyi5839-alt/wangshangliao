# 切换到指定群聊并获取历史消息
$ErrorActionPreference = 'Stop'
$cdpPort = 9333
$targetGroupId = "333338888"
$targetGroupName = "天谕2.8(24h)"

function Invoke-CdpCommand {
    param([hashtable]$Command, [int]$Timeout = 30000)
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
        $buffer = New-Object byte[] 2097152
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

Write-Host "=== 切换到目标群聊 ===" -ForegroundColor Cyan
Write-Host "目标群: $targetGroupName ($targetGroupId)" -ForegroundColor Yellow
Write-Host ""

# 1. 获取群列表并查找目标群
Write-Host "【1】搜索目标群..." -ForegroundColor Yellow
$script1 = @"
(function() {
    var result = { found: false, group: null, allGroups: [], error: null };
    try {
        var app = document.querySelector('#app');
        var gp = app && app.__vue_app__ && app.__vue_app__.config && app.__vue_app__.config.globalProperties;
        var pinia = gp && gp.\$pinia;
        var appStore = pinia && pinia._s && pinia._s.get && pinia._s.get('app');
        
        if (appStore && appStore.groupList) {
            for (var i = 0; i < appStore.groupList.length; i++) {
                var g = appStore.groupList[i];
                var gid = g.groupCloudId || g.teamId || g.id || '';
                var gname = g.groupName || g.name || '';
                result.allGroups.push({ id: gid, name: gname });
                
                // 检查是否是目标群
                if (gid === '$targetGroupId' || gname.indexOf('$targetGroupName') >= 0 || gname.indexOf('天谕') >= 0) {
                    result.found = true;
                    result.group = {
                        id: gid,
                        name: gname,
                        memberNum: g.memberNum || 0,
                        owner: g.owner || '',
                        raw: JSON.stringify(g).substring(0, 500)
                    };
                }
            }
        }
        
        // 也从会话列表中查找
        if (!result.found && appStore && appStore.orderedSessions) {
            for (var i = 0; i < appStore.orderedSessions.length; i++) {
                var s = appStore.orderedSessions[i];
                if (s.scene === 'team') {
                    var sid = s.to || s.id || '';
                    var sname = s.teamName || s.name || s.nickname || '';
                    if (sid === '$targetGroupId' || sname.indexOf('天谕') >= 0) {
                        result.found = true;
                        result.group = { id: sid, name: sname, fromSession: true };
                    }
                }
            }
        }
        
    } catch(e) {
        result.error = e.message;
    }
    return JSON.stringify(result, null, 2);
})()
"@

$cmd = @{ method = "Runtime.evaluate"; params = @{ expression = $script1; returnByValue = $true } }
$response = Invoke-CdpCommand -Command $cmd
if ($response.result -and $response.result.result -and $response.result.result.value) {
    $data = $response.result.result.value | ConvertFrom-Json
    
    Write-Host "所有群列表:" -ForegroundColor Gray
    foreach ($g in $data.allGroups) {
        $marker = if ($g.id -eq $targetGroupId -or $g.name -like "*天谕*") { " ★" } else { "" }
        Write-Host "  - $($g.name) (ID: $($g.id))$marker" -ForegroundColor Gray
    }
    
    if ($data.found) {
        Write-Host ""
        Write-Host "✓ 找到目标群: $($data.group.name) (ID: $($data.group.id))" -ForegroundColor Green
        $targetGroupId = $data.group.id  # 更新实际的群ID
    } else {
        Write-Host ""
        Write-Host "✗ 未找到目标群，尝试通过搜索查找..." -ForegroundColor Yellow
    }
}

Write-Host ""

# 2. 获取当前会话和最近消息
Write-Host "【2】获取会话消息..." -ForegroundColor Yellow
$script2 = @"
(function() {
    var result = { currSession: null, sessionMessages: [], recentMsgs: [], error: null };
    try {
        var app = document.querySelector('#app');
        var gp = app && app.__vue_app__ && app.__vue_app__.config && app.__vue_app__.config.globalProperties;
        var pinia = gp && gp.\$pinia;
        var appStore = pinia && pinia._s && pinia._s.get && pinia._s.get('app');
        
        if (appStore) {
            // 当前会话
            if (appStore.currSession) {
                result.currSession = {
                    scene: appStore.currSession.scene,
                    to: appStore.currSession.to,
                    id: appStore.currSession.id,
                    name: appStore.currSession.teamName || appStore.currSession.name || appStore.currSession.nickname || ''
                };
            }
            
            // 会话列表 - 找目标群的会话
            if (appStore.sessionMap) {
                var keys = Object.keys(appStore.sessionMap);
                for (var i = 0; i < keys.length; i++) {
                    var k = keys[i];
                    if (k.indexOf('$targetGroupId') >= 0 || k.indexOf('team') >= 0) {
                        var sess = appStore.sessionMap[k];
                        result.sessionMessages.push({
                            key: k,
                            lastMsg: sess.lastMsg ? {
                                text: (sess.lastMsg.text || '').substring(0, 100),
                                from: sess.lastMsg.from,
                                time: sess.lastMsg.time
                            } : null
                        });
                    }
                }
            }
        }
        
        // 检查 msg store
        var msgStore = pinia && pinia._s && pinia._s.get && pinia._s.get('msg');
        if (msgStore && msgStore.msgList) {
            for (var i = Math.max(0, msgStore.msgList.length - 20); i < msgStore.msgList.length; i++) {
                var m = msgStore.msgList[i];
                if (m.scene === 'team') {
                    result.recentMsgs.push({
                        from: m.from,
                        to: m.to,
                        text: (m.text || '').substring(0, 80),
                        time: m.time,
                        fromNick: m.fromNick || ''
                    });
                }
            }
        }
        
    } catch(e) {
        result.error = e.message;
    }
    return JSON.stringify(result, null, 2);
})()
"@

$cmd = @{ method = "Runtime.evaluate"; params = @{ expression = $script2; returnByValue = $true } }
$response = Invoke-CdpCommand -Command $cmd
if ($response.result -and $response.result.result -and $response.result.result.value) {
    Write-Host $response.result.result.value -ForegroundColor Green
}

Write-Host ""

# 3. 尝试切换到目标群会话
Write-Host "【3】尝试切换到目标群..." -ForegroundColor Yellow
$script3 = @"
(function() {
    var result = { switched: false, method: '', error: null };
    try {
        var app = document.querySelector('#app');
        var gp = app && app.__vue_app__ && app.__vue_app__.config && app.__vue_app__.config.globalProperties;
        var pinia = gp && gp.\$pinia;
        var appStore = pinia && pinia._s && pinia._s.get && pinia._s.get('app');
        
        // 方法1: 使用 router 跳转
        var router = gp && gp.\$router;
        if (router && router.push) {
            try {
                router.push({ name: 'chatbox', params: { scene: 'team', account: '$targetGroupId' }});
                result.method = 'router.push';
                result.switched = true;
            } catch(e) {}
        }
        
        // 方法2: 直接设置 currSession
        if (!result.switched && appStore) {
            // 在 orderedSessions 中查找
            if (appStore.orderedSessions) {
                for (var i = 0; i < appStore.orderedSessions.length; i++) {
                    var s = appStore.orderedSessions[i];
                    if (s.to === '$targetGroupId' || (s.teamName && s.teamName.indexOf('天谕') >= 0)) {
                        appStore.currSession = s;
                        result.method = 'set currSession';
                        result.switched = true;
                        result.session = { to: s.to, name: s.teamName || s.name };
                        break;
                    }
                }
            }
        }
        
        // 方法3: 模拟点击会话列表
        if (!result.switched) {
            var sessionItems = document.querySelectorAll('[class*="session-item"], [class*="chat-item"], [class*="conversation"]');
            for (var i = 0; i < sessionItems.length; i++) {
                var item = sessionItems[i];
                var text = item.innerText || '';
                if (text.indexOf('天谕') >= 0 || text.indexOf('$targetGroupId') >= 0) {
                    item.click();
                    result.method = 'click session item';
                    result.switched = true;
                    result.clickedText = text.substring(0, 50);
                    break;
                }
            }
        }
        
    } catch(e) {
        result.error = e.message;
    }
    return JSON.stringify(result, null, 2);
})()
"@

$cmd = @{ method = "Runtime.evaluate"; params = @{ expression = $script3; returnByValue = $true } }
$response = Invoke-CdpCommand -Command $cmd
if ($response.result -and $response.result.result -and $response.result.result.value) {
    $data = $response.result.result.value | ConvertFrom-Json
    if ($data.switched) {
        Write-Host "✓ 已切换到目标群 (方法: $($data.method))" -ForegroundColor Green
    } else {
        Write-Host "✗ 未能自动切换，请手动点击群聊窗口" -ForegroundColor Yellow
    }
    Write-Host $response.result.result.value -ForegroundColor Gray
}

Write-Host ""
Write-Host "=== 完成 ===" -ForegroundColor Cyan
Write-Host "请确保旺商聊窗口中已打开目标群聊天窗口，然后监控脚本就能捕获消息了" -ForegroundColor Yellow

