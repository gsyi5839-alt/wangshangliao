/**
 * 拦截消息加密过程，找出加密函数
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
    console.log('🔍 拦截消息加密过程\n');
    
    const wsUrl = await getWebSocketUrl();
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    console.log('✅ 已连接\n');
    
    // 拦截所有可能的加密入口
    console.log('=== 安装加密拦截Hook ===\n');
    await evaluate(`(() => {
        window.__encryptCalls = [];
        
        // Hook JSON.stringify 检查b字段的来源
        var origStringify = JSON.stringify;
        JSON.stringify = function(obj) {
            if (obj && typeof obj === 'object' && obj.b && typeof obj.b === 'string' && obj.b.length > 30) {
                window.__encryptCalls.push({
                    type: 'stringify',
                    time: Date.now(),
                    bLength: obj.b.length,
                    bPreview: obj.b.substring(0, 50),
                    stack: new Error().stack?.substring(0, 800)
                });
            }
            return origStringify.apply(this, arguments);
        };
        
        // Hook btoa
        var origBtoa = window.btoa;
        window.btoa = function(str) {
            if (str && str.length > 50) {
                window.__encryptCalls.push({
                    type: 'btoa',
                    time: Date.now(),
                    inputLength: str.length,
                    inputPreview: str.substring(0, 50),
                    stack: new Error().stack?.substring(0, 800)
                });
            }
            return origBtoa.apply(this, arguments);
        };
        
        // Hook ArrayBuffer和TypedArray的转换
        var origFromCharCode = String.fromCharCode;
        var lastFromCharCodeCalls = 0;
        String.fromCharCode = function() {
            lastFromCharCodeCalls++;
            if (lastFromCharCodeCalls % 100 === 0 && lastFromCharCodeCalls > 0) {
                window.__encryptCalls.push({
                    type: 'fromCharCode',
                    time: Date.now(),
                    callCount: lastFromCharCodeCalls,
                    stack: new Error().stack?.substring(0, 500)
                });
            }
            return origFromCharCode.apply(this, arguments);
        };
        
        // Hook WebCrypto API
        if (window.crypto && window.crypto.subtle) {
            var origEncrypt = window.crypto.subtle.encrypt;
            window.crypto.subtle.encrypt = function() {
                window.__encryptCalls.push({
                    type: 'crypto.subtle.encrypt',
                    time: Date.now(),
                    algorithm: arguments[0],
                    stack: new Error().stack?.substring(0, 500)
                });
                return origEncrypt.apply(this, arguments);
            };
        }
        
        return { success: true };
    })()`, false);
    
    console.log('✅ Hook已安装');
    
    // 触发发送
    console.log('\n=== 触发消息发送 ===\n');
    const sendResult = await evaluate(`(() => {
        // 清空之前的记录
        window.__encryptCalls = [];
        
        // 在输入框输入文字
        var input = document.querySelector('[contenteditable="true"]');
        if (!input) return { error: '未找到输入框' };
        
        input.focus();
        input.textContent = 'Test123';
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
    console.log('发送触发:', sendResult);
    
    await new Promise(r => setTimeout(r, 2000));
    
    // 获取拦截结果
    console.log('\n=== 加密调用链 ===\n');
    const encryptCalls = await evaluate(`(() => window.__encryptCalls || [])()`, false);
    
    console.log(`捕获 ${encryptCalls?.length || 0} 个加密调用:\n`);
    (encryptCalls || []).forEach((call, i) => {
        console.log(`--- ${i + 1}. ${call.type} ---`);
        console.log('时间:', new Date(call.time).toLocaleTimeString());
        if (call.bLength) console.log('b长度:', call.bLength);
        if (call.bPreview) console.log('b预览:', call.bPreview);
        if (call.inputLength) console.log('输入长度:', call.inputLength);
        if (call.stack) console.log('调用栈:\n', call.stack);
        console.log('');
    });
    
    // 尝试直接获取加密函数
    console.log('\n=== 搜索加密函数 ===\n');
    const cryptoSearch = await evaluate(`(() => {
        // 搜索可能的加密模块
        var results = [];
        
        // 检查Vue app的provides
        try {
            var app = document.querySelector('#app')?.__vue_app__;
            var provides = app?._context?.provides;
            if (provides) {
                for (var key in provides) {
                    if (key.toLowerCase().includes('crypt') || key.toLowerCase().includes('encode')) {
                        results.push({ source: 'provides', key: key, type: typeof provides[key] });
                    }
                }
            }
        } catch(e) {}
        
        // 检查Pinia state中可能的加密配置
        try {
            var pinia = window.__pinia || document.querySelector('#app')?.__vue_app__?.config?.globalProperties?.$pinia;
            pinia?._s?.forEach((store, name) => {
                if (store.$state) {
                    for (var key in store.$state) {
                        if (key.toLowerCase().includes('key') || key.toLowerCase().includes('crypt') ||
                            key.toLowerCase().includes('secret')) {
                            results.push({ 
                                source: 'pinia.' + name, 
                                key: key, 
                                value: typeof store.$state[key] === 'string' ? 
                                    store.$state[key].substring(0, 30) : typeof store.$state[key]
                            });
                        }
                    }
                }
            });
        } catch(e) {}
        
        return results;
    })()`, false);
    console.log('加密相关配置:', cryptoSearch);
    
    console.log('\n');
    ws.close();
}

main().catch(console.error);
