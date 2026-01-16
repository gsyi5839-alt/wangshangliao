/**
 * 对比成功显示的消息和我们发送的消息格式
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
    console.log('=== 对比消息格式 ===\n');
    
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
    
    // 获取所有消息，找出图片消息的完整格式
    const result = await evaluate(`
(async function() {
    var result = {
        myImages: [],      // 我发送的图片
        otherImages: [],   // 别人发送的图片（正常显示）
        myText: [],        // 我发送的文本
        otherText: []      // 别人发送的文本（正常显示）
    };
    
    try {
        var msgs = await new Promise(function(resolve) {
            window.nim.getLocalMsgs({
                scene: 'team',
                to: '40821608989',
                limit: 100,
                done: function(err, obj) {
                    if (err) resolve([]);
                    else resolve(obj.msgs || []);
                }
            });
            setTimeout(function() { resolve([]); }, 10000);
        });
        
        // 获取当前用户账号
        var state = JSON.parse(localStorage.getItem('managestate') || '{}');
        var myAccount = state.userInfo?.account || '1948408648';
        
        msgs.forEach(function(m) {
            var isMyMsg = m.from === myAccount;
            var info = {
                idClient: m.idClient,
                idServer: m.idServer,
                type: m.type,
                from: m.from,
                fromNick: m.fromNick,
                time: new Date(m.time).toLocaleTimeString(),
                status: m.status,
                flow: m.flow,
                // 关键字段
                scene: m.scene,
                to: m.to,
                target: m.target,
                sessionId: m.sessionId
            };
            
            if (m.type === 'image') {
                info.file = m.file ? {
                    url: (m.file.url || '').substring(0, 80),
                    name: m.file.name,
                    size: m.file.size,
                    w: m.file.w,
                    h: m.file.h,
                    ext: m.file.ext,
                    md5: m.file.md5
                } : null;
                
                if (isMyMsg) {
                    result.myImages.push(info);
                } else {
                    result.otherImages.push(info);
                }
            } else if (m.type === 'text') {
                info.text = (m.text || '').substring(0, 30);
                if (isMyMsg) {
                    result.myText.push(info);
                } else {
                    result.otherText.push(info);
                }
            }
        });
        
        // 只保留最近几条
        result.myImages = result.myImages.slice(0, 3);
        result.otherImages = result.otherImages.slice(0, 3);
        result.myText = result.myText.slice(0, 2);
        result.otherText = result.otherText.slice(0, 2);
        
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})();`);

    console.log(result);
    
    // 检查是否有消息过滤逻辑
    console.log('\n=== 检查旺商聊的消息过滤/处理逻辑 ===');
    
    const filterCheck = await evaluate(`
(function() {
    var result = {
        hasMessageFilter: false,
        possibleFilters: []
    };
    
    // 检查 Vue store 中的消息处理
    try {
        var app = document.querySelector('#app');
        if (app && app.__vue__) {
            var store = app.__vue__.$store;
            if (store && store.state) {
                // 检查是否有消息过滤设置
                if (store.state.setting) {
                    result.settingKeys = Object.keys(store.state.setting).filter(k => 
                        k.includes('filter') || k.includes('hide') || k.includes('block')
                    );
                }
                
                // 检查当前会话的消息
                if (store.state.currentMsgs || store.state.msgs) {
                    var msgs = store.state.currentMsgs || store.state.msgs;
                    if (Array.isArray(msgs)) {
                        result.storeMsgCount = msgs.length;
                        result.storeImageCount = msgs.filter(m => m.type === 'image').length;
                    }
                }
            }
        }
    } catch(e) {
        result.vueError = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})();`, false);
    
    console.log(filterCheck);
    
    ws.close();
}

main().catch(console.error);

