/**
 * 正确发送消息 - 基于UI分析
 */
const WebSocket = require('ws');
const http = require('http');

let ws = null;
let msgId = 0;

const TEST_MSG = '机器人测试' + Date.now();

async function getWebSocketUrl() {
    return new Promise((resolve, reject) => {
        const req = http.get('http://127.0.0.1:9222/json', (res) => {
            let d = '';
            res.on('data', c => d += c);
            res.on('end', () => resolve(JSON.parse(d)));
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

function evaluate(expression) {
    return sendCDP('Runtime.evaluate', { expression, awaitPromise: true, returnByValue: true })
        .then(r => r?.result?.value);
}

async function main() {
    console.log('🔍 正确发送消息\n');
    console.log('测试消息:', TEST_MSG, '\n');
    
    const res = await getWebSocketUrl();
    const wsUrl = res.find(p => p.url?.includes('index.html'))?.webSocketDebuggerUrl || res[0]?.webSocketDebuggerUrl;
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    console.log('✅ 已连接\n');
    
    // 1. 关闭弹窗
    console.log('=== 1. 关闭弹窗 ===\n');
    const closeResult = await evaluate(`(() => {
        // 查找关闭按钮
        var closeBtn = document.querySelector('.el-dialog__headerbtn, [class*="close"], .el-icon-close');
        if (closeBtn) {
            closeBtn.click();
            return { closed: 'button' };
        }
        
        // 点击遮罩层关闭
        var overlay = document.querySelector('.el-overlay');
        if (overlay) {
            overlay.click();
            return { closed: 'overlay' };
        }
        
        // 按ESC
        document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', keyCode: 27, bubbles: true }));
        return { closed: 'esc' };
    })()`);
    console.log('关闭结果:', closeResult);
    
    await new Promise(r => setTimeout(r, 500));
    
    // 2. 检查弹窗是否关闭
    const dialogCheck = await evaluate(`(() => {
        var dialog = document.querySelector('.el-dialog');
        return {
            dialogExists: !!dialog,
            dialogVisible: dialog ? dialog.offsetParent !== null : false
        };
    })()`);
    console.log('弹窗状态:', dialogCheck);
    
    if (dialogCheck?.dialogVisible) {
        console.log('\n⚠️ 弹窗未关闭，请手动关闭后重试');
        ws.close();
        return;
    }
    
    // 3. 使用CDP点击logo会话位置 (x=118, y=528)
    console.log('\n=== 2. 点击logo会话 ===\n');
    
    // 先获取logo的准确位置
    const logoPos = await evaluate(`(() => {
        var allElements = document.querySelectorAll('p, span, div');
        for (var i = 0; i < allElements.length; i++) {
            var el = allElements[i];
            if (el.textContent?.trim() === 'logo' && 
                !el.closest('.el-dialog') && 
                !el.closest('[class*="member"]')) {
                var rect = el.getBoundingClientRect();
                // 找到包含这个元素的会话项
                var sessionItem = el.closest('[class*="session"]') || 
                                  el.closest('[class*="item"]') || 
                                  el.parentElement?.parentElement?.parentElement;
                var sessionRect = sessionItem ? sessionItem.getBoundingClientRect() : rect;
                return {
                    found: true,
                    x: sessionRect.x + sessionRect.width / 2,
                    y: sessionRect.y + sessionRect.height / 2,
                    elementX: rect.x,
                    elementY: rect.y
                };
            }
        }
        return { found: false };
    })()`);
    console.log('logo位置:', logoPos);
    
    if (!logoPos?.found) {
        console.log('❌ 未找到logo会话');
        ws.close();
        return;
    }
    
    // 使用CDP鼠标点击
    await sendCDP('Input.dispatchMouseEvent', {
        type: 'mousePressed',
        x: logoPos.x,
        y: logoPos.y,
        button: 'left',
        clickCount: 1
    });
    await sendCDP('Input.dispatchMouseEvent', {
        type: 'mouseReleased',
        x: logoPos.x,
        y: logoPos.y,
        button: 'left',
        clickCount: 1
    });
    console.log('已点击位置:', logoPos.x, logoPos.y);
    
    await new Promise(r => setTimeout(r, 1000));
    
    // 4. 检查当前聊天对象
    console.log('\n=== 3. 检查当前聊天 ===\n');
    const currentChat = await evaluate(`(() => {
        // 查找聊天头部的名称
        var header = document.querySelector('[class*="chat-header"], [class*="header"] [class*="name"]');
        return {
            headerText: header?.textContent?.substring(0, 30),
            // 检查是否是私聊
            isP2P: !document.querySelector('[class*="group-info"], [class*="team-info"]')
        };
    })()`);
    console.log('当前聊天:', currentChat);
    
    // 5. 输入消息
    console.log('\n=== 4. 输入消息 ===\n');
    
    // 聚焦输入框
    await evaluate(`(() => {
        var input = document.querySelector('[contenteditable="true"]');
        if (input) {
            input.focus();
            input.innerHTML = '';
        }
    })()`);
    
    await new Promise(r => setTimeout(r, 100));
    
    // 输入文字
    await sendCDP('Input.insertText', { text: TEST_MSG });
    
    const inputContent = await evaluate(`(() => {
        var input = document.querySelector('[contenteditable="true"]');
        return input?.textContent;
    })()`);
    console.log('输入内容:', inputContent);
    
    // 6. 按Enter发送
    console.log('\n=== 5. 发送消息 ===\n');
    await sendCDP('Input.dispatchKeyEvent', {
        type: 'keyDown',
        key: 'Enter',
        code: 'Enter',
        windowsVirtualKeyCode: 13
    });
    await sendCDP('Input.dispatchKeyEvent', {
        type: 'keyUp',
        key: 'Enter',
        code: 'Enter',
        windowsVirtualKeyCode: 13
    });
    console.log('已按Enter');
    
    await new Promise(r => setTimeout(r, 2000));
    
    // 7. 检查结果
    const afterContent = await evaluate(`(() => {
        var input = document.querySelector('[contenteditable="true"]');
        return input?.textContent || '';
    })()`);
    
    if (!afterContent.trim()) {
        console.log('\n✅ 输入框已清空，消息已发送！');
    } else {
        console.log('\n⚠️ 输入框未清空:', afterContent);
    }
    
    // 检查历史
    const history = await evaluate(`(async () => {
        return new Promise(r => {
            window.nim.getHistoryMsgs({
                scene: 'p2p',
                to: '1391351554',
                limit: 3,
                done: (err, obj) => {
                    r((obj?.msgs || []).map(m => ({
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
        console.log(`  ${i + 1}. ${m.type}: ${m.text || '(无)'} @ ${m.time}`);
    });
    
    ws.close();
}

main().catch(console.error);
