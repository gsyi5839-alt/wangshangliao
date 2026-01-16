/**
 * 打开与logo的聊天会话并发送消息
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
    console.log('🔧 打开聊天会话并发送消息\n');
    
    const wsUrl = await getWebSocketUrl();
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    console.log('✅ 已连接\n');
    
    // 1. 设置当前会话为与 logo 的私聊
    console.log('=== 1. 设置当前会话 ===\n');
    const setSession = await evaluate(`(async () => {
        return new Promise(r => {
            window.nim.setCurrSession({
                scene: 'p2p',
                to: '${LOGO_ACCOUNT}',
                done: (err) => {
                    if (err) r({ error: err.message });
                    else r({ success: true });
                }
            });
            setTimeout(() => r({ error: 'Timeout' }), 5000);
        });
    })()`);
    console.log('设置会话结果:', setSession);
    
    // 2. 通过 Pinia 检查当前会话
    console.log('\n=== 2. 检查 Pinia 当前会话 ===\n');
    const currentSession = await evaluate(`(() => {
        try {
            var app = document.querySelector('#app');
            var gp = app?.__vue_app__?.config?.globalProperties;
            var pinia = gp?.$pinia;
            var appStore = pinia?._s?.get('app');
            var session = appStore?.currentSession || appStore?.currSession;
            
            if (session) {
                return {
                    scene: session.scene,
                    to: session.to,
                    id: session.id
                };
            }
            return { error: 'No current session' };
        } catch(e) {
            return { error: e.message };
        }
    })()`, false);
    console.log('当前会话:', currentSession);
    
    // 3. 通过sendMsg发送（不是sendText）
    console.log('\n=== 3. 尝试 sendMsg API ===\n');
    const sendMsgResult = await evaluate(`(async () => {
        return new Promise(r => {
            // 构造消息对象
            var msg = {
                scene: 'p2p',
                to: '${LOGO_ACCOUNT}',
                type: 'text',
                text: '【sendMsg测试】${new Date().toLocaleTimeString()}'
            };
            
            window.nim.sendMsg({
                msg: msg,
                done: (err, sentMsg) => {
                    if (err) r({ success: false, error: err.message, code: err.code });
                    else r({ success: true, idServer: sentMsg?.idServer, to: sentMsg?.to });
                }
            });
            setTimeout(() => r({ error: 'Timeout' }), 10000);
        });
    })()`);
    console.log('sendMsg 结果:', sendMsgResult);
    
    // 4. 直接使用 sendText 再试一次
    console.log('\n=== 4. 再次使用 sendText ===\n');
    const sendTextResult = await evaluate(`(async () => {
        return new Promise(r => {
            window.nim.sendText({
                scene: 'p2p',
                to: '${LOGO_ACCOUNT}',
                text: '【sendText测试】机器人回复 ${new Date().toLocaleTimeString()}',
                done: (err, msg) => {
                    if (err) r({ success: false, error: err.message, code: err.code });
                    else r({ 
                        success: true, 
                        idServer: msg?.idServer, 
                        to: msg?.to,
                        status: msg?.status
                    });
                }
            });
            setTimeout(() => r({ error: 'Timeout' }), 10000);
        });
    })()`);
    console.log('sendText 结果:', sendTextResult);
    
    // 5. 检查UI中是否显示会话
    console.log('\n=== 5. 检查UI会话列表 ===\n');
    const uiSessions = await evaluate(`(() => {
        // 查找会话列表中是否有 logo
        var sessionItems = document.querySelectorAll('.session-item, .chat-item, [class*="session"]');
        var found = [];
        sessionItems.forEach(item => {
            var text = item.textContent || '';
            if (text.includes('logo') || text.includes('${LOGO_ACCOUNT}') || text.includes('法拉利')) {
                found.push(text.substring(0, 50));
            }
        });
        return { count: sessionItems.length, found: found };
    })()`, false);
    console.log('UI会话数:', uiSessions?.count);
    console.log('找到相关会话:', uiSessions?.found);
    
    // 6. 尝试通过UI发送
    console.log('\n=== 6. 模拟UI发送 ===\n');
    const uiSend = await evaluate(`(() => {
        // 查找输入框
        var input = document.querySelector('textarea[placeholder*="输入"], .input-area textarea, [class*="input"] textarea');
        if (!input) return { error: 'Input not found' };
        
        // 设置内容
        input.value = '【UI模拟发送】' + new Date().toLocaleTimeString();
        input.dispatchEvent(new Event('input', { bubbles: true }));
        
        // 查找发送按钮
        var sendBtn = document.querySelector('button[class*="send"], .send-btn, [class*="发送"]');
        
        return { 
            inputFound: !!input,
            sendBtnFound: !!sendBtn,
            inputValue: input.value
        };
    })()`, false);
    console.log('UI发送准备:', uiSend);
    
    console.log('\n========================================');
    console.log('📌 请检查 logo 账号是否收到消息');
    console.log('========================================\n');
    
    ws.close();
}

main().catch(console.error);
