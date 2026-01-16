/**
 * 逆向分析旺商聊消息发送机制
 * 抓取真实发送消息时的完整参数和流程
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
        const timeout = setTimeout(() => reject(new Error('Timeout')), 30000);
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
    console.log('🔍 逆向分析旺商聊消息发送机制\n');
    
    const wsUrl = await getWebSocketUrl();
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    console.log('✅ 已连接\n');
    
    // 1. Hook sendText 方法，捕获所有调用
    console.log('=== 1. Hook nim.sendText 分析调用参数 ===\n');
    await evaluate(`(() => {
        // 保存原始方法
        window.__origSendText = window.__origSendText || window.nim.sendText.bind(window.nim);
        
        // Hook sendText
        window.nim.sendText = function(options) {
            console.log('[HOOK] sendText called with:', JSON.stringify(options, null, 2));
            window.__lastSendTextOptions = options;
            window.__lastSendTextTime = Date.now();
            
            // 调用原始方法
            return window.__origSendText(options);
        };
        
        return true;
    })()`, false);
    console.log('✅ Hook 已安装\n');
    
    // 2. 分析 nim 对象的发送相关方法
    console.log('=== 2. 分析 nim 发送相关方法 ===\n');
    const sendMethods = await evaluate(`(() => {
        var methods = [];
        for (var key in window.nim) {
            if (typeof window.nim[key] === 'function' && 
                (key.toLowerCase().includes('send') || key.toLowerCase().includes('msg'))) {
                methods.push({
                    name: key,
                    length: window.nim[key].length
                });
            }
        }
        return methods.sort((a,b) => a.name.localeCompare(b.name));
    })()`, false);
    
    console.log('发送相关方法:');
    (sendMethods || []).forEach(m => {
        console.log(`  - ${m.name}(${m.length} params)`);
    });
    
    // 3. 检查 nim 的 options 配置
    console.log('\n=== 3. 检查 nim.options 配置 ===\n');
    const nimOptions = await evaluate(`(() => {
        var opts = window.nim.options || {};
        return {
            account: opts.account,
            appKey: opts.appKey?.substring(0, 20) + '...',
            transports: opts.transports,
            db: opts.db,
            syncSessionUnread: opts.syncSessionUnread,
            // 检查是否有自定义发送配置
            customSendConfig: opts.customSendConfig,
            // 检查消息加密配置
            encryptConfig: opts.encryptConfig
        };
    })()`, false);
    console.log('NIM Options:');
    console.log(JSON.stringify(nimOptions, null, 2));
    
    // 4. 分析 Pinia store 中的发送方法
    console.log('\n=== 4. 分析 Pinia sdkStore 发送方法 ===\n');
    const piniaMethods = await evaluate(`(() => {
        try {
            var app = document.querySelector('#app');
            var gp = app?.__vue_app__?.config?.globalProperties;
            var pinia = gp?.$pinia;
            var sdkStore = pinia?._s?.get('sdkStore');
            
            if (!sdkStore) return { error: 'sdkStore not found' };
            
            var methods = [];
            for (var key in sdkStore) {
                if (typeof sdkStore[key] === 'function' && 
                    (key.toLowerCase().includes('send') || key.toLowerCase().includes('msg'))) {
                    methods.push(key);
                }
            }
            
            return {
                methods: methods,
                hasNim: !!sdkStore.nim,
                storeKeys: Object.keys(sdkStore).filter(k => !k.startsWith('$')).slice(0, 20)
            };
        } catch(e) {
            return { error: e.message };
        }
    })()`, false);
    console.log('Pinia sdkStore:');
    console.log(JSON.stringify(piniaMethods, null, 2));
    
    // 5. 搜索页面中的发送消息相关代码
    console.log('\n=== 5. 搜索 Vue 组件中的发送方法 ===\n');
    const vueComponents = await evaluate(`(() => {
        var results = [];
        
        // 遍历所有Vue组件实例
        function findComponents(el) {
            if (!el) return;
            
            if (el.__vue__ || el._vnode?.component) {
                var comp = el.__vue__ || el._vnode?.component?.proxy;
                if (comp) {
                    var methods = [];
                    for (var key in comp) {
                        if (typeof comp[key] === 'function' && 
                            (key.toLowerCase().includes('send') || 
                             key.toLowerCase().includes('submit') ||
                             key.toLowerCase().includes('message'))) {
                            methods.push(key);
                        }
                    }
                    if (methods.length > 0) {
                        results.push({
                            tag: el.tagName,
                            className: el.className?.substring(0, 50),
                            methods: methods
                        });
                    }
                }
            }
            
            Array.from(el.children || []).forEach(findComponents);
        }
        
        findComponents(document.body);
        return results.slice(0, 10);
    })()`, false);
    console.log('Vue组件发送方法:');
    console.log(JSON.stringify(vueComponents, null, 2));
    
    // 6. 检查消息是否需要特殊格式
    console.log('\n=== 6. 分析已发送消息的完整结构 ===\n');
    const sentMsgStructure = await evaluate(`(async () => {
        return new Promise(r => {
            window.nim.getHistoryMsgs({
                scene: 'p2p',
                to: '${LOGO_ACCOUNT}',
                limit: 1,
                done: (err, obj) => {
                    if (err || !obj?.msgs?.length) {
                        r({ error: err?.message || 'No messages' });
                    } else {
                        var msg = obj.msgs[0];
                        // 返回完整消息结构
                        r({
                            // 基本字段
                            scene: msg.scene,
                            from: msg.from,
                            to: msg.to,
                            type: msg.type,
                            text: msg.text,
                            
                            // 消息ID
                            idClient: msg.idClient,
                            idServer: msg.idServer,
                            
                            // 状态
                            status: msg.status,
                            flow: msg.flow,
                            
                            // 时间
                            time: msg.time,
                            
                            // 所有键
                            allKeys: Object.keys(msg),
                            
                            // 可能的加密字段
                            custom: msg.custom,
                            content: msg.content ? JSON.stringify(msg.content).substring(0, 200) : null,
                            attach: msg.attach,
                            pushContent: msg.pushContent,
                            pushPayload: msg.pushPayload,
                            
                            // 配置字段
                            isHistoryable: msg.isHistoryable,
                            isRoamingable: msg.isRoamingable,
                            isSyncable: msg.isSyncable,
                            isPushable: msg.isPushable,
                            needPushNick: msg.needPushNick,
                            
                            // 完整JSON (截取)
                            fullJson: JSON.stringify(msg).substring(0, 1000)
                        });
                    }
                }
            });
            setTimeout(() => r({ error: 'Timeout' }), 10000);
        });
    })()`);
    console.log('已发送消息结构:');
    console.log(JSON.stringify(sentMsgStructure, null, 2));
    
    // 7. 尝试使用 sendMsg 而不是 sendText
    console.log('\n=== 7. 尝试 sendMsg 方法（完整参数） ===\n');
    const sendMsgResult = await evaluate(`(async () => {
        return new Promise(r => {
            var msg = window.nim.buildTextMsg({
                scene: 'p2p',
                to: '${LOGO_ACCOUNT}',
                text: '【buildTextMsg测试】' + new Date().toLocaleTimeString(),
                done: function(err, builtMsg) {
                    if (err) {
                        r({ buildError: err.message });
                        return;
                    }
                    
                    console.log('[DEBUG] Built msg:', builtMsg);
                    
                    // 发送构建好的消息
                    window.nim.sendMsg({
                        msg: builtMsg,
                        done: function(sendErr, sentMsg) {
                            if (sendErr) {
                                r({ sendError: sendErr.message, code: sendErr.code });
                            } else {
                                r({
                                    success: true,
                                    idServer: sentMsg?.idServer,
                                    status: sentMsg?.status,
                                    to: sentMsg?.to
                                });
                            }
                        }
                    });
                }
            });
            
            setTimeout(() => r({ error: 'Timeout' }), 15000);
        });
    })()`);
    console.log('sendMsg 结果:');
    console.log(JSON.stringify(sendMsgResult, null, 2));
    
    console.log('\n========================================\n');
    
    ws.close();
}

main().catch(console.error);
