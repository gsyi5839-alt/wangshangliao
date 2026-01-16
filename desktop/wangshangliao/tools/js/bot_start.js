/**
 * 旺商聊机器人 - 快速启动版
 * 用法: node bot_start.js [群号]
 * 
 * 示例:
 *   node bot_start.js 3333338888   # 绑定天谕群
 *   node bot_start.js              # 列出群并选择
 */
const http = require('http');
const WebSocket = require('ws');

// ====================== 配置 ======================
const CONFIG = {
    cdpPort: 9222,
    pollInterval: 1000,
    maxStoredMsgs: 100,
};

// 自动回复规则
const AUTO_REPLY_RULES = [
    { keywords: ['财富', '发财富', '财付通', 'cf', 'CF'], reply: '私聊前排接单' },
    { keywords: ['支付', '支付宝', 'zf', 'ZF'], reply: '私聊前排接单' },
    { keywords: ['微信', '发微信', '微信号'], reply: '私聊前排接单' },
];

// ====================== 全局状态 ======================
let ws = null;
let cmdId = 0;
let pendingCommands = new Map();
let processedMsgIds = new Set();

// ====================== CDP 通信 ======================
async function connectCDP() {
    const pages = await new Promise((resolve, reject) => {
        http.get(`http://127.0.0.1:${CONFIG.cdpPort}/json`, (res) => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', () => resolve(JSON.parse(data)));
        }).on('error', reject);
    });
    
    const wslPage = pages.find(p => p.title && p.title.includes('旺商聊'));
    if (!wslPage) throw new Error('未找到旺商聊页面');
    
    ws = new WebSocket(wslPage.webSocketDebuggerUrl);
    await new Promise(r => ws.on('open', r));
    
    ws.on('message', (data) => {
        const msg = JSON.parse(data.toString());
        if (msg.id && pendingCommands.has(msg.id)) {
            pendingCommands.get(msg.id).resolve(msg.result);
            pendingCommands.delete(msg.id);
        }
    });
}

async function evalJS(js) {
    const id = ++cmdId;
    return new Promise((resolve, reject) => {
        pendingCommands.set(id, { resolve: (r) => resolve(r?.result?.value), reject });
        ws.send(JSON.stringify({ id, method: 'Runtime.evaluate', params: { expression: js, returnByValue: true, awaitPromise: true } }));
        setTimeout(() => { pendingCommands.delete(id); reject(new Error('超时')); }, 15000);
    });
}

// ====================== 旺商聊操作 ======================
async function getGroupList() {
    const js = `(function() {
        var ms = JSON.parse(localStorage.getItem('managestate') || '{}');
        var groups = [];
        if (ms.groupList) {
            ['owner', 'member'].forEach(function(t) {
                if (ms.groupList[t]) {
                    ms.groupList[t].forEach(function(g) {
                        groups.push({ name: g.name || g.groupName, account: g.groupAccount, cloudId: g.groupCloudId, type: t });
                    });
                }
            });
        }
        return JSON.stringify(groups);
    })()`;
    return JSON.parse(await evalJS(js));
}

async function getCurrentSession() {
    const js = `(function() {
        var ms = JSON.parse(localStorage.getItem('managestate') || '{}');
        if (ms.currSession && ms.currSession.group) {
            return JSON.stringify({ name: ms.currSession.group.name, account: ms.currSession.group.groupAccount });
        }
        return 'null';
    })()`;
    const r = await evalJS(js);
    return r === 'null' ? null : JSON.parse(r);
}

async function getMessagesFromDOM() {
    const js = `(function() {
        var messages = [];
        document.querySelectorAll('.msg-item.user-msg').forEach(function(item, idx) {
            var content = (item.querySelector('[class*="msg-content"]') || item.querySelector('[class*="content"]'))?.textContent?.trim() || '';
            var time = item.querySelector('[class*="time"]')?.textContent?.trim() || '';
            if (time && content.endsWith(time)) content = content.slice(0, -time.length).trim();
            if (content) messages.push({ content: content, time: time });
        });
        return JSON.stringify(messages);
    })()`;
    return JSON.parse(await evalJS(js));
}

