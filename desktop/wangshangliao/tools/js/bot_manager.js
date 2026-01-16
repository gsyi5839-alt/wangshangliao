/**
 * 旺商聊机器人管理器
 * 功能：
 * 1. 获取群列表，让用户选择绑定群
 * 2. 自动监听绑定群消息
 * 3. 自动回复
 * 4. 禁言/解禁功能
 */
const http = require('http');
const WebSocket = require('ws');
const readline = require('readline');

// ====================== 配置 ======================
const CONFIG = {
    cdpPort: 9222,
    pollInterval: 1000,  // 消息轮询间隔
    maxStoredMsgs: 100,  // 最大存储消息数
};

// 自动回复规则 (从 ZCG 配置提取)
const AUTO_REPLY_RULES = [
    // 财付通
    { keywords: ['财富', '发财富', '财付通', 'cf', 'CF', '财付'], reply: '私聊前排接单', type: 'group' },
    // 支付宝
    { keywords: ['支付', '支付宝', 'zf', 'ZF'], reply: '私聊前排接单', type: 'group' },
    // 微信
    { keywords: ['微信', '发微信', '微信号'], reply: '私聊前排接单', type: 'group' },
    // 查余粮
    { keywords: ['1'], reply: null, action: 'query_balance', type: 'group' },
    // 查历史
    { keywords: ['2', '历史', '发历史', '开奖历史'], reply: null, action: 'query_history', type: 'group' },
    // 查数据
    { keywords: ['3', '账单', '数据'], reply: null, action: 'query_data', type: 'group' },
];

// ====================== 全局状态 ======================
let ws = null;
let cmdId = 0;
let pendingCommands = new Map();
let boundGroupId = null;
let boundGroupName = null;
let processedMsgIds = new Set();
let isRunning = false;
let userInfo = null;
let groupList = [];

// ====================== CDP 通信 ======================
async function connectCDP() {
    console.log('正在连接 CDP...');
    
    const pages = await new Promise((resolve, reject) => {
        http.get(`http://127.0.0.1:${CONFIG.cdpPort}/json`, (res) => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', () => resolve(JSON.parse(data)));
        }).on('error', reject);
    });
    
    const wslPage = pages.find(p => p.title && p.title.includes('旺商聊'));
    if (!wslPage) throw new Error('未找到旺商聊页面，请确保旺商聊客户端已打开');
    
    ws = new WebSocket(wslPage.webSocketDebuggerUrl);
    
    await new Promise((resolve, reject) => {
        ws.on('open', resolve);
        ws.on('error', reject);
    });
    
    ws.on('message', (data) => {
        const msg = JSON.parse(data.toString());
        if (msg.id && pendingCommands.has(msg.id)) {
            const { resolve } = pendingCommands.get(msg.id);
            pendingCommands.delete(msg.id);
            resolve(msg.result);
        }
    });
    
    console.log('✓ CDP 已连接\n');
}

function sendCommand(method, params = {}) {
    return new Promise((resolve, reject) => {
        const id = ++cmdId;
        pendingCommands.set(id, { resolve, reject });
        ws.send(JSON.stringify({ id, method, params }));
        setTimeout(() => {
            if (pendingCommands.has(id)) {
                pendingCommands.delete(id);
                reject(new Error('CDP 命令超时'));
            }
        }, 15000);
    });
}

async function evalJS(js) {
    const result = await sendCommand('Runtime.evaluate', { 
        expression: js, 
        returnByValue: true,
        awaitPromise: true
    });
    return result?.result?.value;
}

// ====================== 旺商聊操作 ======================

// 获取用户信息
async function getUserInfo() {
    const js = `(function() {
        var ms = JSON.parse(localStorage.getItem('managestate') || '{}');
        return JSON.stringify({
            nickName: ms.userInfo ? ms.userInfo.nickName : '',
            nimId: ms.userInfo ? ms.userInfo.nimId : '',
            wwid: ms.userInfo ? ms.userInfo.wwid : ''
        });
    })()`;
    return JSON.parse(await evalJS(js));
}

