/**
 * 提取 managestate 中的群成员数据
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
    
    // 提取 managestate 中的群成员数据
    const script = `
(function() {
    var result = {
        success: false,
        members: [],
        error: null
    };
    
    try {
        var state = localStorage.getItem('managestate');
        if (!state) {
            result.error = 'managestate not found';
            return JSON.stringify(result, null, 2);
        }
        
        var data = JSON.parse(state);
        
        // 检查数据结构
        result.topLevelKeys = Object.keys(data);
        
        // 检查 groupList
        if (data.groupList) {
            result.hasGroupList = true;
            result.groupListKeys = Object.keys(data.groupList);
            
            // 获取群列表
            var groups = [];
            if (data.groupList.owner) {
                data.groupList.owner.forEach(function(g) {
                    groups.push({
                        groupId: g.groupId,
                        groupCloudId: g.groupCloudId,
                        groupName: g.groupName,
                        memberNum: g.memberNum
                    });
                });
            }
            if (data.groupList.member) {
                data.groupList.member.forEach(function(g) {
                    groups.push({
                        groupId: g.groupId,
                        groupCloudId: g.groupCloudId,
                        groupName: g.groupName,
                        memberNum: g.memberNum
                    });
                });
            }
            result.groups = groups;
        }
        
        // 检查是否有群成员缓存
        if (data.groupMembersCache) {
            result.hasGroupMembersCache = true;
            result.groupMembersCacheKeys = Object.keys(data.groupMembersCache);
        }
        
        // 检查 helperList
        if (data.helperList) {
            result.hasHelperList = true;
        }
        
        // 检查 friendList
        if (data.friendList) {
            result.hasFriendList = true;
            result.friendListLength = Array.isArray(data.friendList) ? data.friendList.length : Object.keys(data.friendList).length;
            
            // 获取前10个好友
            var friends = Array.isArray(data.friendList) ? data.friendList : Object.values(data.friendList);
            result.sampleFriends = friends.slice(0, 10).map(function(f) {
                return {
                    nimId: f.nimId,
                    uid: f.uid,
                    nickName: f.nickName,
                    remark: f.remark
                };
            });
        }
        
        result.success = true;
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})();`;

    const data = await evaluate(script, false);
    console.log('=== managestate 分析 ===\n');
    console.log(data);
    
    // 提取群成员信息
    console.log('\n=== 提取群成员信息 ===\n');
    
    const memberScript = `
(function() {
    var result = {
        success: false,
        members: [],
        error: null
    };
    
    try {
        // 检查 IndexedDB 中的群成员数据
        // NIM SDK 将群成员存储在 IndexedDB 中
        
        // 先检查 localStorage 中是否有缓存的群成员数据
        for (var i = 0; i < localStorage.length; i++) {
            var key = localStorage.key(i);
            var value = localStorage.getItem(key);
            
            // 查找包含 groupMemberInfo 的数据
            if (value && value.includes('groupMemberInfo')) {
                try {
                    var data = JSON.parse(value);
                    if (data.groupMemberInfo && data.groupMemberInfo.length > 0) {
                        result.foundInKey = key;
                        result.memberCount = data.groupMemberInfo.length;
                        result.members = data.groupMemberInfo.slice(0, 15).map(function(m) {
                            return {
                                accountId: m.accountId,
                                nimId: m.nimId,
                                userNick: m.userNick,
                                groupMemberNick: m.groupMemberNick
                            };
                        });
                        break;
                    }
                } catch(e) {}
            }
            
            // 查找包含昵称的数据
            if (value && value.includes('userNick')) {
                try {
                    var data = JSON.parse(value);
                    if (Array.isArray(data)) {
                        result.foundArrayInKey = key;
                        result.arrayLength = data.length;
                    }
                } catch(e) {}
            }
        }
        
        result.success = true;
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})();`;

    const memberData = await evaluate(memberScript, false);
    console.log(memberData);
    
    ws.close();
}

main().catch(console.error);

