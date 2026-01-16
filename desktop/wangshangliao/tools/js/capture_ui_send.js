/**
 * 抓取UI发送消息时的完整调用参数
 * 通过Hook所有发送相关方法来捕获真实调用
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
    console.log('🔍 抓取UI发送消息的完整调用\n');
    
    const wsUrl = await getWebSocketUrl();
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    console.log('✅ 已连接\n');
    
    // 安装全面的Hook
    console.log('=== 安装全面Hook ===\n');
    await evaluate(`(() => {
        window.__capturedCalls = [];
        
        // Hook sendText
        var origSendText = window.nim.sendText.bind(window.nim);
        window.nim.sendText = function(opts) {
            window.__capturedCalls.push({
                method: 'sendText',
                time: Date.now(),
                options: JSON.parse(JSON.stringify(opts))
            });
            console.log('[CAPTURE] sendText:', JSON.stringify(opts));
            return origSendText(opts);
        };
        
        // Hook sendMsg
        var origSendMsg = window.nim.sendMsg.bind(window.nim);
        window.nim.sendMsg = function(opts) {
            window.__capturedCalls.push({
                method: 'sendMsg',
                time: Date.now(),
                options: JSON.parse(JSON.stringify(opts))
            });
            console.log('[CAPTURE] sendMsg:', JSON.stringify(opts));
            return origSendMsg(opts);
        };
        
        // Hook sendCustomMsg
        var origSendCustomMsg = window.nim.sendCustomMsg.bind(window.nim);
        window.nim.sendCustomMsg = function(opts) {
            window.__capturedCalls.push({
                method: 'sendCustomMsg',
                time: Date.now(),
                options: JSON.parse(JSON.stringify(opts))
            });
            console.log('[CAPTURE] sendCustomMsg:', JSON.stringify(opts));
            return origSendCustomMsg(opts);
        };
        
        // Hook _sendMsgByType
        if (window.nim._sendMsgByType) {
            var origSendMsgByType = window.nim._sendMsgByType.bind(window.nim);
            window.nim._sendMsgByType = function(opts) {
                window.__capturedCalls.push({
                    method: '_sendMsgByType',
                    time: Date.now(),
                    options: JSON.parse(JSON.stringify(opts))
                });
                console.log('[CAPTURE] _sendMsgByType:', JSON.stringify(opts));
                return origSendMsgByType(opts);
            };
        }
        
        // Hook beforeSendMsg
        if (window.nim.beforeSendMsg) {
            var origBeforeSendMsg = window.nim.beforeSendMsg.bind(window.nim);
            window.nim.beforeSendMsg = function(opts) {
                window.__capturedCalls.push({
                    method: 'beforeSendMsg',
                    time: Date.now(),
                    options: JSON.parse(JSON.stringify(opts))
                });
                console.log('[CAPTURE] beforeSendMsg:', JSON.stringify(opts));
                return origBeforeSendMsg(opts);
            };
        }
        
        // Hook afterSendMsg
        if (window.nim.afterSendMsg) {
            var origAfterSendMsg = window.nim.afterSendMsg.bind(window.nim);
            window.nim.afterSendMsg = function(opts) {
                window.__capturedCalls.push({
                    method: 'afterSendMsg',
                    time: Date.now(),
                    options: JSON.parse(JSON.stringify(opts))
                });
                console.log('[CAPTURE] afterSendMsg:', JSON.stringify(opts));
                return origAfterSendMsg(opts);
            };
        }
        
        return { success: true, hookedMethods: ['sendText', 'sendMsg', 'sendCustomMsg', '_sendMsgByType', 'beforeSendMsg', 'afterSendMsg'] };
    })()`, false);
    
    console.log('✅ Hook已安装');
    console.log('\n========================================');
    console.log('请在旺商聊UI中手动发送一条消息');
    console.log('（在聊天窗口输入文字并点击发送）');
    console.log('========================================\n');
    
    // 监控捕获的调用
    let lastCallCount = 0;
    for (let i = 0; i < 120; i++) {
        await new Promise(r => setTimeout(r, 1000));
        
        const captures = await evaluate(`(() => {
            return window.__capturedCalls || [];
        })()`, false);
        
        if (captures?.length > lastCallCount) {
            console.log(`\n🎯 捕获到新调用! (${captures.length - lastCallCount}个)\n`);
            
            // 显示新调用
            captures.slice(lastCallCount).forEach((call, i) => {
                console.log(`--- 调用 ${lastCallCount + i + 1}: ${call.method} ---`);
                console.log('时间:', new Date(call.time).toLocaleTimeString());
                console.log('参数:');
                console.log(JSON.stringify(call.options, null, 2));
                console.log('');
            });
            
            lastCallCount = captures.length;
        }
        
        process.stdout.write(`\r等待UI发送... ${120 - i}秒 (捕获: ${captures?.length || 0})`);
    }
    
    // 显示所有捕获
    console.log('\n\n=== 所有捕获的调用 ===\n');
    const allCaptures = await evaluate(`(() => {
        return window.__capturedCalls || [];
    })()`, false);
    
    if (allCaptures?.length > 0) {
        allCaptures.forEach((call, i) => {
            console.log(`${i + 1}. ${call.method}:`);
            console.log(JSON.stringify(call.options, null, 2));
            console.log('');
        });
    } else {
        console.log('未捕获到任何调用');
    }
    
    ws.close();
}

main().catch(console.error);