// 获取群列表
async function getGroupList() {
    const js = `(function() {
        var ms = JSON.parse(localStorage.getItem('managestate') || '{}');
        var groups = [];
        if (ms.groupList) {
            if (ms.groupList.owner) {
                ms.groupList.owner.forEach(function(g) {
                    groups.push({
                        name: g.name || g.groupName,
                        account: g.groupAccount,
                        cloudId: g.groupCloudId,
                        memberNum: g.groupMemberNum || 0,
                        type: 'owner'
                    });
                });
            }
            if (ms.groupList.member) {
                ms.groupList.member.forEach(function(g) {
                    groups.push({
                        name: g.name || g.groupName,
                        account: g.groupAccount,
                        cloudId: g.groupCloudId,
                        memberNum: g.groupMemberNum || 0,
                        type: 'member'
                    });
                });
            }
        }
        return JSON.stringify(groups);
    })()`;
    return JSON.parse(await evalJS(js));
}

// 获取当前会话
async function getCurrentSession() {
    const js = `(function() {
        var ms = JSON.parse(localStorage.getItem('managestate') || '{}');
        if (ms.currSession && ms.currSession.group) {
            return JSON.stringify({
                name: ms.currSession.group.name,
                account: ms.currSession.group.groupAccount,
                cloudId: ms.currSession.to
            });
        }
        return 'null';
    })()`;
    const result = await evalJS(js);
    return result === 'null' ? null : JSON.parse(result);
}

// 切换到指定群
async function switchToGroup(groupCloudId) {
    const js = `(async function() {
        // 查找群聊元素并点击
        var groupItems = document.querySelectorAll('[class*="group-item"]');
        var found = false;
        
        // 方法1: 通过群列表点击
        groupItems.forEach(function(item) {
            if (item.textContent.includes('${groupCloudId}')) {
                item.click();
                found = true;
            }
        });
        
        if (!found) {
            // 方法2: 通过会话列表
            var sessionItems = document.querySelectorAll('[class*="session-item"]');
            sessionItems.forEach(function(item) {
                var text = item.textContent || '';
                if (text.includes('${groupCloudId}')) {
                    item.click();
                    found = true;
                }
            });
        }
        
        return found;
    })()`;
    return await evalJS(js);
}

// 从 DOM 获取消息
async function getMessagesFromDOM() {
    const js = `(function() {
        var messages = [];
        var msgItems = document.querySelectorAll('.msg-item');
        
        msgItems.forEach(function(item, idx) {
            var isIncoming = item.classList.contains('user-msg');
            if (!isIncoming) return;  // 只处理收到的消息
            
            var contentEl = item.querySelector('[class*="msg-content"]') || item.querySelector('[class*="content"]');
            var content = contentEl ? contentEl.textContent.trim() : '';
            
            var timeEl = item.querySelector('[class*="time"]');
            var time = timeEl ? timeEl.textContent.trim() : '';
            
            if (time && content.endsWith(time)) {
                content = content.slice(0, -time.length).trim();
            }
            
            if (content) {
                messages.push({
                    index: idx,
                    content: content,
                    time: time
                });
            }
        });
        
        return JSON.stringify(messages);
    })()`;
    return JSON.parse(await evalJS(js));
}

