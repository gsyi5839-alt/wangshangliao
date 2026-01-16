/**
 * 检查当前聊天对象和最新消息
 */
const WebSocket = require('ws');
const http = require('http');

let ws = null;
let msgId = 0;

async function getWebSocketUrl() {
    return new Promise((resolve, reject) => {
        const req = http.get('http://127.0.0.1:9222/json', (res) => {
            let d = '';
            res.on('data', c => d += c);
            res.on('end', () => resolve(JSON.parse(d)));
        });
        req.on('error', reject);
    });
}

function evaluate(expression) {
    return new Promise((resolve, reject) => {
        const id = ++msgId;
        const timeout = setTimeout(() => reject(new Error('Timeout')), 15000);
        const handler = (data) => {
            const msg = JSON.parse(data.toString());
            if (msg.id === id) {
                clearTimeout(timeout);
                ws.off('message', handler);
                resolve(msg.result?.result?.value);
            }
        };
        ws.on('message', handler);
        ws.send(JSON.stringify({ 
            id, 
            method: 'Runtime.evaluate', 
            params: { expression, awaitPromise: true, returnByValue: true } 
        }));
    });
}

async function main() {
    const res = await getWebSocketUrl();
    const wsUrl = res.find(p => p.url?.includes('index.html'))?.webSocketDebuggerUrl || res[0]?.webSocketDebuggerUrl;
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    
    console.log('🔍 检查当前聊天状态\n');
    
    // 1. 检查NIM当前会话
    console.log('=== 1. NIM当前会话 ===\n');
    const nimSession = await evaluate(`(() => {
        var session = window.nim?.currSession;
        return session ? {
            id: session.id,
            scene: session.scene,
            to: session.to
        } : { noSession: true };
    })()`);
    console.log('NIM会话:', nimSession);
    
    // 2. 检查页面上的聊天标题
    console.log('\n=== 2. 页面聊天标题 ===\n');
    const chatHeader = await evaluate(`(() => {
        // 查找聊天区域的标题
        var headers = document.querySelectorAll('[class*="header"], [class*="title"], [class*="name"]');
        var result = [];
        headers.forEach(h => {
            var text = h.textContent?.trim();
            if (text && text.length < 30 && !text.includes('搜索')) {
                var rect = h.getBoundingClientRect();
                // 只要在主内容区域的标题 (x > 250)
                if (rect.x > 250 && rect.y < 100) {
                    result.push({
                        text: text,
                        x: rect.x,
                        y: rect.y,
                        className: h.className?.substring(0, 40)
                    });
                }
            }
        });
        return result;
    })()`);
    console.log('聊天标题:', chatHeader);
    
    // 3. 检查最近的所有会话
    console.log('\n=== 3. 所有会话列表 ===\n');
    const sessions = await evaluate(`(async () => {
        return new Promise(r => {
            window.nim.getLocalSessions({
                limit: 10,
                done: (err, obj) => {
                    r((obj || []).map(s => ({
                        id: s.id,
                        scene: s.scene,
                        to: s.to,
                        lastMsgTime: s.lastMsg?.time ? new Date(s.lastMsg.time).toLocaleTimeString() : null,
                        lastMsgText: s.lastMsg?.text?.substring(0, 20)
                    })));
                }
            });
            setTimeout(() => r([]), 5000);
        });
    })()`);
    
    (sessions || []).forEach((s, i) => {
        console.log(`${i + 1}. [${s.scene}] ${s.to} @ ${s.lastMsgTime}`);
        if (s.lastMsgText) console.log(`   最新: ${s.lastMsgText}`);
    });
    
    // 4. 在团队/群聊中搜索刚发送的消息
    console.log('\n=== 4. 搜索"机器人测试"消息 ===\n');
    
    const teamSessions = (sessions || []).filter(s => s.scene === 'team');
    for (const ts of teamSessions.slice(0, 3)) {
        const msgs = await evaluate(`(async () => {
            return new Promise(r => {
                window.nim.getHistoryMsgs({
                    scene: 'team',
                    to: '${ts.to}',
                    limit: 5,
                    done: (err, obj) => {
                        r((obj?.msgs || []).filter(m => 
                            m.text?.includes('机器人测试')
                        ).map(m => ({
                            text: m.text?.substring(0, 40),
                            time: new Date(m.time).toLocaleTimeString()
                        })));
                    }
                });
                setTimeout(() => r([]), 3000);
            });
        })()`);
        
        if (msgs && msgs.length > 0) {
            console.log(`在群 ${ts.to} 找到:`);
            msgs.forEach(m => console.log(`  - ${m.text} @ ${m.time}`));
        }
    }
    
    ws.close();
}

main().catch(console.error);
