/**
 * 修复自定义消息解析和私聊回复
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
    console.log('🔧 修复自定义消息解析和私聊回复\n');
    
    const wsUrl = await getWebSocketUrl();
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    console.log('✅ 已连接\n');
    
    // 1. 安装增强的消息Hook（解析custom消息内容）
    console.log('=== 1. 安装增强消息Hook ===\n');
    const hookResult = await evaluate(`(() => {
        // 解析自定义消息内容
        function extractCustomText(msg) {
            if (msg.text) return msg.text;
            
            if (msg.content) {
                try {
                    var content = typeof msg.content === 'string' ? JSON.parse(msg.content) : msg.content;
                    // 尝试各种可能的文本字段
                    return content.text || content.msg || content.message || 
                           content.data?.text || content.data?.msg ||
                           content.body?.text || content.body?.msg ||
                           (content.type === 1 && content.data) || // type=1 可能是文本
                           '';
                } catch(e) {}
            }
            return '';
        }
        
        // 初始化
        window.__p2pMessages = [];
        
        // 保存原始回调
        if (!window.__origOnmsgSaved) {
            window.__origOnmsgSaved = window.nim.options?.onmsg;
        }
        
        // 安装新Hook
        window.nim.options.onmsg = function(msg) {
            // 解析消息内容
            var text = extractCustomText(msg);
            
            // 存储消息（包含解析后的内容）
            window.__p2pMessages.push({
                time: Date.now(),
                scene: msg.scene,
                from: msg.from,
                to: msg.to,
                type: msg.type,
                text: text,
                rawText: msg.text || '',
                content: msg.content,
                flow: msg.flow || '',
                idClient: msg.idClient || ''
            });
            
            // 只保留最近50条
            if (window.__p2pMessages.length > 50) {
                window.__p2pMessages.shift();
            }
            
            // 调用原始回调
            if (window.__origOnmsgSaved) {
                window.__origOnmsgSaved(msg);
            }
        };
        
        return { success: true };
    })()`, false);
    
    console.log('✅ 增强Hook已安装\n');
    
    // 2. 测试直接发送私聊消息
    console.log('=== 2. 测试直接发送私聊消息 ===\n');
    
    const sendTest = await evaluate(`(async () => {
        try {
            return new Promise((resolve) => {
                window.nim.sendText({
                    scene: 'p2p',
                    to: '${LOGO_ACCOUNT}',
                    text: '【测试】直接发送私聊消息 ' + new Date().toLocaleTimeString(),
                    done: function(err, msg) {
                        if (err) {
                            resolve({ success: false, error: err.message || String(err), code: err.code });
                        } else {
                            resolve({ success: true, idServer: msg?.idServer, to: msg?.to });
                        }
                    }
                });
                
                // 超时
                setTimeout(function() {
                    resolve({ success: false, error: 'Timeout' });
                }, 8000);
            });
        } catch(e) {
            return { success: false, error: e.message };
        }
    })()`);
    
    if (sendTest?.success) {
        console.log('✅ 私聊消息发送成功!');
        console.log('   目标:', sendTest.to);
        console.log('   消息ID:', sendTest.idServer);
    } else {
        console.log('❌ 发送失败:', sendTest?.error, '(code:', sendTest?.code, ')');
    }
    
    // 3. 监听并自动回复
    console.log('\n=== 3. 监听私聊消息（30秒） ===\n');
    console.log('请从 logo 账号发送文字消息（如：你好）...\n');
    
    for (let i = 0; i < 30; i++) {
        await new Promise(r => setTimeout(r, 1000));
        
        const msgs = await evaluate(`(() => {
            return window.__p2pMessages?.filter(m => 
                m.scene === 'p2p' && 
                m.flow === 'in' && 
                m.from === '${LOGO_ACCOUNT}'
            ) || [];
        })()`, false);
        
        if (msgs?.length > 0) {
            console.log('\n📩 收到 logo 私聊消息:');
            msgs.forEach((m, idx) => {
                console.log(`  ${idx + 1}. 类型: ${m.type}`);
                console.log(`     原始text: "${m.rawText || '(空)'}"`);
                console.log(`     解析text: "${m.text || '(空)'}"`);
                if (m.content) {
                    console.log(`     content: ${JSON.stringify(m.content).substring(0, 100)}...`);
                }
            });
            
            // 如果有消息内容，发送回复
            const lastMsg = msgs[msgs.length - 1];
            const msgContent = lastMsg.text || lastMsg.rawText || '收到消息';
            
            console.log('\n📤 发送自动回复...');
            const reply = await evaluate(`(async () => {
                return new Promise((resolve) => {
                    window.nim.sendText({
                        scene: 'p2p',
                        to: '${LOGO_ACCOUNT}',
                        text: '【机器人回复】您发送了: ${msgContent.substring(0, 20)}',
                        done: function(err, msg) {
                            if (err) resolve({ success: false, error: err.message });
                            else resolve({ success: true });
                        }
                    });
                    setTimeout(() => resolve({ success: false, error: 'Timeout' }), 5000);
                });
            })()`);
            
            console.log(reply?.success ? '✅ 回复成功!' : '❌ 回复失败: ' + reply?.error);
            break;
        }
        
        process.stdout.write(`\r等待... ${30 - i}秒 (私聊消息: ${msgs?.length || 0})`);
    }
    
    console.log('\n');
    ws.close();
}

main().catch(console.error);
