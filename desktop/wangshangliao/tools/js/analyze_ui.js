/**
 * 分析旺商聊UI结构
 */
const WebSocket = require('ws');
const http = require('http');

let ws = null;
let msgId = 0;

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

function evaluate(expression) {
    return new Promise((resolve, reject) => {
        const id = ++msgId;
        const timeout = setTimeout(() => reject(new Error('Timeout')), 15000);
        const handler = (data) => {
            const msg = JSON.parse(data.toString());
            if (msg.id === id) {
                clearTimeout(timeout);
                ws.off('message', handler);
                resolve(msg.result?.result?.value);
            }
        };
        ws.on('message', handler);
        ws.send(JSON.stringify({ 
            id, 
            method: 'Runtime.evaluate', 
            params: { expression, awaitPromise: true, returnByValue: true } 
        }));
    });
}

async function main() {
    const res = await getWebSocketUrl();
    const wsUrl = res.find(p => p.url?.includes('index.html'))?.webSocketDebuggerUrl || res[0]?.webSocketDebuggerUrl;
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    
    console.log('🔍 分析旺商聊UI结构\n');
    
    // 1. 分析页面主要布局
    console.log('=== 1. 页面主要布局 ===\n');
    const layout = await evaluate(`(() => {
        var result = [];
        
        // 获取主要区域
        var mainDivs = document.querySelectorAll('body > div, #app > div');
        mainDivs.forEach(div => {
            if (div.className && div.offsetWidth > 100) {
                result.push({
                    className: div.className.substring(0, 60),
                    width: div.offsetWidth,
                    height: div.offsetHeight
                });
            }
        });
        
        return result.slice(0, 10);
    })()`);
    console.log('主要区域:', layout);
    
    // 2. 查找左侧会话列表
    console.log('\n=== 2. 左侧会话列表 ===\n');
    const sessionList = await evaluate(`(() => {
        var result = [];
        
        // 查找所有可能的会话项
        var items = document.querySelectorAll('[class*="session"], [class*="chat-item"], [class*="conversation"], [class*="list-item"]');
        
        items.forEach((item, i) => {
            if (i < 15) {
                // 获取会话名称
                var nameEl = item.querySelector('[class*="name"], [class*="title"], [class*="nick"]');
                var name = nameEl?.textContent?.trim() || item.textContent?.substring(0, 20)?.trim();
                
                result.push({
                    index: i,
                    className: item.className?.substring(0, 50),
                    name: name?.substring(0, 15),
                    tagName: item.tagName,
                    hasAvatar: !!item.querySelector('img, [class*="avatar"]')
                });
            }
        });
        
        return result;
    })()`);
    console.log('会话列表项:');
    (sessionList || []).forEach(s => {
        console.log(`  ${s.index}. [${s.tagName}] "${s.name}" - class: ${s.className}`);
    });
    
    // 3. 查找当前是否有弹窗
    console.log('\n=== 3. 检查弹窗 ===\n');
    const modals = await evaluate(`(() => {
        var result = [];
        
        var modalElements = document.querySelectorAll('[class*="modal"], [class*="dialog"], [class*="popup"], [class*="overlay"]');
        modalElements.forEach(el => {
            if (el.offsetWidth > 0 && el.offsetHeight > 0) {
                result.push({
                    className: el.className?.substring(0, 50),
                    visible: el.style.display !== 'none',
                    width: el.offsetWidth,
                    text: el.textContent?.substring(0, 50)
                });
            }
        });
        
        return result;
    })()`);
    console.log('弹窗:', modals);
    
    // 4. 查找输入框位置
    console.log('\n=== 4. 输入框 ===\n');
    const inputInfo = await evaluate(`(() => {
        var input = document.querySelector('[contenteditable="true"]');
        if (!input) return { error: '未找到输入框' };
        
        var rect = input.getBoundingClientRect();
        return {
            found: true,
            className: input.className?.substring(0, 50),
            x: rect.x,
            y: rect.y,
            width: rect.width,
            height: rect.height,
            parent: input.parentElement?.className?.substring(0, 50)
        };
    })()`);
    console.log('输入框:', inputInfo);
    
    // 5. 查找发送按钮
    console.log('\n=== 5. 发送按钮 ===\n');
    const sendBtn = await evaluate(`(() => {
        var buttons = document.querySelectorAll('button');
        var result = [];
        
        buttons.forEach(btn => {
            var text = btn.textContent?.trim();
            if (text === '发送' || btn.className?.includes('send')) {
                var rect = btn.getBoundingClientRect();
                result.push({
                    text: text,
                    className: btn.className?.substring(0, 50),
                    x: rect.x,
                    y: rect.y,
                    disabled: btn.disabled
                });
            }
        });
        
        return result;
    })()`);
    console.log('发送按钮:', sendBtn);
    
    // 6. 分析logo会话的精确位置
    console.log('\n=== 6. 查找logo会话项 ===\n');
    const logoItem = await evaluate(`(() => {
        // 方法1: 通过文本内容查找
        var allElements = document.querySelectorAll('*');
        var logoElement = null;
        
        for (var i = 0; i < allElements.length; i++) {
            var el = allElements[i];
            // 查找直接包含"logo"文本的元素（不是在弹窗中）
            if (el.childNodes.length === 1 && 
                el.childNodes[0].nodeType === 3 && 
                el.textContent?.trim() === 'logo') {
                
                // 检查是否在弹窗中
                var inModal = el.closest('[class*="modal"], [class*="dialog"], [class*="member"]');
                if (!inModal) {
                    var rect = el.getBoundingClientRect();
                    logoElement = {
                        found: true,
                        tagName: el.tagName,
                        className: el.className?.substring(0, 50),
                        x: rect.x,
                        y: rect.y,
                        parentClass: el.parentElement?.className?.substring(0, 50),
                        grandParentClass: el.parentElement?.parentElement?.className?.substring(0, 50)
                    };
                    break;
                }
            }
        }
        
        return logoElement || { found: false };
    })()`);
    console.log('logo会话位置:', logoItem);
    
    ws.close();
}

main().catch(console.error);
