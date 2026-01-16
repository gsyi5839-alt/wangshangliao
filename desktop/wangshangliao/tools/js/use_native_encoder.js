/**
 * 尝试使用旺商聊的原生编码器发送消息
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
    console.log('🔍 尝试使用旺商聊原生编码器\n');
    
    const wsUrl = await getWebSocketUrl();
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    console.log('✅ 已连接\n');
    
    // 1. 检查IPC通道是否可用
    console.log('=== 1. 检查IPC通道 ===\n');
    const ipcCheck = await evaluate(`(() => {
        return {
            hasElectron: typeof window.electron !== 'undefined',
            hasIpcRenderer: typeof window.electron?.ipcRenderer !== 'undefined',
            hasXclient: typeof window.xclient !== 'undefined'
        };
    })()`, false);
    console.log('IPC检查:', ipcCheck);
    
    // 2. 查找现有的编码调用方式
    console.log('\n=== 2. 搜索编码函数 ===\n');
    const encoderSearch = await evaluate(`(() => {
        var results = [];
        
        // 搜索window中的编码相关
        for (var key in window) {
            try {
                if (key.toLowerCase().includes('xclient') || 
                    key.toLowerCase().includes('encode') ||
                    key.toLowerCase().includes('api')) {
                    results.push({ name: key, type: typeof window[key] });
                }
            } catch(e) {}
        }
        
        return results;
    })()`, false);
    console.log('编码函数:', encoderSearch);
    
    // 3. 尝试直接发送IPC消息进行编码
    console.log('\n=== 3. 尝试IPC编码 ===\n');
    const ipcEncode = await evaluate(`(async () => {
        try {
            // 检查是否有electron对象
            if (!window.electron || !window.electron.ipcRenderer) {
                return { error: 'No electron IPC available' };
            }
            
            // 构建消息
            var msgData = {
                msgFormat: 1,
                text: {
                    data: '【编码测试】' + new Date().toLocaleTimeString()
                }
            };
            
            // 尝试发送encode请求
            return new Promise((resolve, reject) => {
                var key = 'xclient_encode_' + Date.now();
                
                window.electron.ipcRenderer.once(key, (event, data) => {
                    resolve({ success: true, data: data });
                });
                
                window.electron.ipcRenderer.send('xclient', {
                    key: key,
                    type: 'encode',
                    params: JSON.stringify(msgData)
                });
                
                setTimeout(() => resolve({ timeout: true }), 5000);
            });
        } catch(e) {
            return { error: e.message };
        }
    })()`);
    console.log('IPC编码结果:', ipcEncode);
    
    // 4. 检查Vue组件中的发送方法
    console.log('\n=== 4. 分析Vue发送组件 ===\n');
    const vueAnalysis = await evaluate(`(() => {
        try {
            var app = document.querySelector('#app')?.__vue_app__;
            var pinia = app?.config?.globalProperties?.$pinia;
            var sdkStore = pinia?._s?.get('sdk');
            
            // 获取sdkStore的所有action
            var actions = [];
            if (sdkStore?.$options?.actions) {
                for (var key in sdkStore.$options.actions) {
                    actions.push(key);
                }
            }
            
            return {
                hasSDKStore: !!sdkStore,
                actions: actions.filter(a => a.includes('send') || a.includes('Msg'))
            };
        } catch(e) {
            return { error: e.message };
        }
    })()`, false);
    console.log('Vue分析:', vueAnalysis);
    
    // 5. 直接调用sendNimMsg
    console.log('\n=== 5. 调用sendNimMsg ===\n');
    const sendResult = await evaluate(`(async () => {
        try {
            var app = document.querySelector('#app')?.__vue_app__;
            var pinia = app?.config?.globalProperties?.$pinia;
            var sdkStore = pinia?._s?.get('sdk');
            
            if (!sdkStore || !sdkStore.sendNimMsg) {
                return { error: 'sendNimMsg not available' };
            }
            
            // 调用sendNimMsg
            var result = await sdkStore.sendNimMsg({
                scene: 'p2p',
                to: '${LOGO_ACCOUNT}',
                text: '【Pinia测试】' + new Date().toLocaleTimeString()
            });
            
            return { success: true, result: result };
        } catch(e) {
            return { error: e.message, stack: e.stack?.substring(0, 300) };
        }
    })()`);
    console.log('sendNimMsg结果:', sendResult);
    
    // 6. 等待并检查消息历史
    console.log('\n=== 6. 检查最新消息 ===\n');
    await new Promise(r => setTimeout(r, 2000));
    
    const history = await evaluate(`(async () => {
        return new Promise(r => {
            window.nim.getHistoryMsgs({
                scene: 'p2p',
                to: '${LOGO_ACCOUNT}',
                limit: 3,
                done: (err, obj) => {
                    r((obj?.msgs || []).map(m => ({
                        flow: m.flow,
                        type: m.type,
                        text: m.text?.substring(0, 40),
                        time: new Date(m.time).toLocaleTimeString(),
                        status: m.status
                    })));
                }
            });
            setTimeout(() => r([]), 5000);
        });
    })()`);
    console.log('最新消息:');
    (history || []).forEach((m, i) => {
        console.log(`  ${i + 1}. [${m.flow}] ${m.type}: ${m.text || '(无)'} (${m.status}) @ ${m.time}`);
    });
    
    console.log('\n');
    ws.close();
}

main().catch(console.error);
