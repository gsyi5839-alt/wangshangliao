/**
 * 检查所有会话的最新消息
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
    
    console.log('检查所有会话的最新消息\n');
    
    // 获取所有本地会话
    const sessions = await evaluate(`(async () => {
        return new Promise(r => {
            window.nim.getLocalSessions({
                limit: 10,
                done: (err, obj) => {
                    r((obj || []).map(s => ({
                        id: s.id,
                        scene: s.scene,
                        to: s.to,
                        lastMsgText: s.lastMsg?.text?.substring(0, 30),
                        lastMsgType: s.lastMsg?.type,
                        lastMsgTime: s.lastMsg?.time ? new Date(s.lastMsg.time).toLocaleTimeString() : null,
                        unread: s.unread
                    })));
                }
            });
            setTimeout(() => r([]), 10000);
        });
    })()`);
    
    console.log('会话列表:');
    (sessions || []).forEach((s, i) => {
        console.log(`${i + 1}. [${s.scene}] ${s.to}`);
        console.log(`   最新: ${s.lastMsgType} - ${s.lastMsgText || '(无)'} @ ${s.lastMsgTime}`);
        console.log(`   未读: ${s.unread}`);
        console.log('');
    });
    
    // 查找最新的发出消息（可能在群聊中）
    console.log('\n搜索今天发送的"机器人测试"消息...\n');
    
    for (const session of (sessions || []).slice(0, 5)) {
        const msgs = await evaluate(`(async () => {
            return new Promise(r => {
                window.nim.getHistoryMsgs({
                    scene: '${session.scene}',
                    to: '${session.to}',
                    limit: 3,
                    done: (err, obj) => {
                        r((obj?.msgs || []).filter(m => 
                            m.flow === 'out' && 
                            (m.text?.includes('机器人测试') || m.text?.includes('测试'))
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
            console.log(`在 ${session.scene}-${session.to} 找到:`);
            msgs.forEach(m => console.log(`  - ${m.text} @ ${m.time}`));
        }
    }
    
    ws.close();
}

main().catch(console.error);
