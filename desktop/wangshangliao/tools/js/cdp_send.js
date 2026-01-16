/**
 * 使用CDP直接模拟键盘输入发送消息
 */
const WebSocket = require('ws');
const http = require('http');

let ws = null;
let msgId = 0;

const LOGO_ACCOUNT = '1391351554';
const TEST_MSG = '机器人测试' + Date.now();

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

function sendCDP(method, params = {}) {
    return new Promise((resolve, reject) => {
        const id = ++msgId;
        const timeout = setTimeout(() => reject(new Error('Timeout')), 10000);
        const handler = (data) => {
            const msg = JSON.parse(data.toString());
            if (msg.id === id) {
                clearTimeout(timeout);
                ws.off('message', handler);
                resolve(msg.result);
            }
        };
        ws.on('message', handler);
        ws.send(JSON.stringify({ id, method, params }));
    });
}

function evaluate(expression, awaitPromise = true) {
    return sendCDP('Runtime.evaluate', { expression, awaitPromise, returnByValue: true })
        .then(r => r?.result?.value);
}

async function main() {
    console.log('🔍 CDP直接发送消息\n');
    console.log('测试消息:', TEST_MSG);
    console.log('\n⚠️ 请确保：');
    console.log('1. 已关闭所有弹窗');
    console.log('2. 已点击左侧的"logo"会话');
    console.log('3. 聊天窗口已打开\n');
    
    const wsUrl = await getWebSocketUrl();
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    console.log('✅ 已连接\n');
    
    // 等待用户准备
    console.log('等待3秒...\n');
    await new Promise(r => setTimeout(r, 3000));
    
    // 1. 聚焦输入框
    console.log('=== 1. 聚焦输入框 ===\n');
    const focusResult = await evaluate(`(() => {
        var input = document.querySelector('[contenteditable="true"]');
        if (!input) return { error: '未找到输入框' };
        
        input.focus();
        
        // 选中所有内容并删除
        var range = document.createRange();
        range.selectNodeContents(input);
        var sel = window.getSelection();
        sel.removeAllRanges();
        sel.addRange(range);
        
        return { success: true, focused: true };
    })()`, false);
    console.log('聚焦结果:', focusResult);
    
    // 2. 使用CDP Input.insertText 输入文字
    console.log('\n=== 2. 输入文字 ===\n');
    
    // 先清空
    await sendCDP('Input.dispatchKeyEvent', {
        type: 'keyDown',
        key: 'a',
        code: 'KeyA',
        modifiers: 2  // Ctrl
    });
    await sendCDP('Input.dispatchKeyEvent', {
        type: 'keyUp',
        key: 'a',
        code: 'KeyA',
        modifiers: 2
    });
    
    await new Promise(r => setTimeout(r, 100));
    
    // 删除选中内容
    await sendCDP('Input.dispatchKeyEvent', {
        type: 'keyDown',
        key: 'Backspace',
        code: 'Backspace'
    });
    await sendCDP('Input.dispatchKeyEvent', {
        type: 'keyUp',
        key: 'Backspace',
        code: 'Backspace'
    });
    
    await new Promise(r => setTimeout(r, 100));
    
    // 输入文字
    await sendCDP('Input.insertText', { text: TEST_MSG });
    console.log('已输入文字');
    
    // 3. 检查输入框内容
    const inputContent = await evaluate(`(() => {
        var input = document.querySelector('[contenteditable="true"]');
        return input ? input.textContent : null;
    })()`, false);
    console.log('输入框内容:', inputContent);
    
    await new Promise(r => setTimeout(r, 500));
    
    // 4. 按Enter发送
    console.log('\n=== 3. 按Enter发送 ===\n');
    
    await sendCDP('Input.dispatchKeyEvent', {
        type: 'keyDown',
        key: 'Enter',
        code: 'Enter',
        windowsVirtualKeyCode: 13,
        nativeVirtualKeyCode: 13
    });
    await sendCDP('Input.dispatchKeyEvent', {
        type: 'keyUp',
        key: 'Enter',
        code: 'Enter',
        windowsVirtualKeyCode: 13,
        nativeVirtualKeyCode: 13
    });
    
    console.log('已发送Enter键');
    
    // 5. 等待并检查
    console.log('\n=== 4. 等待检查... ===\n');
    await new Promise(r => setTimeout(r, 2000));
    
    // 检查输入框是否清空（消息发送后输入框会清空）
    const afterContent = await evaluate(`(() => {
        var input = document.querySelector('[contenteditable="true"]');
        return input ? input.textContent : null;
    })()`, false);
    console.log('发送后输入框:', afterContent || '(已清空)');
    
    if (!afterContent || afterContent.trim() === '') {
        console.log('\n✅ 输入框已清空，消息可能已发送！');
    } else {
        console.log('\n⚠️ 输入框未清空，消息可能未发送');
    }
    
    // 检查历史消息
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
                        time: new Date(m.time).toLocaleTimeString()
                    })));
                }
            });
            setTimeout(() => r([]), 5000);
        });
    })()`);
    
    console.log('\n最新消息:');
    (history || []).forEach((m, i) => {
        console.log(`  ${i + 1}. [${m.flow}] ${m.type}: ${m.text || '(无)'} @ ${m.time}`);
    });
    
    console.log('\n');
    ws.close();
}

main().catch(console.error);
