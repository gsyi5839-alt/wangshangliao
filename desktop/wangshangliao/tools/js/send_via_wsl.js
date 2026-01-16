/**
 * 通过旺商聊内部机制发送消息
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
    console.log('=== 通过旺商聊内部机制发送 ===\n');
    
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
            }, 60000);
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
    
    // 首先，找到旺商聊的消息发送入口点
    console.log('=== 1. 查找旺商聊发送入口 ===');
    
    const findEntry = await evaluate(`
(function() {
    var result = { found: false };
    
    // 检查 require 模块
    try {
        if (window.require) {
            var modules = window.require.cache;
            if (modules) {
                var keys = Object.keys(modules).filter(k => 
                    k.includes('message') || k.includes('chat') || k.includes('send')
                );
                result.modules = keys.slice(0, 10);
            }
        }
    } catch(e) {}
    
    // 检查 window.__modules__
    if (window.__modules__) {
        result.__modules__ = Object.keys(window.__modules__).slice(0, 20);
    }
    
    // 检查 window.wslApi
    if (window.wslApi) {
        result.wslApi = Object.keys(window.wslApi);
    }
    
    // 检查 window.electronApi
    if (window.electronApi) {
        result.electronApi = Object.keys(window.electronApi).slice(0, 20);
    }
    
    return JSON.stringify(result, null, 2);
})();`, false);
    console.log(findEntry);
    
    // 2. 检查 window.nim 的发送后回调链
    console.log('\n=== 2. 添加发送后UI更新 ===');
    
    const addCallback = await evaluate(`
(async function() {
    var result = { success: false };
    
    try {
        // 保存原始的 sendFile 方法
        var originalSendFile = window.nim.sendFile.bind(window.nim);
        
        // 创建一个包装方法，在发送后更新UI
        window.nim.sendFileWithUI = function(options) {
            var originalDone = options.done;
            
            options.done = function(err, msg) {
                // 调用原始回调
                if (originalDone) {
                    originalDone(err, msg);
                }
                
                // 如果发送成功，触发 UI 更新
                if (!err && msg) {
                    // 触发 onmsg 来更新UI
                    if (window.nim.options && typeof window.nim.options.onmsg === 'function') {
                        setTimeout(function() {
                            window.nim.options.onmsg(msg);
                        }, 100);
                    }
                }
            };
            
            return originalSendFile(options);
        };
        
        result.success = true;
        result.message = 'sendFileWithUI created';
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})();`);
    console.log(addCallback);
    
    // 3. 测试发送（使用新方法）
    console.log('\n=== 3. 测试发送图片（带UI更新） ===');
    
    // 简单的红色100x100 PNG
    const imageBase64 = 'iVBORw0KGgoAAAANSUhEUgAAAGQAAABkCAIAAAD/gAIDAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAABfSURBVHhe7cExAQAAAMKg9U9tCj+gAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAHqqnwwABQqzZAAAAAABJRU5ErkJggg==';
    
    const sendTest = await evaluate(`
(async function() {
    var result = { success: false, stages: [] };
    
    try {
        var scene = 'team';
        var to = '40821608989';
        
        // 创建图片文件
        var base64Data = '${imageBase64}';
        var byteString = atob(base64Data);
        var ab = new ArrayBuffer(byteString.length);
        var ia = new Uint8Array(ab);
        for (var i = 0; i < byteString.length; i++) {
            ia[i] = byteString.charCodeAt(i);
        }
        var blob = new Blob([ab], { type: 'image/png' });
        var file = new File([blob], 'robot_test_' + Date.now() + '.png', { type: 'image/png' });
        
        result.stages.push('file created: ' + file.size + ' bytes');
        
        // 使用带UI更新的方法发送
        var sendResult = await new Promise(function(resolve) {
            var method = window.nim.sendFileWithUI || window.nim.sendFile;
            
            method({
                scene: scene,
                to: to,
                type: 'image',
                blob: file,
                done: function(err, msg) {
                    if (err) {
                        resolve({ success: false, error: err.message || String(err) });
                    } else {
                        resolve({ 
                            success: true, 
                            msgId: msg?.idClient,
                            status: msg?.status
                        });
                    }
                }
            });
            
            setTimeout(function() { resolve({ success: false, error: 'Timeout' }); }, 60000);
        });
        
        result = { ...result, ...sendResult };
        result.stages.push(sendResult.success ? 'sent: ' + sendResult.msgId : 'failed');
        
        // 手动触发 onmsg
        if (sendResult.success && sendResult.msgId) {
            result.stages.push('triggering onmsg...');
            
            // 获取发送的消息
            var msgs = await new Promise(function(resolve) {
                window.nim.getLocalMsgs({
                    scene: scene,
                    to: to,
                    limit: 5,
                    done: function(err, obj) {
                        resolve(obj?.msgs || []);
                    }
                });
                setTimeout(function() { resolve([]); }, 5000);
            });
            
            var sentMsg = msgs.find(m => m.idClient === sendResult.msgId);
            if (sentMsg && window.nim.options && window.nim.options.onmsg) {
                window.nim.options.onmsg(sentMsg);
                result.stages.push('onmsg triggered');
            }
        }
        
    } catch(e) {
        result.error = e.message;
        result.stack = e.stack;
    }
    
    return JSON.stringify(result, null, 2);
})();`);
    console.log(sendTest);
    
    // 4. 等待并检查UI
    console.log('\n等待2秒...');
    await new Promise(r => setTimeout(r, 2000));
    
    console.log('\n=== 4. 检查UI是否更新 ===');
    const uiCheck = await evaluate(`
(function() {
    var result = { images: [] };
    
    // 查找NIM云端图片
    var imgs = document.querySelectorAll('img');
    imgs.forEach(function(img) {
        if (img.src && img.src.includes('nim-nosdn.netease.im')) {
            result.images.push({
                src: img.src.substring(0, 80),
                visible: img.offsetParent !== null,
                size: img.width + 'x' + img.height
            });
        }
    });
    
    return JSON.stringify(result, null, 2);
})();`, false);
    console.log(uiCheck);
    
    ws.close();
    console.log('\n=== 完成 ===');
    console.log('请检查旺商聊群聊界面是否显示新图片');
}

main().catch(console.error);

