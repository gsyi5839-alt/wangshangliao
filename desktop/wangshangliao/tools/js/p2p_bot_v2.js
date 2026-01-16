/**
 * 私聊自动回复机器人 V2 - 改进版
 * 实时监听并回复
 */
const WebSocket = require('ws');
const http = require('http');

let ws = null;
let msgId = 0;

// 配置
const LOGO_ACCOUNT = '1391351554';
const AUTO_REPLY = '【机器人自动回复】您好，已收到您的消息！';

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

async function sendReply(to, text) {
    console.log(`\n📤 正在发送回复到 ${to}...`);
    console.log(`   内容: ${text}`);
    
    const result = await evaluate(`(async () => {
        try {
            return new Promise((resolve) => {
                console.log('开始发送私聊消息...');
                window.nim.sendText({
                    scene: 'p2p',
                    to: '${to}',
                    text: '${text.replace(/'/g, "\\'")}',
                    done: function(err, msg) {
                        console.log('sendText done callback:', err, msg);
                        if (err) {
                            resolve({ 
                                success: false, 
                                error: err.message || String(err),
                                code: err.code
                            });
                        } else {
                            resolve({ 
                                success: true, 
                                idServer: msg?.idServer,
                                to: msg?.to,
                                time: msg?.time
                            });
                        }
                    }
                });
                
                setTimeout(function() {
                    resolve({ success: false, error: 'Timeout after 8s' });
                }, 8000);
            });
        } catch(e) {
            return { success: false, error: e.message };
        }
    })()`);
    
    if (result?.success) {
        console.log(`   ✅ 发送成功!`);
        console.log(`   消息ID: ${result.idServer}`);
        console.log(`   目标: ${result.to}`);
    } else {
        console.log(`   ❌ 发送失败: ${result?.error}`);
        console.log(`   错误码: ${result?.code}`);
    }
    
    return result;
}

async function main() {
    console.log('🤖 私聊自动回复机器人 V2\n');
    console.log(`目标账号: ${LOGO_ACCOUNT} (logo)`);
    console.log(`回复内容: ${AUTO_REPLY}\n`);
    
    const wsUrl = await getWebSocketUrl();
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    console.log('✅ 已连接旺商聊\n');
    
    // 获取当前账号
    const myInfo = await evaluate(`(async () => {
        return new Promise(r => {
            window.nim.getMyInfo({ done: (e, i) => r(i || {}) });
            setTimeout(() => r({}), 5000);
        });
    })()`);
    console.log('机器人账号:', myInfo?.account);
    
    // 先发送一条测试消息
    console.log('\n=== 测试发送私聊消息 ===');
    await sendReply(LOGO_ACCOUNT, '[测试] 机器人已启动 ' + new Date().toLocaleTimeString());
    
    // 安装实时消息监听
    console.log('\n=== 安装消息监听 ===\n');
    
    await evaluate(`(() => {
        // 清空队列
        window.__p2pQueue = [];
        window.__processedIds = new Set();
        
        // 保存原始回调
        if (!window.__origOnmsgV2) {
            window.__origOnmsgV2 = window.nim.options?.onmsg;
        }
        
        // 安装新回调
        window.nim.options.onmsg = function(msg) {
            console.log('onmsg收到消息:', msg.scene, msg.from, msg.type);
            
            // 只处理私聊入站消息
            if (msg.scene === 'p2p' && msg.flow === 'in') {
                // 防重复
                var msgKey = msg.idClient || (msg.from + '-' + msg.time);
                if (!window.__processedIds.has(msgKey)) {
                    window.__processedIds.add(msgKey);
                    window.__p2pQueue.push({
                        from: msg.from,
                        text: msg.text || '',
                        type: msg.type,
                        time: Date.now(),
                        idClient: msg.idClient,
                        content: msg.content ? JSON.stringify(msg.content).substring(0, 100) : ''
                    });
                    console.log('添加到队列:', msg.from, msg.text);
                }
            }
            
            // 调用原始回调
            if (window.__origOnmsgV2) {
                window.__origOnmsgV2(msg);
            }
        };
        
        return { success: true };
    })()`, false);
    
    console.log('✅ 消息监听已安装');
    console.log('\n⏳ 开始监听...');
    console.log('请从 logo 账号发送私聊消息!\n');
    console.log('========================================\n');
    
    // 轮询处理
    let lastMsgTime = 0;
    
    while (true) {
        try {
            const msgs = await evaluate(`(() => {
                var queue = window.__p2pQueue || [];
                window.__p2pQueue = [];
                return queue;
            })()`, false);
            
            for (const msg of (msgs || [])) {
                const time = new Date(msg.time).toLocaleTimeString();
                
                console.log(`📩 [${time}] 收到私聊消息!`);
                console.log(`   来自: ${msg.from}`);
                console.log(`   类型: ${msg.type}`);
                console.log(`   文本: "${msg.text || '(空)'}"`);
                console.log(`   内容: ${msg.content || '(无)'}`);
                
                // 检查是否是 logo 发的
                if (msg.from === LOGO_ACCOUNT) {
                    console.log(`   ✅ 是 logo 发的消息，准备回复...`);
                    await sendReply(LOGO_ACCOUNT, AUTO_REPLY);
                } else {
                    console.log(`   ⚠️ 不是 logo 发的，跳过`);
                }
                
                console.log('');
            }
        } catch (e) {
            if (!e.message.includes('Timeout')) {
                console.error('错误:', e.message);
            }
        }
        
        await new Promise(r => setTimeout(r, 300));
    }
}

main().catch(e => {
    console.error('启动失败:', e.message);
    process.exit(1);
});
