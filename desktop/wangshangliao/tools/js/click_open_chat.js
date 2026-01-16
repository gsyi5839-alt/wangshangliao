/**
 * 在机器人端点击打开与logo的聊天窗口
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
    console.log('🔧 在机器人端打开与logo的聊天\n');
    
    const wsUrl = await getWebSocketUrl();
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    console.log('✅ 已连接\n');
    
    // 1. 获取会话列表
    console.log('=== 1. 获取本地会话列表 ===\n');
    const sessions = await evaluate(`(async () => {
        return new Promise(r => {
            window.nim.getLocalSessions({
                limit: 50,
                done: (err, result) => {
                    if (err) r({ error: err.message });
                    else {
                        var sessions = Array.isArray(result) ? result : (result?.sessions || []);
                        r(sessions.map(s => ({
                            id: s.id,
                            scene: s.scene,
                            to: s.to,
                            unread: s.unread,
                            lastMsgTime: s.lastMsg?.time
                        })));
                    }
                }
            });
            setTimeout(() => r([]), 10000);
        });
    })()`);
    
    console.log('会话数:', sessions?.length || 0);
    
    // 查找与logo的会话
    const logoSession = (sessions || []).find(s => s.to === LOGO_ACCOUNT);
    if (logoSession) {
        console.log('\n✅ 找到与 logo 的会话:');
        console.log('   会话ID:', logoSession.id);
        console.log('   未读数:', logoSession.unread);
    } else {
        console.log('\n⚠️ 没有与 logo 的会话记录');
    }
    
    // 显示前5个会话
    console.log('\n前5个会话:');
    (sessions || []).slice(0, 5).forEach((s, i) => {
        const mark = s.to === LOGO_ACCOUNT ? '⭐' : '';
        console.log(`  ${i + 1}. ${s.scene}|${s.to} | 未读:${s.unread} ${mark}`);
    });
    
    // 2. 尝试通过Vue路由或事件打开会话
    console.log('\n=== 2. 尝试通过Pinia/Vue打开会话 ===\n');
    const openResult = await evaluate(`(() => {
        try {
            var app = document.querySelector('#app');
            var gp = app?.__vue_app__?.config?.globalProperties;
            var pinia = gp?.$pinia;
            var appStore = pinia?._s?.get('app');
            
            if (appStore) {
                // 设置当前会话
                appStore.currentSession = {
                    scene: 'p2p',
                    to: '${LOGO_ACCOUNT}',
                    id: 'p2p-${LOGO_ACCOUNT}'
                };
                return { success: true, message: 'Set via Pinia' };
            }
            return { error: 'appStore not found' };
        } catch(e) {
            return { error: e.message };
        }
    })()`, false);
    console.log('打开会话结果:', openResult);
    
    // 3. 发送测试消息
    console.log('\n=== 3. 发送测试消息 ===\n');
    const testMsg = `【最终测试】${new Date().toLocaleTimeString()} - 请确认logo是否收到`;
    
    const sendResult = await evaluate(`(async () => {
        return new Promise(r => {
            window.nim.sendText({
                scene: 'p2p',
                to: '${LOGO_ACCOUNT}',
                text: '${testMsg}',
                done: (err, msg) => {
                    if (err) r({ success: false, error: err.message });
                    else r({ 
                        success: true, 
                        idServer: msg?.idServer,
                        status: msg?.status
                    });
                }
            });
            setTimeout(() => r({ error: 'Timeout' }), 10000);
        });
    })()`);
    
    console.log('发送结果:', sendResult);
    
    if (sendResult?.success) {
        console.log('\n✅ 消息发送成功');
        console.log('消息ID:', sendResult.idServer);
        console.log('\n📌 请立即检查 logo 账号是否收到消息：');
        console.log(`   "${testMsg}"`);
    }
    
    // 4. 检查发送后的历史
    console.log('\n=== 4. 检查发送后历史 ===\n');
    await new Promise(r => setTimeout(r, 2000));
    
    const history = await evaluate(`(async () => {
        return new Promise(r => {
            window.nim.getHistoryMsgs({
                scene: 'p2p',
                to: '${LOGO_ACCOUNT}',
                limit: 3,
                done: (err, obj) => {
                    if (err) r({ error: err.message });
                    else r((obj?.msgs || []).map(m => ({
                        flow: m.flow,
                        text: m.text?.substring(0, 60),
                        time: new Date(m.time).toLocaleTimeString(),
                        status: m.status
                    })));
                }
            });
            setTimeout(() => r([]), 5000);
        });
    })()`);
    
    console.log('最新3条消息:');
    (history || []).forEach((m, i) => {
        const dir = m.flow === 'out' ? '📤发出' : '📥收到';
        console.log(`  ${i + 1}. [${m.time}] ${dir} | ${m.status} | ${m.text}`);
    });
    
    console.log('\n');
    ws.close();
}

main().catch(console.error);
