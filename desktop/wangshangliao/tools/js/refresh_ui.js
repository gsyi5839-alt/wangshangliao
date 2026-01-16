/**
 * 触发旺商聊UI刷新
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
    console.log('=== 触发旺商聊UI刷新 ===\n');
    
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
    
    // 方法1：触发Vue更新
    console.log('=== 方法1: 触发Vue更新 ===');
    const result1 = await evaluate(`
(function() {
    var result = { success: false };
    try {
        var app = document.querySelector('#app');
        if (app && app.__vue__) {
            // 强制更新
            app.__vue__.$forceUpdate();
            result.forceUpdate = true;
            
            // 触发数据变化
            if (app.__vue__.$store) {
                // 尝试重新加载当前会话的消息
                var store = app.__vue__.$store;
                
                // 检查是否有刷新方法
                var actions = Object.keys(store._actions || {});
                result.availableActions = actions.filter(a => 
                    a.includes('load') || a.includes('refresh') || a.includes('msg') || a.includes('fetch')
                );
            }
            result.success = true;
        }
    } catch(e) {
        result.error = e.message;
    }
    return JSON.stringify(result, null, 2);
})();`, false);
    console.log(result1);
    
    // 方法2：尝试调用 store action 刷新消息
    console.log('\n=== 方法2: 调用store刷新 ===');
    const result2 = await evaluate(`
(async function() {
    var result = { success: false };
    try {
        var app = document.querySelector('#app')?.__vue__;
        if (app && app.$store) {
            var store = app.$store;
            
            // 尝试触发消息加载
            if (store.dispatch) {
                // 常见的 action 名称
                var tryActions = [
                    'loadMoreMsgs',
                    'loadMsgs', 
                    'refreshMsgs',
                    'fetchMsgs',
                    'getLocalMsgs'
                ];
                
                for (var i = 0; i < tryActions.length; i++) {
                    try {
                        await store.dispatch(tryActions[i]);
                        result.dispatched = tryActions[i];
                        break;
                    } catch(e) {
                        // 继续尝试
                    }
                }
            }
            
            result.success = true;
        }
    } catch(e) {
        result.error = e.message;
    }
    return JSON.stringify(result, null, 2);
})();`);
    console.log(result2);
    
    // 方法3：触发滚动到底部（可能触发渲染）
    console.log('\n=== 方法3: 滚动消息列表 ===');
    const result3 = await evaluate(`
(function() {
    var result = { success: false };
    try {
        // 找到消息列表容器
        var containers = [
            document.querySelector('.chat-message-list'),
            document.querySelector('.message-list'),
            document.querySelector('[class*="msg-scroll"]'),
            document.querySelector('[class*="message-scroll"]')
        ].filter(Boolean);
        
        result.foundContainers = containers.length;
        
        if (containers.length > 0) {
            var container = containers[0];
            // 滚动到底部
            container.scrollTop = container.scrollHeight;
            result.scrolled = true;
            result.scrollHeight = container.scrollHeight;
        }
        
        // 尝试模拟一个新消息事件
        var event = new CustomEvent('scroll');
        document.dispatchEvent(event);
        
        result.success = true;
    } catch(e) {
        result.error = e.message;
    }
    return JSON.stringify(result, null, 2);
})();`, false);
    console.log(result3);
    
    // 方法4：检查消息是否在IndexedDB中
    console.log('\n=== 方法4: 检查IndexedDB ===');
    const result4 = await evaluate(`
(async function() {
    var result = { success: false };
    try {
        // 检查 NIM SDK 的 IndexedDB
        var dbs = await indexedDB.databases();
        result.databases = dbs.map(d => d.name).filter(n => n && n.includes('nim'));
        result.success = true;
    } catch(e) {
        result.error = e.message;
    }
    return JSON.stringify(result, null, 2);
})();`);
    console.log(result4);
    
    // 方法5：按F5刷新页面
    console.log('\n=== 方法5: 建议操作 ===');
    console.log('请手动尝试以下操作:');
    console.log('1. 在旺商聊中按 F5 刷新页面');
    console.log('2. 或者切换到其他群聊再切回来');
    console.log('3. 或者关闭旺商聊重新打开');
    
    ws.close();
}

main().catch(console.error);

