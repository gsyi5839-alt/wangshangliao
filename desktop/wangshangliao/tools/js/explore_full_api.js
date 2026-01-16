/**
 * 完整探索旺商聊所有API和群设置功能
 */
const WebSocket = require('ws');
const http = require('http');
const crypto = require('crypto');

// AES 解密配置
const KEY = Buffer.from('d6ba6647b7c43b79d0e42ceb2790e342', 'utf8');
const IV = Buffer.from('kgWRyiiODMjSCh0m', 'utf8');

function decrypt(ciphertext) {
    try {
        const decipher = crypto.createDecipheriv('aes-256-cbc', KEY, IV);
        let decrypted = decipher.update(ciphertext, 'base64', 'utf8');
        decrypted += decipher.final('utf8');
        return decrypted;
    } catch (e) {
        return null;
    }
}

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
    console.log('=== 旺商聊完整API探索 ===\n');
    
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
    
    // 1. 获取完整群列表信息（包含 groupId 和 groupCloudId 映射）
    console.log('=== 1. 群列表完整信息 ===\n');
    
    const groupScript = `
(function() {
    var result = {
        success: false,
        groups: [],
        error: null
    };
    
    try {
        var state = JSON.parse(localStorage.getItem('managestate') || '{}');
        
        if (state.groupList) {
            var groups = [];
            if (state.groupList.owner) {
                state.groupList.owner.forEach(function(g) {
                    groups.push({
                        groupId: g.groupId,
                        groupCloudId: g.groupCloudId,
                        groupName: g.groupName,
                        memberNum: g.memberNum,
                        avatar: g.groupAvatar ? 'has' : 'none',
                        role: 'owner'
                    });
                });
            }
            if (state.groupList.member) {
                state.groupList.member.forEach(function(g) {
                    groups.push({
                        groupId: g.groupId,
                        groupCloudId: g.groupCloudId,
                        groupName: g.groupName,
                        memberNum: g.memberNum,
                        avatar: g.groupAvatar ? 'has' : 'none',
                        role: 'member'
                    });
                });
            }
            result.groups = groups;
        }
        
        // 获取当前登录用户信息
        if (state.userInfo) {
            result.userInfo = {
                uid: state.userInfo.uid,
                nickName: state.userInfo.nickName,
                nimId: state.userInfo.nimId
            };
        }
        
        result.success = true;
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})();`;

    const groupData = await evaluate(groupScript, false);
    const groups = JSON.parse(groupData);
    console.log('用户信息:', groups.userInfo);
    console.log('\n群列表:');
    groups.groups.forEach(g => {
        console.log(`  ${g.groupName} (${g.role})`);
        console.log(`    groupId: ${g.groupId}`);
        console.log(`    groupCloudId: ${g.groupCloudId}`);
        console.log(`    memberNum: ${g.memberNum}`);
    });
    
    // 2. 获取当前会话信息
    console.log('\n=== 2. 当前会话信息 ===\n');
    
    const sessionScript = `
(function() {
    var result = {
        success: false,
        error: null
    };
    
    try {
        // 从 URL 获取当前会话
        var url = window.location.href;
        var match = url.match(/sessionId=([^&]+)/);
        if (match) {
            result.sessionId = match[1];
        }
        
        // 从 managestate 获取当前会话
        var state = JSON.parse(localStorage.getItem('managestate') || '{}');
        if (state.currSession) {
            result.currSession = {
                id: state.currSession.id,
                scene: state.currSession.scene,
                to: state.currSession.to
            };
            
            // 如果是群聊，获取群信息
            if (state.currSession.group) {
                result.currSession.group = {
                    groupId: state.currSession.group.groupId,
                    groupCloudId: state.currSession.group.groupCloudId,
                    groupName: state.currSession.group.groupName
                };
            }
        }
        
        result.success = true;
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})();`;

    const sessionData = await evaluate(sessionScript, false);
    console.log(sessionData);
    
    // 3. 获取群成员完整信息并解密
    console.log('\n=== 3. 群成员信息（带解密） ===\n');
    
    if (groups.groups.length > 0) {
        const targetGroup = groups.groups[0];
        console.log(`获取群 "${targetGroup.groupName}" 的成员信息...`);
        console.log(`  groupId: ${targetGroup.groupId}`);
        console.log(`  groupCloudId: ${targetGroup.groupCloudId}`);
        
        const memberScript = `
(async function() {
    var result = {
        success: false,
        members: [],
        error: null
    };
    
    try {
        var teamId = '${targetGroup.groupCloudId}';
        
        if (window.nim) {
            var teamData = await new Promise(function(resolve, reject) {
                window.nim.getTeamMembers({
                    teamId: teamId,
                    done: function(err, obj) {
                        if (err) reject(err);
                        else resolve(obj);
                    }
                });
                setTimeout(function() { reject(new Error('timeout')); }, 60000);
            });
            
            var members = teamData.members || teamData || [];
            result.memberCount = members.length;
            
            // 提取前20个成员的完整信息
            result.members = members.slice(0, 20).map(function(m) {
                var customData = null;
                if (m.custom) {
                    try {
                        customData = JSON.parse(m.custom);
                    } catch(e) {}
                }
                
                return {
                    account: m.account,
                    nick: m.nick,
                    nickInTeam: m.nickInTeam,
                    avatar: m.avatar ? 'has' : 'none',
                    type: m.type,
                    joinTime: m.joinTime,
                    custom: customData
                };
            });
        }
        
        result.success = true;
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})();`;

        const memberData = await evaluate(memberScript);
        const members = JSON.parse(memberData);
        console.log(`\n群成员总数: ${members.memberCount}`);
        console.log('\n前20个成员详情:');
        
        members.members.forEach((m, i) => {
            let nickname = m.nick || m.nickInTeam || '(无昵称)';
            
            // 尝试解密
            if (m.custom && m.custom.nicknameCiphertext) {
                const decrypted = decrypt(m.custom.nicknameCiphertext);
                if (decrypted) {
                    nickname = decrypted + ' (解密)';
                }
            }
            
            // 检测是否是MD5
            const isMd5 = /^[a-f0-9]{32}$/i.test(nickname);
            if (isMd5) {
                nickname = nickname.substring(0, 8) + '... (MD5哈希)';
            }
            
            console.log(`  ${i+1}. ${m.account} -> ${nickname}`);
        });
    }
    
    // 4. 探索群设置API
    console.log('\n=== 4. 群设置功能列表 ===\n');
    
    const settingsApis = [
        { name: '全体禁言', api: 'set-group-mute', desc: '开启/关闭群禁言' },
        { name: '成员禁言', api: 'set-member-mute', desc: '禁言指定成员' },
        { name: '修改群名', api: 'set-group-name', desc: '修改群名称' },
        { name: '修改群头像', api: 'set-group-avatar', desc: '修改群头像' },
        { name: '设置群公告', api: 'add-notice', desc: '添加/修改群公告' },
        { name: '入群验证', api: 'set-enter-limit', desc: '设置入群验证方式' },
        { name: '搜索设置', api: 'set-search-mode', desc: '是否允许被搜索' },
        { name: '私聊设置', api: 'set-private-chat', desc: '允许成员私聊' },
        { name: '昵称模式', api: 'set-nickname-mode', desc: '群内昵称显示模式' },
        { name: '移除成员', api: 'remove-group-member', desc: '踢出群成员' },
        { name: '添加管理', api: 'add-group-manage', desc: '添加群管理员' },
        { name: '删除管理', api: 'del-group-manage', desc: '删除群管理员' },
        { name: '转让群主', api: 'group-transfer', desc: '转让群主身份' },
        { name: '解散群聊', api: 'group-dismiss', desc: '解散群聊' }
    ];
    
    settingsApis.forEach(api => {
        console.log(`  ${api.name}: /v1/group/${api.api}`);
        console.log(`    ${api.desc}`);
    });
    
    // 5. 获取群详细设置信息
    console.log('\n=== 5. 群详细设置信息 ===\n');
    
    if (groups.groups.length > 0) {
        const targetGroup = groups.groups[0];
        
        const settingsScript = `
(function() {
    var result = {
        success: false,
        settings: null,
        error: null
    };
    
    try {
        var state = JSON.parse(localStorage.getItem('managestate') || '{}');
        
        // 查找群设置
        var groupId = ${targetGroup.groupId};
        var appSettings = state.appSetting || {};
        
        result.appSettings = {
            groupSetting: appSettings.groupSetting ? {
                groupRemark: appSettings.groupSetting.groupRemark || {}
            } : null
        };
        
        // 查找muteTeams (禁言设置)
        if (state.muteTeams) {
            var cloudId = '${targetGroup.groupCloudId}';
            var muteInfo = state.muteTeams[cloudId];
            if (muteInfo) {
                result.muteSettings = {
                    muteTeam: muteInfo.muteTeam,
                    muteNotiType: muteInfo.muteNotiType
                };
            }
        }
        
        result.success = true;
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})();`;

        const settingsData = await evaluate(settingsScript, false);
        console.log(settingsData);
    }
    
    ws.close();
    console.log('\n=== 探索完成 ===');
}

main().catch(console.error);

