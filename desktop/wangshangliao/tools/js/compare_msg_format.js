/**
 * 对比收到消息和发送消息的格式差异
 */
const WebSocket = require('ws');
const http = require('http');

let ws = null;
let msgId = 0;

const LOGO_ACCOUNT = '1391351554';

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
        const timeout = setTimeout(() => reject(new Error('Timeout')), 20000);
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
    console.log('🔍 对比消息格式差异\n');
    
    const wsUrl = await getWebSocketUrl();
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    console.log('✅ 已连接\n');
    
    // 获取与logo的历史消息
    console.log('=== 获取历史消息 ===\n');
    const history = await evaluate(`(async () => {
        return new Promise(r => {
            window.nim.getHistoryMsgs({
                scene: 'p2p',
                to: '${LOGO_ACCOUNT}',
                limit: 20,
                done: (err, obj) => {
                    if (err) r({ error: err.message });
                    else r(obj?.msgs || []);
                }
            });
            setTimeout(() => r({ error: 'Timeout' }), 15000);
        });
    })()`);
    
    if (history?.error) {
        console.log('获取历史失败:', history.error);
    } else {
        // 分离收到的和发出的消息
        const received = history.filter(m => m.flow === 'in');
        const sent = history.filter(m => m.flow === 'out');
        
        console.log(`收到消息: ${received.length} 条`);
        console.log(`发出消息: ${sent.length} 条`);
        
        // 对比第一条收到的和第一条发出的
        console.log('\n=== 收到消息的完整结构 ===\n');
        if (received.length > 0) {
            const recvMsg = received[0];
            console.log('类型:', recvMsg.type);
            console.log('内容text:', recvMsg.text || '(空)');
            console.log('attach:', recvMsg.attach);
            console.log('content:', typeof recvMsg.content, recvMsg.content ? JSON.stringify(recvMsg.content).substring(0, 200) : 'null');
            console.log('custom:', recvMsg.custom);
            console.log('\n所有字段:');
            Object.keys(recvMsg).forEach(k => {
                const v = recvMsg[k];
                const val = typeof v === 'object' ? JSON.stringify(v).substring(0, 80) : String(v).substring(0, 80);
                console.log(`  ${k}: ${val}`);
            });
        }
        
        console.log('\n=== 发出消息的完整结构 ===\n');
        if (sent.length > 0) {
            const sentMsg = sent[0];
            console.log('类型:', sentMsg.type);
            console.log('内容text:', sentMsg.text?.substring(0, 50) || '(空)');
            console.log('attach:', sentMsg.attach);
            console.log('content:', typeof sentMsg.content, sentMsg.content ? JSON.stringify(sentMsg.content).substring(0, 200) : 'null');
            console.log('custom:', sentMsg.custom);
            console.log('\n所有字段:');
            Object.keys(sentMsg).forEach(k => {
                const v = sentMsg[k];
                const val = typeof v === 'object' ? JSON.stringify(v).substring(0, 80) : String(v).substring(0, 80);
                console.log(`  ${k}: ${val}`);
            });
        }
        
        // 关键对比
        console.log('\n=== 关键差异对比 ===\n');
        if (received.length > 0 && sent.length > 0) {
            const r = received[0];
            const s = sent[0];
            
            console.log('字段对比:');
            console.log(`  type:       收到=${r.type}, 发出=${s.type}`);
            console.log(`  有text:     收到=${!!r.text}, 发出=${!!s.text}`);
            console.log(`  有content:  收到=${!!r.content}, 发出=${!!s.content}`);
            console.log(`  有attach:   收到=${!!r.attach}, 发出=${!!s.attach}`);
            console.log(`  有custom:   收到=${!!r.custom}, 发出=${!!s.custom}`);
            console.log(`  status:     收到=${r.status}, 发出=${s.status}`);
            
            // 检查收到消息的content格式
            if (r.content && r.type === 'custom') {
                console.log('\n收到消息的content解析:');
                try {
                    const content = typeof r.content === 'string' ? JSON.parse(r.content) : r.content;
                    console.log(JSON.stringify(content, null, 2));
                } catch (e) {
                    console.log('  解析失败:', e.message);
                }
            }
        }
    }
    
    // 尝试用custom类型发送
    console.log('\n=== 尝试用 custom 类型发送 ===\n');
    const customResult = await evaluate(`(async () => {
        return new Promise(r => {
            window.nim.sendCustomMsg({
                scene: 'p2p',
                to: '${LOGO_ACCOUNT}',
                content: JSON.stringify({
                    type: 1,
                    data: {
                        text: '【custom测试】' + new Date().toLocaleTimeString()
                    }
                }),
                done: (err, msg) => {
                    if (err) r({ success: false, error: err.message, code: err.code });
                    else r({ success: true, idServer: msg?.idServer, type: msg?.type, status: msg?.status });
                }
            });
            setTimeout(() => r({ error: 'Timeout' }), 10000);
        });
    })()`);
    console.log('custom消息发送结果:', customResult);
    
    console.log('\n');
    ws.close();
}

main().catch(console.error);
