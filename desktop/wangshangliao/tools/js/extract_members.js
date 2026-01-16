/**
 * 从旺商聊界面提取群成员信息
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
    console.log('Connecting to WangShangLiao...\n');
    
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
    
    // 直接从 DOM 提取群成员列表
    const script = `
(async function() {
    var result = {
        success: false,
        members: [],
        error: null
    };
    
    try {
        // 查找所有可能的群成员元素
        var selectors = [
            '.member-list-item',
            '[class*="member-item"]',
            '[class*="group-member"]',
            '.member-info',
            '.contact-item',
            '[data-member-id]',
            '.list-item'
        ];
        
        var allElements = [];
        selectors.forEach(function(sel) {
            var els = document.querySelectorAll(sel);
            if (els.length > 0) {
                result['selector_' + sel] = els.length;
                els.forEach(function(el) { allElements.push(el); });
            }
        });
        
        result.totalElements = allElements.length;
        
        // 查找页面上所有包含昵称的文本
        var allText = document.body.innerText;
        var lines = allText.split('\\n').filter(function(l) { return l.trim().length > 0 && l.trim().length < 20; });
        result.sampleLines = lines.slice(0, 30);
        
        // 检查右侧群成员面板
        var rightPanel = document.querySelector('.right-panel, [class*="right-panel"], [class*="sidebar-right"]');
        if (rightPanel) {
            result.rightPanelText = rightPanel.innerText.substring(0, 500);
        }
        
        // 检查群成员搜索框附近
        var searchInput = document.querySelector('input[placeholder*="搜索"], input[placeholder*="成员"]');
        if (searchInput) {
            result.searchInputFound = true;
            var parent = searchInput.parentElement;
            while (parent && parent !== document.body) {
                if (parent.innerText && parent.innerText.length > 100) {
                    result.searchAreaText = parent.innerText.substring(0, 500);
                    break;
                }
                parent = parent.parentElement;
            }
        }
        
        // 尝试获取群详情按钮并模拟点击（不实际执行，只记录信息）
        var groupDetailBtn = document.querySelector('[class*="group-detail"], [class*="more-info"], button[title*="群"]');
        if (groupDetailBtn) {
            result.groupDetailBtnFound = true;
        }
        
        result.success = true;
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})();`;

    const data = await evaluate(script);
    console.log('=== DOM 提取结果 ===\n');
    const parsed = JSON.parse(data);
    console.log('成功:', parsed.success);
    console.log('总元素数:', parsed.totalElements);
    console.log('示例文本行:', parsed.sampleLines?.slice(0, 15));
    if (parsed.rightPanelText) {
        console.log('\n右侧面板文本:\n', parsed.rightPanelText);
    }
    if (parsed.searchAreaText) {
        console.log('\n搜索区域文本:\n', parsed.searchAreaText);
    }
    
    ws.close();
}

main().catch(console.error);

