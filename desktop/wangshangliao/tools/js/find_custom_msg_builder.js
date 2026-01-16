/**
 * 找到旺商聊的customMsg构建器
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
    console.log('🔍 查找customMsg构建器\n');
    
    const wsUrl = await getWebSocketUrl();
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    console.log('✅ 已连接\n');
    
    // 1. 检查SDK store的customMsg
    console.log('=== 1. 检查SDK store的customMsg ===\n');
    const customMsgInfo = await evaluate(`(() => {
        try {
            var app = document.querySelector('#app')?.__vue_app__;
            var pinia = app?.config?.globalProperties?.$pinia;
            var sdkStore = pinia?._s?.get('sdk');
            
            if (!sdkStore) return { error: 'SDK store not found' };
            
            var customMsg = sdkStore.customMsg;
            if (!customMsg) return { error: 'customMsg not found' };
            
            // 分析customMsg对象
            return {
                type: typeof customMsg,
                isObject: typeof customMsg === 'object',
                keys: Object.keys(customMsg),
                methods: Object.keys(customMsg).filter(k => typeof customMsg[k] === 'function'),
                preview: JSON.stringify(customMsg).substring(0, 500)
            };
        } catch(e) {
            return { error: e.message };
        }
    })()`, false);
    console.log('customMsg:', customMsgInfo);
    
    // 2. 如果customMsg是构建器，尝试找到其方法
    console.log('\n=== 2. 分析customMsg方法 ===\n');
    const customMsgMethods = await evaluate(`(() => {
        try {
            var app = document.querySelector('#app')?.__vue_app__;
            var pinia = app?.config?.globalProperties?.$pinia;
            var sdkStore = pinia?._s?.get('sdk');
            var customMsg = sdkStore?.customMsg;
            
            if (!customMsg || typeof customMsg !== 'object') return { error: 'Not an object' };
            
            var methods = {};
            for (var key in customMsg) {
                if (typeof customMsg[key] === 'function') {
                    methods[key] = {
                        argCount: customMsg[key].length,
                        preview: customMsg[key].toString().substring(0, 300)
                    };
                }
            }
            
            return methods;
        } catch(e) {
            return { error: e.message };
        }
    })()`, false);
    console.log('customMsg方法:', customMsgMethods);
    
    // 3. 搜索全局作用域中的消息编码函数
    console.log('\n=== 3. 搜索全局消息编码函数 ===\n');
    const globalEncoders = await evaluate(`(() => {
        var results = [];
        
        // 搜索常见的编码函数名
        var searchKeys = ['encodeMsg', 'packMsg', 'buildMsg', 'createMsg', 'msgBuilder', 'msgEncoder', 'customBuilder'];
        
        function searchObj(obj, path, depth) {
            if (depth > 2 || !obj) return;
            
            for (var key in obj) {
                try {
                    var lowerKey = key.toLowerCase();
                    if (searchKeys.some(sk => lowerKey.includes(sk.toLowerCase()))) {
                        results.push({
                            path: path + '.' + key,
                            type: typeof obj[key]
                        });
                    }
                    
                    if (typeof obj[key] === 'object' && obj[key] !== null && depth < 2) {
                        searchObj(obj[key], path + '.' + key, depth + 1);
                    }
                } catch(e) {}
            }
        }
        
        searchObj(window, 'window', 0);
        searchObj(window.nim, 'nim', 0);
        
        return results.slice(0, 20);
    })()`, false);
    console.log('全局编码函数:', globalEncoders);
    
    // 4. Hook UI发送按钮，追踪完整调用链
    console.log('\n=== 4. 追踪完整发送调用链 ===\n');
    await evaluate(`(() => {
        window.__callChain = [];
        
        // 深度Hook
        var origCustomMsg = window.nim.sendCustomMsg.bind(window.nim);
        window.nim.sendCustomMsg = function(opts) {
            var stack = new Error().stack;
            window.__callChain.push({
                method: 'sendCustomMsg',
                time: Date.now(),
                content: opts.content?.substring(0, 100),
                stack: stack?.substring(0, 500)
            });
            return origCustomMsg(opts);
        };
        
        return true;
    })()`, false);
    
    // 5. 模拟发送并捕获调用链
    console.log('=== 5. 模拟发送捕获调用链 ===\n');
    const simulateResult = await evaluate(`(() => {
        // 在输入框输入文字
        var input = document.querySelector('[contenteditable="true"]');
        if (!input) return { error: '未找到输入框' };
        
        input.focus();
        input.textContent = 'Test Message';
        input.dispatchEvent(new Event('input', { bubbles: true }));
        
        // 点击发送
        var sendBtn = null;
        document.querySelectorAll('button').forEach(btn => {
            if (btn.textContent?.includes('发送')) sendBtn = btn;
        });
        
        if (sendBtn) {
            sendBtn.click();
            return { success: true };
        }
        return { error: '未找到发送按钮' };
    })()`, false);
    console.log('模拟发送:', simulateResult);
    
    await new Promise(r => setTimeout(r, 1000));
    
    const callChain = await evaluate(`(() => window.__callChain || [])()`, false);
    console.log('\n调用链:');
    (callChain || []).forEach((c, i) => {
        console.log(`\n${i + 1}. ${c.method}`);
        console.log('Content:', c.content);
        console.log('Stack:', c.stack);
    });
    
    // 6. 直接在源码中搜索消息打包逻辑
    console.log('\n\n=== 6. 搜索源码中的打包逻辑 ===\n');
    const sourceSearch = await evaluate(`(() => {
        // 获取所有script标签
        var scripts = document.querySelectorAll('script');
        var results = [];
        
        scripts.forEach(s => {
            if (s.src) {
                results.push({ type: 'external', src: s.src });
            } else if (s.textContent && s.textContent.length > 100) {
                // 检查内联脚本
                if (s.textContent.includes('sendCustomMsg') || s.textContent.includes('packMsg')) {
                    results.push({
                        type: 'inline',
                        preview: s.textContent.substring(0, 200),
                        length: s.textContent.length
                    });
                }
            }
        });
        
        return results.slice(0, 5);
    })()`, false);
    console.log('源码搜索:', sourceSearch);
    
    console.log('\n');
    ws.close();
}

main().catch(console.error);
