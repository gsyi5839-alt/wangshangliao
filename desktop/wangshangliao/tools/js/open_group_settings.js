/**
 * 直接打开旺商聊群聊设置窗口
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
    console.log('=== 打开群聊设置窗口 ===\n');
    
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
    
    // 打开群设置窗口
    const result = await evaluate(`
(async function() {
    var result = { success: false, message: '', steps: [] };
    
    function sleep(ms) { return new Promise(function(r) { setTimeout(r, ms); }); }
    
    try {
        // 方法1: 点击右侧群成员面板上方的设置图标
        var settingsIcon = document.querySelector('[class*="group-setting"]') ||
                          document.querySelector('[title="群设置"]') ||
                          document.querySelector('[title="设置"]');
        
        if (settingsIcon) {
            result.steps.push('找到设置图标');
            settingsIcon.click();
            await sleep(500);
            result.success = true;
            result.message = '已点击设置图标';
            return JSON.stringify(result, null, 2);
        }
        
        // 方法2: 点击群头像旁边的下拉菜单
        result.steps.push('尝试下拉菜单方式...');
        
        // 找到群聊头部区域的下拉按钮
        var dropdowns = document.querySelectorAll('.el-dropdown');
        var headerDropdown = null;
        
        for (var i = 0; i < dropdowns.length; i++) {
            var dd = dropdowns[i];
            var rect = dd.getBoundingClientRect();
            // 在顶部区域的下拉菜单
            if (rect.top < 150 && rect.top > 0 && rect.width > 0) {
                headerDropdown = dd;
                break;
            }
        }
        
        if (headerDropdown) {
            result.steps.push('找到头部下拉菜单');
            
            // 点击触发下拉菜单
            var trigger = headerDropdown.querySelector('.el-dropdown-link') || 
                         headerDropdown.querySelector('[class*="trigger"]') ||
                         headerDropdown;
            trigger.click();
            await sleep(300);
            
            result.steps.push('已点击下拉触发器');
            
            // 查找"查看群名片"或"设置"选项
            var menuItems = document.querySelectorAll('.el-dropdown-menu__item');
            var settingsItem = null;
            
            for (var j = 0; j < menuItems.length; j++) {
                var item = menuItems[j];
                var text = item.textContent || '';
                if (text.includes('设置') || text.includes('群名片') || text.includes('群资料')) {
                    settingsItem = item;
                    result.steps.push('找到: ' + text);
                    break;
                }
            }
            
            if (settingsItem) {
                settingsItem.click();
                await sleep(500);
                result.success = true;
                result.message = '已打开群设置';
            } else {
                // 列出所有菜单项
                result.menuItems = [];
                menuItems.forEach(function(item) {
                    result.menuItems.push(item.textContent);
                });
                result.message = '未找到设置选项';
            }
        } else {
            result.message = '未找到下拉菜单';
        }
        
        // 方法3: 直接点击群名称区域
        if (!result.success) {
            result.steps.push('尝试点击群名称...');
            
            var groupNameArea = document.querySelector('[class*="chat-header"]') ||
                               document.querySelector('[class*="group-name"]');
            
            if (groupNameArea) {
                groupNameArea.click();
                await sleep(500);
                result.steps.push('已点击群名称区域');
            }
        }
        
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})();`);

    console.log(result);
    
    ws.close();
}

main().catch(console.error);