async function sendMessage(text) {
    const js = `(async function() {
        var editor = document.querySelector('#con_edit');
        if (!editor) return '{"success":false,"error":"no editor"}';
        editor.focus(); editor.innerHTML = '';
        document.execCommand('insertText', false, '${text.replace(/'/g, "\\'")}');
        await new Promise(r => setTimeout(r, 100));
        var btn = Array.from(document.querySelectorAll('button')).find(b => b.className.includes('blue-color'));
        if (!btn) return '{"success":false,"error":"no button"}';
        btn.click();
        return '{"success":true}';
    })()`;
    return JSON.parse(await evalJS(js));
}

// ====================== 主程序 ======================
async function main() {
    const targetGroup = process.argv[2];
    
    console.log('═'.repeat(50));
    console.log('     旺商聊机器人 - 快速启动');
    console.log('═'.repeat(50));
    
    console.log('\n正在连接 CDP...');
    await connectCDP();
    console.log('✓ CDP 已连接');
    
    // 获取群列表
    const groups = await getGroupList();
    console.log(`\n找到 ${groups.length} 个群聊:\n`);
    groups.forEach((g, i) => {
        const marker = targetGroup && g.account === targetGroup ? ' ← 目标' : '';
        console.log(`  ${i + 1}. ${g.name} (${g.account})${marker}`);
    });
    
    // 确定目标群
    let boundGroup = null;
    if (targetGroup) {
        boundGroup = groups.find(g => g.account === targetGroup);
        if (!boundGroup) {
            console.log(`\n✗ 未找到群号 ${targetGroup}`);
            process.exit(1);
        }
    } else if (groups.length === 1) {
        boundGroup = groups[0];
    } else {
        console.log('\n用法: node bot_start.js <群号>');
        console.log('示例: node bot_start.js 3333338888');
        process.exit(0);
    }
    
    console.log(`\n✓ 绑定群: ${boundGroup.name} (${boundGroup.account})`);
    
    // 检查当前会话
    const current = await getCurrentSession();
    if (!current || current.account !== boundGroup.account) {
        console.log(`\n⚠ 请在旺商聊中切换到 [${boundGroup.name}] 群`);
        console.log('   然后重新运行此脚本');
        process.exit(0);
    }
    
    console.log('✓ 当前会话已是目标群');
    
    // 标记现有消息
    const existingMsgs = await getMessagesFromDOM();
    existingMsgs.forEach(msg => {
        processedMsgIds.add(Buffer.from(msg.content + msg.time).toString('base64').substring(0, 20));
    });
    console.log(`✓ 已标记 ${existingMsgs.length} 条现有消息`);
    
    // 显示规则
    console.log('\n【自动回复规则】');
    AUTO_REPLY_RULES.forEach((r, i) => {
        console.log(`  ${i + 1}. "${r.keywords.slice(0, 3).join('|')}..." → "${r.reply}"`);
    });
    
    // 开始监听
    console.log('\n' + '─'.repeat(50));
    console.log('开始监听消息... (Ctrl+C 退出)');
    console.log('─'.repeat(50) + '\n');
    
    setInterval(async () => {
        try {
            const messages = await getMessagesFromDOM();
            for (const msg of messages) {
                const hash = Buffer.from(msg.content + msg.time).toString('base64').substring(0, 20);
                if (processedMsgIds.has(hash)) continue;
                processedMsgIds.add(hash);
                
                const time = new Date().toLocaleTimeString();
                console.log(`[${time}] 收到: ${msg.content.substring(0, 60)}${msg.content.length > 60 ? '...' : ''}`);
                
                // 匹配规则
                for (const rule of AUTO_REPLY_RULES) {
                    if (rule.keywords.some(k => msg.content === k || msg.content.includes(k))) {
                        console.log(`[${time}] 匹配 → 回复: ${rule.reply}`);
                        const result = await sendMessage(rule.reply);
                        console.log(`[${time}] ${result.success ? '✓ 发送成功' : '✗ 失败: ' + result.error}`);
                        break;
                    }
                }
                
                // 限制集合大小
                if (processedMsgIds.size > CONFIG.maxStoredMsgs * 2) {
                    Array.from(processedMsgIds).slice(0, CONFIG.maxStoredMsgs).forEach(id => processedMsgIds.delete(id));
                }
            }
        } catch (e) {
            // 静默处理错误
        }
    }, CONFIG.pollInterval);
}

main().catch(err => {
    console.error('错误:', err.message);
    process.exit(1);
});
