/**
 * Hook beforeSendMsg 来分析旺商聊的消息处理流程
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
    console.log('🔍 深度Hook分析消息发送流程\n');
    
    const wsUrl = await getWebSocketUrl();
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    console.log('✅ 已连接\n');
    
    // 安装深度Hook
    console.log('=== 安装深度Hook ===\n');
    await evaluate(`(() => {
        window.__sendMsgLogs = [];
        
        // Hook beforeSendMsg
        if (window.nim.beforeSendMsg) {
            var origBeforeSend = window.nim.beforeSendMsg.bind(window.nim);
            window.nim.beforeSendMsg = function(msg) {
                console.log('[HOOK beforeSendMsg] 输入:', JSON.stringify(msg).substring(0, 500));
                window.__sendMsgLogs.push({
                    stage: 'beforeSendMsg-input',
                    time: Date.now(),
                    data: JSON.parse(JSON.stringify(msg))
                });
                
                var result = origBeforeSend(msg);
                
                console.log('[HOOK beforeSendMsg] 输出:', JSON.stringify(result).substring(0, 500));
                window.__sendMsgLogs.push({
                    stage: 'beforeSendMsg-output',
                    time: Date.now(),
                    data: JSON.parse(JSON.stringify(result))
                });
                
                return result;
            };
        }
        
        // Hook _sendMsgByType - 这是实际发送的内部方法
        if (window.nim._sendMsgByType) {
            var origSendByType = window.nim._sendMsgByType.bind(window.nim);
            window.nim._sendMsgByType = function(opts) {
                console.log('[HOOK _sendMsgByType]:', JSON.stringify(opts).substring(0, 500));
                window.__sendMsgLogs.push({
                    stage: '_sendMsgByType',
                    time: Date.now(),
                    data: JSON.parse(JSON.stringify(opts))
                });
                return origSendByType(opts);
            };
        }
        
        // Hook sendMsgValidate
        if (window.nim.sendMsgValidate) {
            var origValidate = window.nim.sendMsgValidate.bind(window.nim);
            window.nim.sendMsgValidate = function(opts) {
                console.log('[HOOK sendMsgValidate]:', JSON.stringify(opts).substring(0, 300));
                window.__sendMsgLogs.push({
                    stage: 'sendMsgValidate',
                    time: Date.now(),
                    data: JSON.parse(JSON.stringify(opts))
                });
                return origValidate(opts);
            };
        }
        
        // Hook sendCmd - 这是最底层的命令发送
        var origSendCmd = window.nim.sendCmd.bind(window.nim);
        window.nim.sendCmd = function(cmd, opts, cb) {
            if (cmd === 'sendMsg' || cmd === 'sendText') {
                console.log('[HOOK sendCmd] cmd:', cmd, 'opts:', JSON.stringify(opts).substring(0, 500));
                window.__sendMsgLogs.push({
                    stage: 'sendCmd-' + cmd,
                    time: Date.now(),
                    cmd: cmd,
                    data: JSON.parse(JSON.stringify(opts))
                });
            }
            return origSendCmd.apply(this, arguments);
        };
        
        return { success: true };
    })()`, false);
    
    console.log('✅ Hook已安装');
    console.log('\n请在旺商聊UI中发送一条消息...');
    console.log('或者我将在5秒后程序发送测试消息\n');
    
    await new Promise(r => setTimeout(r, 5000));
    
    // 用程序发送测试消息
    console.log('=== 程序发送测试消息 ===\n');
    const sendResult = await evaluate(`(async () => {
        return new Promise(r => {
            window.nim.sendText({
                scene: 'p2p',
                to: '${LOGO_ACCOUNT}',
                text: 'Hook测试消息 ' + Date.now(),
                done: (err, msg) => {
                    r({ error: err?.message, idServer: msg?.idServer, status: msg?.status });
                }
            });
            setTimeout(() => r({ error: 'Timeout' }), 10000);
        });
    })()`);
    console.log('发送结果:', sendResult);
    
    // 收集日志
    await new Promise(r => setTimeout(r, 1000));
    
    console.log('\n=== 发送流程日志 ===\n');
    const logs = await evaluate(`(() => {
        return window.__sendMsgLogs || [];
    })()`, false);
    
    (logs || []).forEach((log, i) => {
        console.log(`\n--- ${i + 1}. ${log.stage} ---`);
        console.log('时间:', new Date(log.time).toLocaleTimeString());
        if (log.cmd) console.log('命令:', log.cmd);
        console.log('数据:');
        console.log(JSON.stringify(log.data, null, 2).substring(0, 800));
    });
    
    // 查找消息编码的Pinia store方法
    console.log('\n\n=== 搜索Pinia store中的发送方法 ===\n');
    const storeSearch = await evaluate(`(() => {
        try {
            var pinia = window.__pinia;
            if (!pinia) {
                var app = document.querySelector('#app')?.__vue_app__;
                pinia = app?.config?.globalProperties?.$pinia;
            }
            
            if (!pinia) return { error: 'Pinia not found' };
            
            var results = {};
            pinia._s.forEach((store, name) => {
                var methods = [];
                for (var key in store) {
                    if (typeof store[key] === 'function' && 
                        (key.toLowerCase().includes('send') || 
                         key.toLowerCase().includes('msg') ||
                         key.toLowerCase().includes('encode') ||
                         key.toLowerCase().includes('encrypt'))) {
                        methods.push(key);
                    }
                }
                if (methods.length > 0) {
                    results[name] = methods;
                }
            });
            
            return results;
        } catch(e) {
            return { error: e.message };
        }
    })()`, false);
    console.log('Store方法:', storeSearch);
    
    console.log('\n');
    ws.close();
}

main().catch(console.error);
