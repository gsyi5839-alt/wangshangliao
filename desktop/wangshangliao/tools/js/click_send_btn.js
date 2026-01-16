/**
 * 直接点击旺商聊的发送按钮
 */
const WebSocket = require('ws');
const http = require('http');

let ws = null;
let msgId = 0;

const LOGO_ACCOUNT = '1391351554';
const TEST_MESSAGE = '【机器人测试】' + new Date().toLocaleTimeString();

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
    console.log('🔍 直接点击发送按钮测试\n');
    
    const wsUrl = await getWebSocketUrl();
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    console.log('✅ 已连接\n');
    
    // 1. 首先Hook消息发送以便捕获
    console.log('=== 1. 安装发送Hook ===\n');
    await evaluate(`(() => {
        window.__sendCaptures = [];
        
        // Hook sendCustomMsg
        var origCustom = window.nim.sendCustomMsg.bind(window.nim);
        window.nim.sendCustomMsg = function(opts) {
            console.log('[HOOK sendCustomMsg]', JSON.stringify(opts));
            window.__sendCaptures.push({ method: 'sendCustomMsg', opts: JSON.parse(JSON.stringify(opts)), time: Date.now() });
            return origCustom(opts);
        };
        
        // Hook sendText
        var origText = window.nim.sendText.bind(window.nim);
        window.nim.sendText = function(opts) {
            console.log('[HOOK sendText]', JSON.stringify(opts));
            window.__sendCaptures.push({ method: 'sendText', opts: JSON.parse(JSON.stringify(opts)), time: Date.now() });
            return origText(opts);
        };
        
        return true;
    })()`, false);
    console.log('✅ Hook已安装\n');
    
    // 2. 在输入框输入文字
    console.log('=== 2. 输入文字 ===\n');
    const inputResult = await evaluate(`(() => {
        // 找到contenteditable输入框
        var input = document.querySelector('[contenteditable="true"]');
        if (!input) return { error: '未找到输入框' };
        
        // 聚焦
        input.focus();
        
        // 清空并输入
        input.innerHTML = '';
        input.textContent = '${TEST_MESSAGE}';
        
        // 触发事件
        input.dispatchEvent(new Event('input', { bubbles: true }));
        input.dispatchEvent(new Event('change', { bubbles: true }));
        
        return { 
            success: true, 
            content: input.textContent
        };
    })()`, false);
    console.log('输入结果:', inputResult);
    
    await new Promise(r => setTimeout(r, 500));
    
    // 3. 点击发送按钮
    console.log('\n=== 3. 点击发送按钮 ===\n');
    const clickResult = await evaluate(`(() => {
        // 找发送按钮
        var sendBtn = document.querySelector('button.bg-\\\\#2E7BFD');
        if (!sendBtn) {
            // 找包含"发送"文字的按钮
            var allBtns = document.querySelectorAll('button');
            allBtns.forEach(btn => {
                if (btn.textContent?.includes('发送')) {
                    sendBtn = btn;
                }
            });
        }
        
        if (!sendBtn) return { error: '未找到发送按钮', allBtns: document.querySelectorAll('button').length };
        
        // 点击
        sendBtn.click();
        
        return { 
            success: true,
            buttonText: sendBtn.textContent,
            className: sendBtn.className
        };
    })()`, false);
    console.log('点击结果:', clickResult);
    
    // 4. 等待并检查发送Hook
    console.log('\n=== 4. 等待检查发送结果 ===\n');
    await new Promise(r => setTimeout(r, 2000));
    
    const captures = await evaluate(`(() => window.__sendCaptures || [])()`, false);
    console.log('捕获的发送调用:', captures?.length || 0, '个');
    (captures || []).forEach((c, i) => {
        console.log(`\n--- 调用 ${i + 1}: ${c.method} ---`);
        console.log('选项:', JSON.stringify(c.opts, null, 2));
    });
    
    // 5. 检查历史消息
    console.log('\n=== 5. 检查历史消息 ===\n');
    const history = await evaluate(`(async () => {
        return new Promise(r => {
            window.nim.getHistoryMsgs({
                scene: 'p2p',
                to: '${LOGO_ACCOUNT}',
                limit: 5,
                done: (err, obj) => {
                    r((obj?.msgs || []).map(m => ({
                        flow: m.flow,
                        type: m.type,
                        status: m.status,
                        text: m.text?.substring(0, 50) || '(无)',
                        hasContent: !!m.content,
                        time: new Date(m.time).toLocaleTimeString()
                    })));
                }
            });
            setTimeout(() => r([]), 5000);
        });
    })()`);
    
    console.log('最新消息:');
    (history || []).forEach((m, i) => {
        console.log(`  ${i + 1}. [${m.flow}] ${m.type}: ${m.text} (${m.status}) @ ${m.time}`);
    });
    
    console.log('\n');
    ws.close();
}

main().catch(console.error);
