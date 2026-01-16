/**
 * 正确发送消息 - 在私聊中使用Enter键发送
 */
const WebSocket = require('ws');
const http = require('http');

let ws = null;
let msgId = 0;

const LOGO_ACCOUNT = '1391351554';
const TEST_MSG = '【机器人测试】' + new Date().toLocaleTimeString();

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
    console.log('🔍 正确发送测试 - 私聊 + Enter键\n');
    console.log('测试消息:', TEST_MSG);
    
    const wsUrl = await getWebSocketUrl();
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    console.log('✅ 已连接\n');
    
    // 1. 先关闭任何弹窗
    console.log('=== 1. 关闭弹窗 ===\n');
    await evaluate(`(() => {
        // 关闭所有弹窗
        var closeButtons = document.querySelectorAll('[class*="close"], [class*="modal"] button');
        closeButtons.forEach(btn => {
            if (btn.textContent?.includes('×') || btn.className?.includes('close')) {
                btn.click();
            }
        });
        
        // 按ESC关闭弹窗
        document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', keyCode: 27 }));
        
        return true;
    })()`, false);
    
    await new Promise(r => setTimeout(r, 500));
    
    // 2. 点击左侧会话列表中的logo会话
    console.log('=== 2. 点击logo会话 ===\n');
    const clickSession = await evaluate(`(() => {
        // 查找会话列表中包含"logo"的项
        var sessionList = document.querySelectorAll('[class*="session"], [class*="chat-item"], [class*="conversation"]');
        var found = false;
        
        // 遍历所有可能的会话元素
        var allItems = document.querySelectorAll('div, li, span');
        for (var i = 0; i < allItems.length; i++) {
            var item = allItems[i];
            var text = item.textContent || '';
            
            // 查找包含"logo"的会话项（但不是在弹窗中）
            if (text.trim() === 'logo' && 
                !item.closest('[class*="modal"]') && 
                !item.closest('[class*="dialog"]') &&
                item.closest('[class*="session"]')) {
                
                // 点击这个会话
                var clickTarget = item.closest('[class*="session"]') || item;
                clickTarget.click();
                found = true;
                return { success: true, text: text.substring(0, 20) };
            }
        }
        
        return { success: false, sessionCount: sessionList.length };
    })()`, false);
    console.log('点击会话:', clickSession);
    
    await new Promise(r => setTimeout(r, 1000));
    
    // 3. 检查当前会话是否是p2p-logo
    console.log('\n=== 3. 检查当前会话 ===\n');
    const currentSession = await evaluate(`(() => {
        // 检查当前URL或会话状态
        var currSession = window.nim?.currSession || null;
        
        return {
            currSession: currSession,
            // 检查页面标题或聊天对象名称
            chatTitle: document.querySelector('[class*="chat-header"], [class*="title"]')?.textContent?.substring(0, 30)
        };
    })()`, false);
    console.log('当前会话:', currentSession);
    
    // 4. 在输入框输入消息
    console.log('\n=== 4. 输入消息 ===\n');
    const inputResult = await evaluate(`(() => {
        var input = document.querySelector('[contenteditable="true"]');
        if (!input) return { error: '未找到输入框' };
        
        // 聚焦输入框
        input.focus();
        
        // 清空并输入
        input.innerHTML = '';
        input.textContent = '${TEST_MSG}';
        
        // 触发input事件
        input.dispatchEvent(new Event('input', { bubbles: true }));
        input.dispatchEvent(new Event('change', { bubbles: true }));
        
        return { success: true, content: input.textContent };
    })()`, false);
    console.log('输入结果:', inputResult);
    
    await new Promise(r => setTimeout(r, 300));
    
    // 5. 按Enter键发送
    console.log('\n=== 5. 按Enter键发送 ===\n');
    const enterResult = await evaluate(`(() => {
        var input = document.querySelector('[contenteditable="true"]');
        if (!input) return { error: '未找到输入框' };
        
        // 确保输入框有焦点
        input.focus();
        
        // 发送Enter键事件
        var enterEvent = new KeyboardEvent('keydown', {
            key: 'Enter',
            code: 'Enter',
            keyCode: 13,
            which: 13,
            bubbles: true,
            cancelable: true
        });
        
        input.dispatchEvent(enterEvent);
        
        return { success: true, sent: 'Enter key dispatched' };
    })()`, false);
    console.log('Enter键结果:', enterResult);
    
    // 6. 等待并检查历史
    console.log('\n=== 6. 等待检查消息... ===\n');
    await new Promise(r => setTimeout(r, 3000));
    
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
