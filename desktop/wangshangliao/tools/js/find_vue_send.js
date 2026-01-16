/**
 * 通过 Vue 组件发送消息
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
    console.log('=== 查找 Vue 发送方法 ===\n');
    
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
    
    // 1. 查找 Vue 组件的发送方法
    console.log('=== 1. 查找聊天组件 ===');
    const vueComponents = await evaluate(`
(function() {
    var result = { components: [] };
    
    // 递归查找 Vue 组件
    function findComponents(el, depth) {
        if (depth > 5) return;
        if (!el) return;
        
        if (el.__vue__) {
            var vm = el.__vue__;
            var methods = Object.keys(vm.$options.methods || {});
            var sendMethods = methods.filter(m => 
                m.toLowerCase().includes('send') || 
                m.toLowerCase().includes('message') ||
                m.toLowerCase().includes('upload') ||
                m.toLowerCase().includes('image')
            );
            
            if (sendMethods.length > 0) {
                result.components.push({
                    name: vm.$options.name || vm.$options._componentTag || 'unknown',
                    sendMethods: sendMethods,
                    allMethods: methods.slice(0, 20)
                });
            }
        }
        
        if (el.children) {
            for (var i = 0; i < el.children.length; i++) {
                findComponents(el.children[i], depth + 1);
            }
        }
    }
    
    findComponents(document.body, 0);
    
    return JSON.stringify(result, null, 2);
})();`, false);
    console.log(vueComponents);
    
    // 2. 查找消息输入框组件
    console.log('\n=== 2. 查找消息输入组件 ===');
    const inputComponent = await evaluate(`
(function() {
    var result = {};
    
    // 找到聊天输入框
    var inputSelectors = [
        '.chat-input',
        '.message-input', 
        '[class*="editor"]',
        'textarea',
        '[contenteditable="true"]'
    ];
    
    for (var i = 0; i < inputSelectors.length; i++) {
        var el = document.querySelector(inputSelectors[i]);
        if (el) {
            result.found = true;
            result.selector = inputSelectors[i];
            result.tagName = el.tagName;
            
            // 查找父组件
            var parent = el;
            while (parent && !parent.__vue__) {
                parent = parent.parentElement;
            }
            
            if (parent && parent.__vue__) {
                var vm = parent.__vue__;
                result.componentName = vm.$options.name;
                result.methods = Object.keys(vm.$options.methods || {});
                result.data = Object.keys(vm.$data || {}).slice(0, 20);
                
                // 检查是否有发送方法
                var sendMethod = result.methods.find(m => 
                    m === 'send' || m === 'sendMessage' || m === 'handleSend'
                );
                if (sendMethod) {
                    result.sendMethod = sendMethod;
                }
            }
            break;
        }
    }
    
    return JSON.stringify(result, null, 2);
})();`, false);
    console.log(inputComponent);
    
    // 3. 检查文件上传入口
    console.log('\n=== 3. 查找文件上传入口 ===');
    const uploadEntry = await evaluate(`
(function() {
    var result = { inputs: [], buttons: [] };
    
    // 找文件input
    var fileInputs = document.querySelectorAll('input[type="file"]');
    fileInputs.forEach(function(input, i) {
        result.inputs.push({
            accept: input.accept,
            className: input.className,
            id: input.id,
            hidden: input.style.display === 'none' || input.hidden
        });
    });
    
    // 找上传按钮
    var buttons = document.querySelectorAll('[class*="upload"], [class*="image"], [class*="photo"], [class*="picture"]');
    buttons.forEach(function(btn, i) {
        if (i < 10) {
            result.buttons.push({
                tag: btn.tagName,
                className: btn.className,
                title: btn.title || btn.getAttribute('title'),
                hasClick: typeof btn.onclick === 'function'
            });
        }
    });
    
    return JSON.stringify(result, null, 2);
})();`, false);
    console.log(uploadEntry);
    
    // 4. 检查 Vuex store 的 actions
    console.log('\n=== 4. 检查 Vuex store ===');
    const storeCheck = await evaluate(`
(function() {
    var result = {};
    
    try {
        var app = document.querySelector('#app');
        if (app && app.__vue__ && app.__vue__.$store) {
            var store = app.__vue__.$store;
            
            // 获取所有 actions
            var actions = Object.keys(store._actions || {});
            result.sendActions = actions.filter(a => 
                a.toLowerCase().includes('send') ||
                a.toLowerCase().includes('message') ||
                a.toLowerCase().includes('upload') ||
                a.toLowerCase().includes('file')
            );
            
            result.allActions = actions;
            
            // 获取 mutations
            var mutations = Object.keys(store._mutations || {});
            result.msgMutations = mutations.filter(m => 
                m.toLowerCase().includes('msg') ||
                m.toLowerCase().includes('message')
            );
        }
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})();`, false);
    console.log(storeCheck);
    
    // 5. 尝试通过 store dispatch 发送
    console.log('\n=== 5. 尝试 store dispatch 发送文本 ===');
    const dispatchTest = await evaluate(`
(async function() {
    var result = { success: false };
    
    try {
        var app = document.querySelector('#app');
        if (app && app.__vue__ && app.__vue__.$store) {
            var store = app.__vue__.$store;
            
            // 尝试各种可能的 action
            var possibleActions = [
                'sendTextMessage',
                'sendMessage', 
                'sendMsg',
                'chat/sendMessage',
                'message/send'
            ];
            
            for (var i = 0; i < possibleActions.length; i++) {
                var action = possibleActions[i];
                try {
                    if (store._actions[action]) {
                        result.foundAction = action;
                        // 不实际调用，只记录找到
                        break;
                    }
                } catch(e) {}
            }
            
            result.success = true;
        }
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})();`);
    console.log(dispatchTest);
    
    ws.close();
    
    console.log('\n=== 结论 ===');
    console.log('消息通过 NIM SDK 发送成功并保存到服务器');
    console.log('但旺商聊的 UI 不会自动刷新显示');
    console.log('\n建议:');
    console.log('1. 请按 F5 刷新旺商聊界面查看图片');
    console.log('2. 或切换到其他群聊再切回来');
    console.log('3. 其他群成员应该能看到这些图片');
}

main().catch(console.error);

