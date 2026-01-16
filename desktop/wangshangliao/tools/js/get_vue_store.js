/**
 * 直接访问 Vue/Pinia store 获取群成员
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
            }, 60000);
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
    
    // 访问 Vue store
    const script = `
(async function() {
    var result = {
        success: false,
        error: null
    };
    
    try {
        // 从 localStorage 获取 managestate 中的群列表
        var state = JSON.parse(localStorage.getItem('managestate') || '{}');
        
        if (state.groupList) {
            var groups = [];
            if (state.groupList.owner) groups = groups.concat(state.groupList.owner);
            if (state.groupList.member) groups = groups.concat(state.groupList.member);
            
            result.groupCount = groups.length;
            result.groups = groups.map(function(g) {
                return {
                    groupId: g.groupId,
                    groupCloudId: g.groupCloudId,
                    groupName: g.groupName,
                    memberNum: g.memberNum
                };
            });
        }
        
        // 获取当前会话的群ID
        var url = window.location.href;
        var match = url.match(/sessionId=team-([0-9]+)/);
        if (match) {
            result.currentTeamId = match[1];
            
            // 尝试获取该群的成员
            // 从 groupMembersMap 获取
            if (state.groupMembersMap) {
                result.hasGroupMembersMap = true;
            }
        }
        
        // 检查 sessionMap 是否有成员信息
        if (state.sessionMap) {
            result.sessionMapKeys = Object.keys(state.sessionMap).slice(0, 10);
        }
        
        // 尝试触发 HTTP 请求获取群成员
        // 旺商聊使用 fetch API 调用后端
        var apiBase = '';
        
        // 查找 API base URL
        if (state.userInfo && state.userInfo.token) {
            result.hasToken = true;
        }
        
        result.success = true;
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})();`;

    const data = await evaluate(script, false);
    console.log('=== Vue Store 数据 ===\n');
    console.log(data);
    
    // 直接从聊天消息中提取发送者昵称
    console.log('\n=== 从消息中提取发送者昵称 ===\n');
    
    const msgScript = `
(async function() {
    var result = {
        success: false,
        nicknames: {},
        error: null
    };
    
    try {
        // 获取当前群聊的 teamId
        var url = window.location.href;
        var match = url.match(/sessionId=team-([0-9]+)/);
        var teamId = match ? match[1] : null;
        
        result.teamId = teamId;
        
        if (teamId && window.nim) {
            // 获取最近的消息
            var msgs = await new Promise(function(resolve, reject) {
                window.nim.getLocalMsgs({
                    sessionId: 'team-' + teamId,
                    limit: 500,
                    done: function(err, obj) {
                        if (err) reject(err);
                        else resolve(obj);
                    }
                });
                setTimeout(function() { reject(new Error('timeout')); }, 30000);
            });
            
            var msgList = msgs.msgs || msgs || [];
            result.msgCount = msgList.length;
            
            // 提取发送者昵称
            var nickMap = {};
            msgList.forEach(function(m) {
                if (m.from && m.fromNick) {
                    // 检查昵称是否是 MD5 (32位十六进制)
                    var isMd5 = /^[a-f0-9]{32}$/i.test(m.fromNick);
                    if (!isMd5) {
                        nickMap[m.from] = m.fromNick;
                    }
                }
            });
            
            result.nicknames = nickMap;
            result.nicknameCount = Object.keys(nickMap).length;
            
            // 显示前20个昵称
            var sample = {};
            var count = 0;
            for (var acc in nickMap) {
                if (count >= 20) break;
                sample[acc] = nickMap[acc];
                count++;
            }
            result.sampleNicknames = sample;
        }
        
        result.success = true;
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})();`;

    const msgData = await evaluate(msgScript);
    console.log(msgData);
    
    ws.close();
}

main().catch(console.error);

