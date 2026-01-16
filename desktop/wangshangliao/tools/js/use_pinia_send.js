/**
 * 使用Pinia SDK store的正确方法发送消息
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
    console.log('🔍 使用Pinia SDK store发送消息\n');
    
    const wsUrl = await getWebSocketUrl();
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    console.log('✅ 已连接\n');
    
    // 1. 分析sendNimMsg方法
    console.log('=== 1. 分析 sendNimMsg 方法 ===\n');
    const sendNimMsgInfo = await evaluate(`(() => {
        try {
            var app = document.querySelector('#app')?.__vue_app__;
            var pinia = app?.config?.globalProperties?.$pinia;
            var sdkStore = pinia?._s?.get('sdk');
            
            if (!sdkStore) return { error: 'SDK store not found' };
            
            var fn = sdkStore.sendNimMsg;
            if (fn) {
                return {
                    found: true,
                    length: fn.length,
                    preview: fn.toString().substring(0, 500)
                };
            }
            return { error: 'sendNimMsg not found' };
        } catch(e) {
            return { error: e.message };
        }
    })()`, false);
    console.log('sendNimMsg:', sendNimMsgInfo);
    
    // 2. 分析sendNimAutoReplyMsg方法
    console.log('\n=== 2. 分析 sendNimAutoReplyMsg 方法 ===\n');
    const autoReplyInfo = await evaluate(`(() => {
        try {
            var app = document.querySelector('#app')?.__vue_app__;
            var pinia = app?.config?.globalProperties?.$pinia;
            var sdkStore = pinia?._s?.get('sdk');
            
            if (!sdkStore) return { error: 'SDK store not found' };
            
            var fn = sdkStore.sendNimAutoReplyMsg;
            if (fn) {
                return {
                    found: true,
                    length: fn.length,
                    preview: fn.toString().substring(0, 800)
                };
            }
            return { error: 'sendNimAutoReplyMsg not found' };
        } catch(e) {
            return { error: e.message };
        }
    })()`, false);
    console.log('sendNimAutoReplyMsg:', autoReplyInfo);
    
    // 3. 查看SDK store的完整结构
    console.log('\n=== 3. SDK store 完整结构 ===\n');
    const sdkStoreKeys = await evaluate(`(() => {
        try {
            var app = document.querySelector('#app')?.__vue_app__;
            var pinia = app?.config?.globalProperties?.$pinia;
            var sdkStore = pinia?._s?.get('sdk');
            
            if (!sdkStore) return { error: 'SDK store not found' };
            
            var result = { methods: [], properties: [] };
            for (var key in sdkStore) {
                if (key.startsWith('$') || key.startsWith('_')) continue;
                if (typeof sdkStore[key] === 'function') {
                    result.methods.push(key);
                } else {
                    result.properties.push(key);
                }
            }
            return result;
        } catch(e) {
            return { error: e.message };
        }
    })()`, false);
    console.log('方法列表:', sdkStoreKeys?.methods?.filter(m => m.includes('send') || m.includes('Msg')));
    
    // 4. 尝试用 sendNimMsg 发送
    console.log('\n=== 4. 尝试用 sendNimMsg 发送 ===\n');
    const nimMsgResult = await evaluate(`(async () => {
        try {
            var app = document.querySelector('#app')?.__vue_app__;
            var pinia = app?.config?.globalProperties?.$pinia;
            var sdkStore = pinia?._s?.get('sdk');
            
            if (!sdkStore || !sdkStore.sendNimMsg) {
                return { error: 'sendNimMsg not available' };
            }
            
            // 尝试调用
            var result = await sdkStore.sendNimMsg({
                scene: 'p2p',
                to: '${LOGO_ACCOUNT}',
                text: '【Pinia sendNimMsg测试】' + new Date().toLocaleTimeString()
            });
            
            return { 
                success: true, 
                result: result ? JSON.stringify(result).substring(0, 300) : 'no result'
            };
        } catch(e) {
            return { error: e.message, stack: e.stack?.substring(0, 300) };
        }
    })()`);
    console.log('sendNimMsg结果:', nimMsgResult);
    
    // 5. 查找当前聊天会话
    console.log('\n=== 5. 获取当前会话信息 ===\n');
    const currentSession = await evaluate(`(() => {
        try {
            var app = document.querySelector('#app')?.__vue_app__;
            var pinia = app?.config?.globalProperties?.$pinia;
            
            // 查找chat store
            var chatStore = pinia?._s?.get('chat');
            var appStore = pinia?._s?.get('app');
            
            return {
                chatStore: chatStore ? {
                    hasCurrentSession: !!chatStore.currentSession,
                    sessionId: chatStore.currentSession?.id || chatStore.currentSessionId,
                    methods: Object.keys(chatStore).filter(k => typeof chatStore[k] === 'function' && k.includes('send'))
                } : null,
                appStore: appStore ? {
                    currentAccount: appStore.currentAccount || appStore.account
                } : null
            };
        } catch(e) {
            return { error: e.message };
        }
    })()`, false);
    console.log('会话信息:', currentSession);
    
    // 6. 查找聊天组件的发送方法
    console.log('\n=== 6. 查找聊天组件的发送方法 ===\n');
    const chatComponent = await evaluate(`(() => {
        // 查找聊天输入框组件
        var inputEl = document.querySelector('[class*="chat-input"], [class*="message-input"], textarea[class*="input"]');
        if (!inputEl) return { error: 'Chat input not found' };
        
        // 向上查找Vue组件
        var el = inputEl;
        var comp = null;
        while (el && !comp) {
            comp = el.__vue__ || el._vnode?.component?.proxy;
            el = el.parentElement;
        }
        
        if (!comp) return { error: 'Vue component not found' };
        
        var methods = [];
        for (var key in comp) {
            if (typeof comp[key] === 'function') {
                if (key.includes('send') || key.includes('submit') || key.includes('msg') || key.includes('input')) {
                    methods.push({
                        name: key,
                        preview: comp[key].toString().substring(0, 200)
                    });
                }
            }
        }
        
        return {
            componentFound: true,
            methods: methods
        };
    })()`, false);
    console.log('聊天组件方法:', chatComponent);
    
    console.log('\n');
    ws.close();
}

main().catch(console.error);
