/**
 * 模拟UI发送消息 - 找到并调用界面上的发送按钮
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
    console.log('🔍 模拟UI发送消息\n');
    
    const wsUrl = await getWebSocketUrl();
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    console.log('✅ 已连接\n');
    
    // 1. 查找当前页面的会话列表，切换到logo的会话
    console.log('=== 1. 查找并切换到logo会话 ===\n');
    const switchResult = await evaluate(`(() => {
        // 查找会话列表项
        var sessionItems = document.querySelectorAll('[class*="session-item"], [class*="chat-item"], [class*="conversation"]');
        var foundItem = null;
        
        sessionItems.forEach(item => {
            var text = item.textContent || '';
            if (text.includes('logo') || text.includes('${LOGO_ACCOUNT}')) {
                foundItem = item;
            }
        });
        
        if (foundItem) {
            foundItem.click();
            return { success: true, clicked: 'session item' };
        }
        
        return { 
            success: false, 
            itemCount: sessionItems.length,
            texts: Array.from(sessionItems).slice(0, 3).map(i => i.textContent?.substring(0, 30))
        };
    })()`, false);
    console.log('切换会话:', switchResult);
    
    await new Promise(r => setTimeout(r, 500));
    
    // 2. 查找输入框和发送按钮
    console.log('\n=== 2. 查找输入框和发送按钮 ===\n');
    const uiElements = await evaluate(`(() => {
        // 查找输入框
        var inputs = document.querySelectorAll('textarea, [contenteditable="true"], input[type="text"]');
        var inputInfo = Array.from(inputs).map(el => ({
            tag: el.tagName,
            className: el.className?.substring(0, 50),
            placeholder: el.placeholder,
            editable: el.contentEditable
        }));
        
        // 查找发送按钮
        var buttons = document.querySelectorAll('button, [class*="send"], [class*="btn"]');
        var buttonInfo = [];
        buttons.forEach(btn => {
            var text = btn.textContent?.trim() || '';
            if (text.includes('发送') || text.includes('send') || btn.className?.includes('send')) {
                buttonInfo.push({
                    tag: btn.tagName,
                    text: text.substring(0, 20),
                    className: btn.className?.substring(0, 50)
                });
            }
        });
        
        return { inputs: inputInfo, sendButtons: buttonInfo };
    })()`, false);
    console.log('UI元素:', uiElements);
    
    // 3. 尝试在输入框输入文字
    console.log('\n=== 3. 模拟输入文字 ===\n');
    const inputResult = await evaluate(`(() => {
        // 找到主要的输入框
        var input = document.querySelector('textarea[class*="input"], [contenteditable="true"], textarea');
        
        if (!input) return { error: '未找到输入框' };
        
        // 聚焦输入框
        input.focus();
        
        // 设置文字
        var testText = '【UI模拟发送】' + new Date().toLocaleTimeString();
        
        if (input.tagName === 'TEXTAREA' || input.tagName === 'INPUT') {
            input.value = testText;
            // 触发input事件
            input.dispatchEvent(new Event('input', { bubbles: true }));
        } else {
            // contenteditable
            input.textContent = testText;
            input.dispatchEvent(new Event('input', { bubbles: true }));
        }
        
        return { 
            success: true, 
            inputTag: input.tagName,
            text: testText
        };
    })()`, false);
    console.log('输入结果:', inputResult);
    
    await new Promise(r => setTimeout(r, 300));
    
    // 4. 点击发送按钮或模拟回车
    console.log('\n=== 4. 模拟发送 ===\n');
    const sendResult = await evaluate(`(() => {
        // 找到发送按钮
        var sendBtn = document.querySelector('button[class*="send"], [class*="send-btn"], button:has-text("发送")');
        
        if (!sendBtn) {
            // 尝试查找包含"发送"文字的按钮
            var buttons = document.querySelectorAll('button, div[class*="btn"]');
            buttons.forEach(btn => {
                if (btn.textContent?.includes('发送')) {
                    sendBtn = btn;
                }
            });
        }
        
        if (sendBtn) {
            sendBtn.click();
            return { success: true, method: 'button click' };
        }
        
        // 如果没找到按钮，尝试按回车键
        var input = document.querySelector('textarea, [contenteditable="true"]');
        if (input) {
            var enterEvent = new KeyboardEvent('keydown', {
                key: 'Enter',
                code: 'Enter',
                keyCode: 13,
                which: 13,
                bubbles: true
            });
            input.dispatchEvent(enterEvent);
            return { success: true, method: 'enter key' };
        }
        
        return { error: '未找到发送方式' };
    })()`, false);
    console.log('发送结果:', sendResult);
    
    // 5. 等待并检查历史消息
    console.log('\n=== 5. 等待检查消息状态 ===\n');
    await new Promise(r => setTimeout(r, 2000));
    
    const checkResult = await evaluate(`(async () => {
        return new Promise(r => {
            window.nim.getHistoryMsgs({
                scene: 'p2p',
                to: '${LOGO_ACCOUNT}',
                limit: 3,
                done: (err, obj) => {
                    if (err) r({ error: err.message });
                    else r((obj?.msgs || []).map(m => ({
                        flow: m.flow,
                        type: m.type,
                        status: m.status,
                        text: m.text?.substring(0, 40) || '(无text)',
                        content: m.content?.substring(0, 50),
                        time: new Date(m.time).toLocaleTimeString()
                    })));
                }
            });
            setTimeout(() => r([]), 5000);
        });
    })()`);
    console.log('最新消息:');
    (checkResult || []).forEach((m, i) => {
        console.log(`  ${i + 1}. [${m.flow}] ${m.type}: ${m.text || m.content} (${m.status}) @ ${m.time}`);
    });
    
    // 6. 分析发送消息的Vue组件调用链
    console.log('\n=== 6. 分析Vue发送组件 ===\n');
    const vueAnalysis = await evaluate(`(() => {
        // 查找聊天相关的Vue组件
        var chatEl = document.querySelector('[class*="chat-panel"], [class*="message-panel"], [class*="chat-content"]');
        if (!chatEl) return { error: '未找到聊天面板' };
        
        // 查找Vue组件实例
        var findVueInstance = (el) => {
            while (el) {
                if (el.__vue__) return el.__vue__;
                if (el._vnode?.component?.proxy) return el._vnode.component.proxy;
                el = el.parentElement;
            }
            return null;
        };
        
        var comp = findVueInstance(chatEl);
        if (!comp) return { error: '未找到Vue组件' };
        
        // 列出所有方法
        var methods = [];
        for (var key in comp) {
            if (typeof comp[key] === 'function' && !key.startsWith('_') && !key.startsWith('$')) {
                methods.push(key);
            }
        }
        
        return {
            componentFound: true,
            methodCount: methods.length,
            sendMethods: methods.filter(m => m.toLowerCase().includes('send') || m.toLowerCase().includes('submit'))
        };
    })()`, false);
    console.log('Vue组件分析:', vueAnalysis);
    
    console.log('\n');
    ws.close();
}

main().catch(console.error);