// 发送消息
async function sendMessage(text) {
    const escapedText = text.replace(/'/g, "\\'").replace(/\n/g, '\\n');
    const js = `(async function() {
        var editor = document.querySelector('#con_edit');
        if (!editor) return { success: false, error: 'no editor' };
        
        editor.focus();
        editor.innerHTML = '';
        document.execCommand('insertText', false, '${escapedText}');
        
        await new Promise(r => setTimeout(r, 100));
        
        var buttons = document.querySelectorAll('button');
        var sendBtn = null;
        buttons.forEach(function(btn) {
            if (btn.className.includes('blue-color')) sendBtn = btn;
        });
        
        if (!sendBtn) return { success: false, error: 'no send button' };
        
        sendBtn.click();
        return { success: true };
    })()`;
    
    return JSON.parse(await evalJS(js));
}

// 禁言用户
async function muteUser(userId, duration = 60) {
    // TODO: 实现禁言功能
    console.log(`[禁言] 用户 ${userId}, 时长 ${duration} 秒`);
    return { success: true, message: '禁言功能待实现' };
}

// 解除禁言
async function unmuteUser(userId) {
    // TODO: 实现解禁功能
    console.log(`[解禁] 用户 ${userId}`);
    return { success: true, message: '解禁功能待实现' };
}

// ====================== 自动回复逻辑 ======================

function matchAutoReply(content) {
    for (const rule of AUTO_REPLY_RULES) {
        for (const keyword of rule.keywords) {
            if (content === keyword || content.includes(keyword)) {
                return rule;
            }
        }
    }
    return null;
}

async function processNewMessage(msg) {
    const time = new Date().toLocaleTimeString();
    console.log(`[${time}] 收到: ${msg.content.substring(0, 50)}${msg.content.length > 50 ? '...' : ''}`);
    
    const rule = matchAutoReply(msg.content);
    if (!rule) return;
    
    if (rule.reply) {
        console.log(`[${time}] 匹配关键词，回复: ${rule.reply}`);
        const result = await sendMessage(rule.reply);
        if (result.success) {
            console.log(`[${time}] ✓ 回复成功`);
        } else {
            console.log(`[${time}] ✗ 回复失败: ${result.error}`);
        }
    } else if (rule.action) {
        console.log(`[${time}] 触发动作: ${rule.action}`);
        // TODO: 实现各种动作
    }
}

// ====================== 消息轮询 ======================

async function pollMessages() {
    if (!isRunning) return;
    
    try {
        const messages = await getMessagesFromDOM();
        
        for (const msg of messages) {
            const msgHash = Buffer.from(msg.content + msg.time).toString('base64').substring(0, 20);
            
            if (!processedMsgIds.has(msgHash)) {
                processedMsgIds.add(msgHash);
                await processNewMessage(msg);
                
                if (processedMsgIds.size > CONFIG.maxStoredMsgs * 2) {
                    const arr = Array.from(processedMsgIds);
                    arr.slice(0, CONFIG.maxStoredMsgs).forEach(id => processedMsgIds.delete(id));
                }
            }
        }
    } catch (err) {
        console.error('轮询错误:', err.message);
    }
}

// ====================== 用户界面 ======================

const rl = readline.createInterface({
    input: process.stdin,
    output: process.stdout
});

function prompt(question) {
    return new Promise(resolve => rl.question(question, resolve));
}

async function showMenu() {
    console.log('\n' + '═'.repeat(50));
    console.log('         旺商聊机器人管理器');
    console.log('═'.repeat(50));
    console.log(`当前用户: ${userInfo?.nickName || '未知'}`);
    console.log(`绑定群聊: ${boundGroupName || '未绑定'} (${boundGroupId || '-'})`);
    console.log(`运行状态: ${isRunning ? '✓ 运行中' : '○ 已停止'}`);
    console.log('─'.repeat(50));
    console.log('1. 选择绑定群');
    console.log('2. 开始监听');
    console.log('3. 停止监听');
    console.log('4. 发送测试消息');
    console.log('5. 查看自动回复规则');
    console.log('6. 禁言用户');
    console.log('7. 解除禁言');
    console.log('0. 退出');
    console.log('─'.repeat(50));
}

async function selectGroup() {
    console.log('\n正在获取群列表...');
    groupList = await getGroupList();
    
    if (groupList.length === 0) {
        console.log('未找到任何群聊');
        return;
    }
    
    console.log('\n【群聊列表】');
    groupList.forEach((g, i) => {
        const typeStr = g.type === 'owner' ? '[群主]' : '[成员]';
        console.log(`  ${i + 1}. ${g.name} ${typeStr}`);
        console.log(`     群号: ${g.account}, 人数: ${g.memberNum}`);
    });
    
    const choice = await prompt('\n请输入群序号 (0 取消): ');
    const idx = parseInt(choice) - 1;
    
    if (idx >= 0 && idx < groupList.length) {
        const group = groupList[idx];
        boundGroupId = group.account;
        boundGroupName = group.name;
        
        console.log(`\n✓ 已绑定群: ${boundGroupName} (${boundGroupId})`);
        
        // 尝试切换到该群
        console.log('正在切换到该群...');
        await switchToGroup(group.cloudId);
        
        // 检查当前会话
        const current = await getCurrentSession();
        if (current && current.account === boundGroupId) {
            console.log('✓ 已切换到该群');
        } else {
            console.log('⚠ 请在旺商聊中手动切换到该群');
        }
    }
}

async function startListening() {
    if (!boundGroupId) {
        console.log('请先绑定群聊！');
        return;
    }
    
    if (isRunning) {
        console.log('已经在运行中');
        return;
    }
    
    // 检查当前会话
    const current = await getCurrentSession();
    if (!current || current.account !== boundGroupId) {
        console.log(`⚠ 当前会话不是绑定的群 (${boundGroupName})`);
        console.log('  请在旺商聊中切换到该群，或按回车继续');
        await prompt('');
    }
    
    // 标记现有消息为已处理
    const existingMsgs = await getMessagesFromDOM();
    existingMsgs.forEach(msg => {
        const msgHash = Buffer.from(msg.content + msg.time).toString('base64').substring(0, 20);
        processedMsgIds.add(msgHash);
    });
    
    isRunning = true;
    console.log(`\n✓ 开始监听群: ${boundGroupName}`);
    console.log(`  已标记 ${existingMsgs.length} 条现有消息`);
    console.log('  按任意键返回菜单...\n');
    
    // 开始轮询
    const pollTimer = setInterval(pollMessages, CONFIG.pollInterval);
    
    // 等待用户按键
    await prompt('');
    
    clearInterval(pollTimer);
    console.log('已暂停轮询，返回菜单');
}

async function stopListening() {
    isRunning = false;
    console.log('✓ 已停止监听');
}

async function sendTestMessage() {
    const msg = await prompt('请输入测试消息: ');
    if (msg.trim()) {
        const result = await sendMessage(msg);
        if (result.success) {
            console.log('✓ 发送成功');
        } else {
            console.log(`✗ 发送失败: ${result.error}`);
        }
    }
}

function showRules() {
    console.log('\n【自动回复规则】');
    AUTO_REPLY_RULES.forEach((rule, i) => {
        const keywords = rule.keywords.slice(0, 5).join('|');
        const action = rule.reply || `[动作: ${rule.action}]`;
        console.log(`  ${i + 1}. "${keywords}..." → ${action}`);
    });
}

async function muteUserMenu() {
    const userId = await prompt('请输入用户ID: ');
    const duration = await prompt('禁言时长(秒, 默认60): ');
    const result = await muteUser(userId, parseInt(duration) || 60);
    console.log(result.message);
}

async function unmuteUserMenu() {
    const userId = await prompt('请输入用户ID: ');
    const result = await unmuteUser(userId);
    console.log(result.message);
}

// ====================== 主程序 ======================

async function main() {
    try {
        await connectCDP();
        
        userInfo = await getUserInfo();
        console.log(`当前用户: ${userInfo.nickName} (NIM: ${userInfo.nimId})`);
        
        while (true) {
            await showMenu();
            const choice = await prompt('请选择: ');
            
            switch (choice.trim()) {
                case '1': await selectGroup(); break;
                case '2': await startListening(); break;
                case '3': await stopListening(); break;
                case '4': await sendTestMessage(); break;
                case '5': showRules(); break;
                case '6': await muteUserMenu(); break;
                case '7': await unmuteUserMenu(); break;
                case '0':
                case 'q':
                case 'exit':
                    console.log('再见！');
                    rl.close();
                    process.exit(0);
                default:
                    console.log('无效选择');
            }
        }
    } catch (err) {
        console.error('错误:', err.message);
        process.exit(1);
    }
}

main();
