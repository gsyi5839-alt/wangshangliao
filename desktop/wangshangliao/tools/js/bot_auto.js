/**
 * 旺商聊机器人 - 自动读取副框架配置
 * 自动从副框架的 accounts.json 读取绑定群号
 */
const http = require('http');
const WebSocket = require('ws');
const fs = require('fs');
const path = require('path');

// ====================== 配置路径 ======================
const CONFIG_PATHS = [
    // Release 版
    path.join(__dirname, 'src/WSLFramework/bin/Release/zcg/accounts.json'),
    // Debug 版
    path.join(__dirname, 'src/WSLFramework/bin/Debug/zcg/accounts.json'),
    // 根目录
    path.join(__dirname, 'zcg/accounts.json'),
];

const CDP_PORT = 9222;
const POLL_INTERVAL = 1000;

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
let boundGroupId = null;
let boundGroupAccount = null;

// ====================== 读取副框架配置 ======================
function loadFrameworkConfig() {
    console.log('正在读取副框架配置...\n');
    
    for (const configPath of CONFIG_PATHS) {
        if (!fs.existsSync(configPath)) continue;
        
        console.log(`找到配置: ${configPath}`);
        try {
            let json = fs.readFileSync(configPath, 'utf-8');
            // 去除 UTF-8 BOM
            if (json.charCodeAt(0) === 0xFEFF) {
                json = json.substring(1);
            }
            const data = JSON.parse(json);
            
            // 格式1: { Accounts: [...] }
            if (data.Accounts && Array.isArray(data.Accounts) && data.Accounts.length > 0) {
                const account = data.Accounts[0];
                if (account.GroupId) {
                    console.log(`\n【副框架账号配置】`);
                    console.log(`  账号: ${account.Account}`);
                    console.log(`  昵称: ${account.Nickname || account.BotName}`);
                    console.log(`  绑定群号: ${account.GroupId}`);
                    console.log(`  NIM ID: ${account.NimAccid || '-'}`);
                    console.log(`  状态: ${account.LoginStatus || '-'}`);
                    return account;
                }
            }
            
            // 格式2: [{ ... }]
            if (Array.isArray(data) && data.length > 0) {
                const account = data[0];
                if (account.GroupId) {
                    console.log(`\n【副框架账号配置】`);
                    console.log(`  账号: ${account.Account}`);
                    console.log(`  昵称: ${account.Nickname}`);
                    console.log(`  绑定群号: ${account.GroupId}`);
                    return account;
                }
            }
        } catch (e) {
            console.log(`  解析失败: ${e.message}`);
        }
    }
    
    console.log('未找到有效的副框架配置');
    return null;
}

// ====================== CDP 通信 ======================
async function connectCDP() {
    const pages = await new Promise((resolve, reject) => {
        http.get(`http://127.0.0.1:${CDP_PORT}/json`, (res) => {
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
                        groups.push({ name: g.name || g.groupName, account: g.groupAccount, cloudId: g.groupCloudId });
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
        document.querySelectorAll('.msg-item.user-msg').forEach(function(item) {
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
    console.log('═'.repeat(55));
    console.log('     旺商聊机器人 - 自动读取副框架配置');
    console.log('═'.repeat(55) + '\n');
    
    // 1. 读取副框架配置
    const frameworkConfig = loadFrameworkConfig();
    if (!frameworkConfig || !frameworkConfig.GroupId) {
        console.log('\n✗ 未找到副框架配置或绑定群号为空');
        console.log('  请在副框架中设置绑定群号后重试');
        process.exit(1);
    }
    
    boundGroupId = frameworkConfig.GroupId;
    boundGroupAccount = frameworkConfig.Account;
    
    console.log(`\n✓ 读取到绑定群号: ${boundGroupId}`);
    
    // 2. 连接 CDP
    console.log('\n正在连接 CDP...');
    await connectCDP();
    console.log('✓ CDP 已连接');
    
    // 3. 获取群列表并验证
    const groups = await getGroupList();
    console.log(`\n找到 ${groups.length} 个群聊:`);
    
    let targetGroup = null;
    groups.forEach((g, i) => {
        const isTarget = g.account === boundGroupId;
        console.log(`  ${i + 1}. ${g.name} (${g.account})${isTarget ? ' ← 绑定群' : ''}`);
        if (isTarget) targetGroup = g;
    });
    
    if (!targetGroup) {
        console.log(`\n✗ 未找到绑定群号 ${boundGroupId} 对应的群`);
        console.log('  请检查副框架配置是否正确');
        process.exit(1);
    }
    
    console.log(`\n✓ 目标群: ${targetGroup.name} (${targetGroup.account})`);
    
    // 4. 检查当前会话
    const current = await getCurrentSession();
    if (!current || current.account !== boundGroupId) {
        console.log(`\n⚠ 当前会话不是绑定群 [${targetGroup.name}]`);
        console.log(`  请在旺商聊中切换到该群，然后重新运行此脚本`);
        process.exit(0);
    }
    
    console.log('✓ 当前会话已是目标群');
    
    // 5. 标记现有消息
    const existingMsgs = await getMessagesFromDOM();
    existingMsgs.forEach(msg => {
        processedMsgIds.add(Buffer.from(msg.content + msg.time).toString('base64').substring(0, 20));
    });
    console.log(`✓ 已标记 ${existingMsgs.length} 条现有消息`);
    
    // 6. 显示规则
    console.log('\n【自动回复规则】');
    AUTO_REPLY_RULES.forEach((r, i) => {
        console.log(`  ${i + 1}. "${r.keywords.slice(0, 3).join('|')}..." → "${r.reply}"`);
    });
    
    // 7. 开始监听
    console.log('\n' + '─'.repeat(55));
    console.log(`开始监听群 [${targetGroup.name}] 的消息...`);
    console.log('─'.repeat(55) + '\n');
    
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
                if (processedMsgIds.size > 200) {
                    Array.from(processedMsgIds).slice(0, 100).forEach(id => processedMsgIds.delete(id));
                }
            }
        } catch (e) {
            // 静默处理
        }
    }, POLL_INTERVAL);
}

main().catch(err => {
    console.error('错误:', err.message);
    process.exit(1);
});
