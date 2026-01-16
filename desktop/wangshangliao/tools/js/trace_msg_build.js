/**
 * 追踪消息构建过程
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
    console.log('🔍 追踪消息构建过程\n');
    
    const wsUrl = await getWebSocketUrl();
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    console.log('✅ 已连接\n');
    
    // 搜索所有可能的消息构建相关代码
    console.log('=== 1. 搜索所有stores ===\n');
    const allStores = await evaluate(`(() => {
        try {
            var pinia = window.__pinia || document.querySelector('#app')?.__vue_app__?.config?.globalProperties?.$pinia;
            var stores = [];
            pinia?._s?.forEach((store, name) => {
                stores.push({
                    name: name,
                    methods: Object.keys(store).filter(k => typeof store[k] === 'function' && !k.startsWith('$'))
                });
            });
            return stores;
        } catch(e) {
            return { error: e.message };
        }
    })()`, false);
    console.log('Stores:');
    (allStores || []).forEach(s => {
        console.log(`  ${s.name}: ${s.methods?.length || 0} methods`);
        // 显示可能相关的方法
        const relevant = (s.methods || []).filter(m => 
            m.includes('send') || m.includes('msg') || m.includes('encode') || m.includes('pack') || m.includes('build'));
        if (relevant.length > 0) {
            console.log('    相关方法:', relevant.join(', '));
        }
    });
    
    // 查找SDK store详细分析
    console.log('\n=== 2. 详细分析SDK store ===\n');
    const sdkDetail = await evaluate(`(() => {
        try {
            var pinia = window.__pinia || document.querySelector('#app')?.__vue_app__?.config?.globalProperties?.$pinia;
            var sdkStore = pinia?._s?.get('sdk');
            
            if (!sdkStore) return { error: 'SDK store not found' };
            
            // 获取所有属性和方法
            var result = {
                state: {},
                methods: {}
            };
            
            for (var key in sdkStore) {
                if (key.startsWith('$') || key.startsWith('_')) continue;
                
                if (typeof sdkStore[key] === 'function') {
                    // 获取原始方法体
                    var fnStr = '';
                    try {
                        // 尝试获取未包装的方法
                        var originalAction = sdkStore.$options?.actions?.[key];
                        fnStr = originalAction ? originalAction.toString() : sdkStore[key].toString();
                    } catch(e) {
                        fnStr = sdkStore[key].toString();
                    }
                    
                    if (key.includes('send') || key.includes('Msg') || key.includes('nim')) {
                        result.methods[key] = {
                            length: sdkStore[key].length,
                            preview: fnStr.substring(0, 400)
                        };
                    }
                } else {
                    if (key.toLowerCase().includes('nim') || key.toLowerCase().includes('msg')) {
                        result.state[key] = typeof sdkStore[key];
                    }
                }
            }
            
            return result;
        } catch(e) {
            return { error: e.message };
        }
    })()`, false);
    console.log('SDK store详情:');
    console.log('State:', sdkDetail?.state);
    console.log('\nMethods:');
    for (const [name, info] of Object.entries(sdkDetail?.methods || {})) {
        console.log(`\n  ${name}:`);
        console.log('  Preview:', info.preview?.substring(0, 200));
    }
    
    // 直接检查nim对象中是否有编码相关方法
    console.log('\n\n=== 3. 检查nim原型链上的方法 ===\n');
    const nimMethods = await evaluate(`(() => {
        var methods = [];
        var proto = window.nim;
        var depth = 0;
        
        while (proto && depth < 3) {
            for (var key in proto) {
                try {
                    if (typeof proto[key] === 'function' && 
                        (key.includes('encode') || key.includes('pack') || key.includes('build') || 
                         key.includes('custom') || key.includes('msg') || key.includes('send'))) {
                        methods.push({ 
                            name: key, 
                            depth: depth,
                            preview: proto[key].toString().substring(0, 200)
                        });
                    }
                } catch(e) {}
            }
            proto = Object.getPrototypeOf(proto);
            depth++;
        }
        
        return methods;
    })()`, false);
    console.log('NIM相关方法:');
    (nimMethods || []).slice(0, 10).forEach(m => {
        console.log(`\n  ${m.name} (depth: ${m.depth}):`);
        console.log('  ', m.preview?.substring(0, 150));
    });
    
    // 直接在源码中搜索构建消息content的位置
    console.log('\n\n=== 4. 尝试直接调用旺商聊的消息构建 ===\n');
    const buildResult = await evaluate(`(async () => {
        try {
            // 找到SDK store的sendNimMsg action的原始定义
            var pinia = window.__pinia || document.querySelector('#app')?.__vue_app__?.config?.globalProperties?.$pinia;
            var sdkStore = pinia?._s?.get('sdk');
            
            // 尝试直接调用
            if (sdkStore && sdkStore.sendNimMsg) {
                var result = await sdkStore.sendNimMsg({
                    scene: 'p2p',
                    to: '${LOGO_ACCOUNT}',
                    text: '直接调用测试',
                    type: 'text'
                });
                return { 
                    called: true, 
                    result: result ? JSON.stringify(result).substring(0, 200) : 'void'
                };
            }
            
            return { error: 'sendNimMsg not available' };
        } catch(e) {
            return { error: e.message, stack: e.stack?.substring(0, 300) };
        }
    })()`);
    console.log('sendNimMsg调用结果:', buildResult);
    
    // 最后，检查是否有专门的消息编码器
    console.log('\n\n=== 5. 检查全局消息编码器 ===\n');
    const encoderSearch = await evaluate(`(() => {
        var results = [];
        
        // 搜索常见的编码器命名模式
        var patterns = ['Encoder', 'Packer', 'Builder', 'Formatter', 'Protocol', 'Codec'];
        
        for (var key in window) {
            try {
                if (patterns.some(p => key.includes(p))) {
                    results.push({ name: key, type: typeof window[key] });
                }
            } catch(e) {}
        }
        
        // 检查nim.options中的编码配置
        if (window.nim && window.nim.options) {
            for (var k in window.nim.options) {
                if (patterns.some(p => k.toLowerCase().includes(p.toLowerCase()))) {
                    results.push({ name: 'nim.options.' + k, type: typeof window.nim.options[k] });
                }
            }
        }
        
        return results;
    })()`, false);
    console.log('编码器搜索:', encoderSearch);
    
    console.log('\n');
    ws.close();
}

main().catch(console.error);
