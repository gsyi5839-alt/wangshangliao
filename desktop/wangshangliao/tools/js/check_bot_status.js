/**
 * 检查机器人状态和私聊消息处理能力
 */
const WebSocket = require('ws');
const http = require('http');

let ws = null;
let msgId = 0;

async function getWebSocketUrl() {
    return new Promise((resolve, reject) => {
        const req = http.get('http://127.0.0.1:9222/json', (res) => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', () => {
                const pages = JSON.parse(data);
                const mainPage = pages.find(p => p.url?.includes('index.html')) || pages[0];
                resolve(mainPage?.webSocketDebuggerUrl);
            });
        });
        req.on('error', reject);
    });
}

function evaluate(expression, awaitPromise = true) {
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
        ws.send(JSON.stringify({ id, method: 'Runtime.evaluate', params: { expression, awaitPromise, returnByValue: true } }));
    });
}

async function main() {
    console.log('🔍 检查机器人状态\n');
    
    const wsUrl = await getWebSocketUrl();
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    console.log('✅ 已连接旺商聊\n');
    
    // 1. 检查Hook状态
    console.log('=== 1. Hook状态 ===\n');
    const hookStatus = await evaluate(`(() => {
        return {
            botReceivedMessages: !!window.__botReceivedMessages,
            msgCount: (window.__botReceivedMessages || []).length,
            botSystemMessages: !!window.__botSystemMessages,
            sysCount: (window.__botSystemMessages || []).length,
            nimConnected: !!window.nim,
            onmsgHooked: typeof window.nim?.options?.onmsg === 'function',
            onmsgsHooked: typeof window.nim?.options?.onmsgs === 'function'
        };
    })()`, false);
    
    console.log('Hook数组:', hookStatus?.botReceivedMessages ? '✅ 已创建' : '❌ 未创建');
    console.log('消息数量:', hookStatus?.msgCount);
    console.log('NIM已连接:', hookStatus?.nimConnected ? '✅' : '❌');
    console.log('onmsg已Hook:', hookStatus?.onmsgHooked ? '✅' : '❌');
    console.log('onmsgs已Hook:', hookStatus?.onmsgsHooked ? '✅' : '❌');
    
    // 2. 检查是否有私聊会话
    console.log('\n=== 2. 私聊会话检查 ===\n');
    const sessions = await evaluate(`(async () => {
        return new Promise(r => {
            window.nim.getLocalSessions({
                limit: 100,
                done: (err, result) => {
                    if (err) r({ error: err.message });
                    else {
                        var sessions = Array.isArray(result) ? result : (result?.sessions || []);
                        r({
                            total: sessions.length,
                            p2p: sessions.filter(s => s.scene === 'p2p').map(s => ({
                                to: s.to,
                                lastMsg: s.lastMsg?.text?.substring(0, 30) || '',
                                updateTime: s.updateTime
                            }))
                        });
                    }
                }
            });
            setTimeout(() => r({ error: 'Timeout' }), 10000);
        });
    })()`);
    
    if (sessions?.error) {
        console.log('获取会话失败:', sessions.error);
    } else {
        console.log('会话总数:', sessions?.total);
        console.log('私聊会话:', sessions?.p2p?.length || 0);
        
        if (sessions?.p2p?.length > 0) {
            console.log('\n私聊会话列表:');
            sessions.p2p.slice(0, 10).forEach((s, i) => {
                console.log(`  ${i + 1}. ${s.to}: ${s.lastMsg || '(无消息)'}`);
            });
        }
    }
    
    // 3. 当前账号信息
    console.log('\n=== 3. 当前账号 ===\n');
    const myInfo = await evaluate(`(async () => {
        return new Promise(r => {
            window.nim.getMyInfo({ done: (e, i) => r(i || {}) });
            setTimeout(() => r({}), 5000);
        });
    })()`);
    console.log('账号:', myInfo?.account);
    console.log('昵称:', myInfo?.nick);
    
    // 4. 测试私聊消息接收（通过onmsg回调）
    console.log('\n=== 4. 测试消息回调 ===\n');
    
    // 注入一个测试标记
    await evaluate(`(() => {
        window.__testP2PReceived = [];
        var origOnmsg = window.nim.options.onmsg;
        window.nim.options.onmsg = function(msg) {
            if (msg.scene === 'p2p' && msg.flow === 'in') {
                window.__testP2PReceived.push({
                    from: msg.from,
                    text: msg.text,
                    time: Date.now()
                });
            }
            if (origOnmsg) origOnmsg(msg);
        };
        return true;
    })()`, false);
    
    console.log('已注入私聊消息监控');
    console.log('\n⏳ 请在30秒内从 logo 账号私聊机器人...\n');
    
    for (let i = 0; i < 30; i++) {
        await new Promise(r => setTimeout(r, 1000));
        
        const p2pMsgs = await evaluate(`(() => {
            return window.__testP2PReceived || [];
        })()`, false);
        
        if (p2pMsgs?.length > 0) {
            console.log('\n\n🎉 收到私聊消息!');
            p2pMsgs.forEach((m, idx) => {
                console.log(`  ${idx + 1}. 来自: ${m.from}`);
                console.log(`     内容: ${m.text}`);
            });
            break;
        }
        
        process.stdout.write(`\r等待中... ${30 - i}秒 (私聊消息: ${p2pMsgs?.length || 0})`);
    }
    
    console.log('\n');
    
    ws.close();
}

main().catch(console.error);
