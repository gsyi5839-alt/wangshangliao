/**
 * 深入分析旺商聊的消息协议格式
 */
const WebSocket = require('ws');
const http = require('http');
const crypto = require('crypto');

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

function base64ToHex(b64) {
    let std = b64.replace(/-/g, '+').replace(/_/g, '/');
    const mod = std.length % 4;
    if (mod) std += '='.repeat(4 - mod);
    return Buffer.from(std, 'base64').toString('hex');
}

function analyzeProtocol(hex) {
    console.log('HEX分析:');
    console.log('  完整:', hex);
    console.log('  长度:', hex.length / 2, '字节');
    
    // 分析头部
    console.log('\n头部分析:');
    console.log('  字节0:', hex.substring(0, 2), '- 协议版本?');
    console.log('  字节1-4:', hex.substring(2, 10), '- 魔数 (d5d77109)');
    console.log('  字节5-8:', hex.substring(10, 18), '- 子类型? (9c559303)');
    console.log('  字节9-12:', hex.substring(18, 26), '- 标识? (1192/117c)');
    console.log('  字节13-16:', hex.substring(26, 34), '- 时间戳低位?');
    console.log('  字节17-20:', hex.substring(34, 42), '- 填充?');
    console.log('  字节21-24:', hex.substring(42, 50), '- 更多头部');
    
    // 提取可能的加密数据部分
    const payload = hex.substring(50);
    console.log('\n载荷部分:');
    console.log('  起始位置: 字节25');
    console.log('  载荷长度:', payload.length / 2, '字节');
    console.log('  载荷HEX:', payload.substring(0, 64) + '...');
    
    return payload;
}

async function main() {
    console.log('🔍 深入分析旺商聊消息协议\n');
    
    const wsUrl = await getWebSocketUrl();
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    console.log('✅ 已连接\n');
    
    // 获取多条消息进行对比分析
    const msgs = await evaluate(`(async () => {
        return new Promise(r => {
            window.nim.getHistoryMsgs({
                scene: 'p2p',
                to: '${LOGO_ACCOUNT}',
                limit: 20,
                done: (err, obj) => {
                    if (err) r([]);
                    else r((obj?.msgs || []).filter(m => m.type === 'custom'));
                }
            });
            setTimeout(() => r([]), 10000);
        });
    })()`);
    
    console.log('=== 消息协议分析 ===\n');
    console.log(`共 ${msgs?.length || 0} 条custom消息\n`);
    
    const customMsgs = (msgs || []).filter(m => m.content);
    
    customMsgs.slice(0, 5).forEach((msg, i) => {
        console.log(`\n========== 消息 ${i + 1} (${msg.flow === 'in' ? '收到' : '发出'}) ==========`);
        console.log('时间:', new Date(msg.time).toLocaleTimeString());
        
        try {
            const content = typeof msg.content === 'string' ? JSON.parse(msg.content) : msg.content;
            if (content.b) {
                const hex = base64ToHex(content.b);
                analyzeProtocol(hex);
            }
        } catch (e) {
            console.log('解析失败:', e.message);
        }
    });
    
    // 查找旺商聊中的消息编码函数
    console.log('\n\n=== 搜索消息编码函数 ===\n');
    const encodeFuncs = await evaluate(`(() => {
        var results = [];
        
        // 搜索可能的消息编码相关代码
        function searchObject(obj, path, depth) {
            if (depth > 3 || !obj) return;
            
            for (var key in obj) {
                try {
                    var val = obj[key];
                    var fullPath = path + '.' + key;
                    
                    if (typeof val === 'function') {
                        var funcStr = val.toString().substring(0, 200);
                        if (funcStr.includes('content') && 
                            (funcStr.includes('encode') || funcStr.includes('encrypt') || 
                             funcStr.includes('pack') || funcStr.includes('Buffer'))) {
                            results.push({
                                path: fullPath,
                                preview: funcStr.substring(0, 100)
                            });
                        }
                    }
                } catch(e) {}
            }
        }
        
        // 搜索nim对象
        searchObject(window.nim, 'nim', 0);
        
        return results.slice(0, 10);
    })()`, false);
    console.log('找到的编码函数:', encodeFuncs);
    
    // 直接在旺商聊中测试发送文本消息
    console.log('\n=== 测试直接调用sendText ===\n');
    const textResult = await evaluate(`(async () => {
        return new Promise(r => {
            window.nim.sendText({
                scene: 'p2p',
                to: '${LOGO_ACCOUNT}',
                text: '【直接文本测试】' + new Date().toLocaleTimeString(),
                done: (err, msg) => {
                    r({
                        error: err?.message,
                        code: err?.code,
                        type: msg?.type,
                        text: msg?.text?.substring(0, 50),
                        content: msg?.content ? msg.content.substring(0, 100) : null,
                        idServer: msg?.idServer,
                        status: msg?.status
                    });
                }
            });
            setTimeout(() => r({ error: 'Timeout' }), 10000);
        });
    })()`);
    console.log('sendText结果:', textResult);
    
    // 检查是否有消息拦截器
    console.log('\n=== 检查消息拦截器/中间件 ===\n');
    const interceptors = await evaluate(`(() => {
        var results = {
            beforeSendMsgHook: !!window.nim.beforeSendMsg,
            afterSendMsgHook: !!window.nim.afterSendMsg,
            msgInterceptor: !!window.nim.msgInterceptor,
            sendMsgValidate: !!window.nim.sendMsgValidate,
            options: {}
        };
        
        // 检查nim.options中的回调
        if (window.nim.options) {
            results.options.beforeSendMsgEnabled = !!window.nim.options.beforeSendMsg;
            results.options.afterSendMsgEnabled = !!window.nim.options.afterSendMsg;
            results.options.onbeforeSendMsg = !!window.nim.options.onbeforeSendMsg;
        }
        
        return results;
    })()`, false);
    console.log('拦截器检查:', interceptors);
    
    console.log('\n');
    ws.close();
}

main().catch(console.error);
