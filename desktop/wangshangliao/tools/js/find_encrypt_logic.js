/**
 * 找到旺商聊的消息加密逻辑
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
    console.log('🔍 查找旺商聊消息加密逻辑\n');
    
    const wsUrl = await getWebSocketUrl();
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    console.log('✅ 已连接\n');
    
    // 1. 搜索Pinia store中的加密/编码方法
    console.log('=== 1. 搜索Pinia store中的方法 ===\n');
    const storeMethods = await evaluate(`(() => {
        try {
            var app = document.querySelector('#app')?.__vue_app__;
            var pinia = app?.config?.globalProperties?.$pinia;
            
            var results = {};
            pinia?._s?.forEach((store, name) => {
                var interesting = [];
                for (var key in store) {
                    if (typeof store[key] === 'function' && !key.startsWith('$') && !key.startsWith('_')) {
                        var fnStr = store[key].toString();
                        if (fnStr.includes('encrypt') || fnStr.includes('encode') || 
                            fnStr.includes('Buffer') || fnStr.includes('btoa') ||
                            fnStr.includes('content') || fnStr.includes('custom')) {
                            interesting.push(key);
                        }
                    }
                }
                if (interesting.length > 0) {
                    results[name] = interesting;
                }
            });
            
            return results;
        } catch(e) {
            return { error: e.message };
        }
    })()`, false);
    console.log('相关方法:', storeMethods);
    
    // 2. 查找SDK store的sendNimMsg实现
    console.log('\n=== 2. 分析sendNimMsg源码 ===\n');
    const sendNimMsgSource = await evaluate(`(() => {
        try {
            var app = document.querySelector('#app')?.__vue_app__;
            var pinia = app?.config?.globalProperties?.$pinia;
            var sdkStore = pinia?._s?.get('sdk');
            
            // 尝试获取原始action定义
            var actionDef = sdkStore?.$options?.actions?.sendNimMsg;
            if (actionDef) {
                return { 
                    found: true, 
                    source: actionDef.toString().substring(0, 1500) 
                };
            }
            
            // 尝试从store state获取
            var state = sdkStore?.$state;
            return { 
                found: false, 
                stateKeys: state ? Object.keys(state).slice(0, 20) : [],
                hint: 'Check sendNimAutoReplyMsg or sendNoticeCustomMsg'
            };
        } catch(e) {
            return { error: e.message };
        }
    })()`, false);
    console.log('sendNimMsg源码:', sendNimMsgSource);
    
    // 3. Hook JSON.stringify 来捕获加密前的数据
    console.log('\n=== 3. Hook捕获加密过程 ===\n');
    await evaluate(`(() => {
        window.__encryptCaptures = [];
        
        // Hook 可能的加密入口
        var origStringify = JSON.stringify;
        JSON.stringify = function(obj) {
            if (obj && typeof obj === 'object' && obj.b && typeof obj.b === 'string' && obj.b.length > 50) {
                window.__encryptCaptures.push({
                    time: Date.now(),
                    input: { hasB: true, bLength: obj.b.length, bPreview: obj.b.substring(0, 50) }
                });
            }
            return origStringify.apply(this, arguments);
        };
        
        return true;
    })()`, false);
    
    // 4. 搜索全局window中的编码函数
    console.log('\n=== 4. 搜索全局编码函数 ===\n');
    const globalFuncs = await evaluate(`(() => {
        var results = [];
        
        // 常见加密库命名
        var keywords = ['Crypto', 'encode', 'encrypt', 'pack', 'serialize', 'Buffer', 'msgpack', 'protobuf'];
        
        for (var key in window) {
            try {
                if (keywords.some(k => key.toLowerCase().includes(k.toLowerCase()))) {
                    results.push({ name: key, type: typeof window[key] });
                }
            } catch(e) {}
        }
        
        return results;
    })()`, false);
    console.log('全局函数:', globalFuncs);
    
    // 5. 尝试直接获取旺商聊的自定义消息构建函数
    console.log('\n=== 5. 搜索Vue组件中的消息构建 ===\n');
    const componentSearch = await evaluate(`(() => {
        var results = [];
        
        // 遍历所有Vue组件
        function searchComponents(el, depth) {
            if (depth > 5 || !el) return;
            
            var comp = el.__vue__ || el._vnode?.component?.proxy;
            if (comp) {
                for (var key in comp) {
                    try {
                        if (typeof comp[key] === 'function') {
                            var fnStr = comp[key].toString();
                            // 查找可能构建消息内容的函数
                            if ((fnStr.includes('sendCustomMsg') || fnStr.includes('content') && fnStr.includes('b')) &&
                                fnStr.length < 2000) {
                                results.push({
                                    componentClass: el.className?.substring(0, 30),
                                    methodName: key,
                                    preview: fnStr.substring(0, 300)
                                });
                            }
                        }
                    } catch(e) {}
                }
            }
            
            Array.from(el.children || []).forEach(child => searchComponents(child, depth + 1));
        }
        
        searchComponents(document.body, 0);
        return results.slice(0, 5);
    })()`, false);
    console.log('组件方法:', componentSearch);
    
    // 6. 查找消息发送的中间件/拦截器
    console.log('\n=== 6. 查找消息发送中间件 ===\n');
    const middleware = await evaluate(`(() => {
        // 查找beforeSendMsg的实际实现
        var nimProto = Object.getPrototypeOf(window.nim);
        var methods = [];
        
        for (var key in nimProto) {
            if (typeof nimProto[key] === 'function' && key.includes('Send')) {
                methods.push({
                    name: key,
                    argCount: nimProto[key].length
                });
            }
        }
        
        // 检查nim.options中的hook
        var hooks = {};
        if (window.nim.options) {
            for (var k in window.nim.options) {
                if (k.includes('send') || k.includes('msg') || k.includes('before') || k.includes('after')) {
                    hooks[k] = typeof window.nim.options[k];
                }
            }
        }
        
        return { methods: methods.slice(0, 10), hooks: hooks };
    })()`, false);
    console.log('中间件:', middleware);
    
    // 7. 直接使用正确格式发送
    console.log('\n=== 7. 分析已发送消息的content格式 ===\n');
    const contentAnalysis = await evaluate(`(async () => {
        return new Promise(r => {
            window.nim.getHistoryMsgs({
                scene: 'p2p',
                to: '1391351554',
                limit: 10,
                done: (err, obj) => {
                    var customMsgs = (obj?.msgs || []).filter(m => m.type === 'custom' && m.content);
                    r(customMsgs.map(m => {
                        try {
                            var content = JSON.parse(m.content);
                            var b = content.b || '';
                            // URL-safe base64 转标准
                            var std = b.replace(/-/g, '+').replace(/_/g, '/');
                            var pad = std.length % 4;
                            if (pad) std += '='.repeat(4 - pad);
                            
                            // 解码base64
                            var bytes = atob(std);
                            var hex = '';
                            for (var i = 0; i < Math.min(bytes.length, 30); i++) {
                                hex += bytes.charCodeAt(i).toString(16).padStart(2, '0') + ' ';
                            }
                            
                            return {
                                flow: m.flow,
                                bLength: b.length,
                                byteLength: bytes.length,
                                hexPreview: hex,
                                time: new Date(m.time).toLocaleTimeString()
                            };
                        } catch(e) {
                            return { error: e.message };
                        }
                    }));
                }
            });
            setTimeout(() => r([]), 5000);
        });
    })()`);
    console.log('Content分析:');
    (contentAnalysis || []).forEach((c, i) => {
        console.log(`\n${i + 1}. [${c.flow}] @ ${c.time}`);
        console.log(`   b长度: ${c.bLength}, 字节: ${c.byteLength}`);
        console.log(`   HEX: ${c.hexPreview}`);
    });
    
    console.log('\n');
    ws.close();
}

main().catch(console.error);
