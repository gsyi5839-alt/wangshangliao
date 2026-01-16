/**
 * 找到旺商聊自己发送消息的方法
 */
const WebSocket = require('ws');
const http = require('http');

function httpGet(url) {
    return new Promise((resolve, reject) => {
        http.get(url, (res) => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', () => {
                try { resolve(JSON.parse(data)); }
                catch(e) { reject(new Error('Invalid JSON')); }
            });
        }).on('error', reject);
    });
}

async function main() {
    console.log('=== 找到旺商聊发送消息方法 ===\n');
    
    const pages = await httpGet('http://localhost:9222/json');
    const page = pages.find(p => p.url && p.url.includes('wangshangliao'));
    
    if (!page) {
        console.log('WangShangLiao not found');
        return;
    }
    
    const ws = new WebSocket(page.webSocketDebuggerUrl);
    let messageId = 1;
    const pending = new Map();
    
    ws.on('message', (data) => {
        const msg = JSON.parse(data);
        if (msg.id && pending.has(msg.id)) {
            pending.get(msg.id)(msg);
            pending.delete(msg.id);
        }
    });
    
    function sendCommand(method, params = {}) {
        return new Promise((resolve, reject) => {
            const id = messageId++;
            pending.set(id, resolve);
            ws.send(JSON.stringify({ id, method, params }));
            setTimeout(() => {
                if (pending.has(id)) {
                    pending.delete(id);
                    reject(new Error('Timeout'));
                }
            }, 30000);
        });
    }
    
    async function evaluate(expression, awaitPromise = true) {
        const result = await sendCommand('Runtime.evaluate', {
            expression,
            returnByValue: true,
            awaitPromise
        });
        return result.result?.result?.value;
    }
    
    await new Promise(resolve => ws.on('open', resolve));
    console.log('Connected!\n');
    
    // 1. 检查全局变量中的发送方法
    console.log('=== 1. 检查全局发送方法 ===');
    const globals = await evaluate(`
(function() {
    var result = { globals: [] };
    
    // 检查 window 上的相关方法
    for (var key in window) {
        try {
            if (typeof window[key] === 'function') {
                var str = key.toLowerCase();
                if (str.includes('send') || str.includes('message') || str.includes('chat')) {
                    result.globals.push(key);
                }
            } else if (typeof window[key] === 'object' && window[key] !== null) {
                // 检查对象中的发送方法
                if (key === 'chatService' || key === 'msgService' || key === 'messageService') {
                    result[key] = Object.keys(window[key]).slice(0, 20);
                }
            }
        } catch(e) {}
    }
    
    return JSON.stringify(result, null, 2);
})();`, false);
    console.log(globals);
    
    // 2. 检查 window.api 或 window.wsl
    console.log('\n=== 2. 检查API对象 ===');
    const apiCheck = await evaluate(`
(function() {
    var result = {};
    
    // 检查常见的API命名空间
    ['api', 'wsl', 'SDK', 'chatApi', 'msgApi', 'nimApi'].forEach(function(name) {
        if (window[name] && typeof window[name] === 'object') {
            result[name] = Object.keys(window[name]).filter(k => !k.startsWith('_')).slice(0, 30);
        }
    });
    
    return JSON.stringify(result, null, 2);
})();`, false);
    console.log(apiCheck);
    
    // 3. 检查 ipcRenderer (Electron)
    console.log('\n=== 3. 检查Electron IPC ===');
    const ipcCheck = await evaluate(`
(function() {
    var result = { hasIpc: false };
    
    try {
        if (window.require) {
            var electron = window.require('electron');
            if (electron && electron.ipcRenderer) {
                result.hasIpc = true;
                // 尝试获取已注册的频道
                result.channels = [];
            }
        }
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})();`, false);
    console.log(ipcCheck);
    
    // 4. 寻找发送图片的按钮元素及其事件
    console.log('\n=== 4. 检查发送图片按钮 ===');
    const btnCheck = await evaluate(`
(function() {
    var result = { buttons: [] };
    
    // 找发送图片的按钮
    var btns = document.querySelectorAll('[class*="image"], [class*="photo"], [class*="picture"], [title*="图片"]');
    btns.forEach(function(btn, i) {
        if (i < 10) {
            result.buttons.push({
                tag: btn.tagName,
                className: btn.className,
                title: btn.title,
                id: btn.id
            });
        }
    });
    
    // 找发送按钮
    var sendBtns = document.querySelectorAll('[class*="send"], button[class*="btn"]');
    sendBtns.forEach(function(btn, i) {
        if (i < 5 && btn.textContent.includes('发送')) {
            result.buttons.push({
                tag: btn.tagName,
                className: btn.className,
                text: btn.textContent.substring(0, 20)
            });
        }
    });
    
    return JSON.stringify(result, null, 2);
})();`, false);
    console.log(btnCheck);
    
    // 5. 检查 nim.sendFile 的回调是否更新了UI
    console.log('\n=== 5. 检查 nim.sendFile 回调 ===');
    const callbackCheck = await evaluate(`
(function() {
    var result = {};
    
    try {
        if (window.nim && window.nim.options) {
            var opts = window.nim.options;
            result.callbacks = {
                onmsg: typeof opts.onmsg === 'function',
                onsendmsg: typeof opts.onsendmsg === 'function', 
                onroamingmsgs: typeof opts.onroamingmsgs === 'function',
                onofflinemsgs: typeof opts.onofflinemsgs === 'function'
            };
            
            // 尝试获取原始的 onmsg 处理器
            if (opts.onmsg) {
                result.onmsgStr = opts.onmsg.toString().substring(0, 200);
            }
        }
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})();`, false);
    console.log(callbackCheck);
    
    // 6. 尝试手动触发 onmsg 来添加消息到UI
    console.log('\n=== 6. 尝试手动触发消息回调 ===');
    const triggerResult = await evaluate(`
(async function() {
    var result = { success: false };
    
    try {
        // 获取最近发送的图片消息
        var msgs = await new Promise(function(resolve) {
            window.nim.getLocalMsgs({
                scene: 'team',
                to: '40821608989',
                limit: 5,
                done: function(err, obj) {
                    if (err) resolve([]);
                    else resolve(obj.msgs || []);
                }
            });
            setTimeout(function() { resolve([]); }, 5000);
        });
        
        var imgMsg = msgs.find(m => m.type === 'image' && m.flow === 'out');
        if (imgMsg && window.nim && window.nim.options && typeof window.nim.options.onmsg === 'function') {
            // 尝试手动调用 onmsg
            window.nim.options.onmsg(imgMsg);
            result.triggered = true;
            result.msgId = imgMsg.idClient;
        } else {
            result.reason = imgMsg ? 'no onmsg callback' : 'no image message';
        }
        
        result.success = true;
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})();`);
    console.log(triggerResult);
    
    ws.close();
}

main().catch(console.error);

