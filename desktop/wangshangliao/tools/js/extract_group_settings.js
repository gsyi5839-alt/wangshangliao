// 全面提取旺商聊群聊设置相关API
const WebSocket = require('ws');
const http = require('http');
const fs = require('fs');

async function getDebuggerUrl() {
    return new Promise((resolve, reject) => {
        http.get('http://127.0.0.1:9222/json', (res) => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', () => {
                const pages = JSON.parse(data);
                const mainPage = pages.find(p => p.url.includes('index.html'));
                if (mainPage) {
                    resolve(mainPage.webSocketDebuggerUrl);
                } else {
                    reject(new Error('未找到旺商聊主页面'));
                }
            });
        }).on('error', reject);
    });
}

async function extractGroupSettings() {
    const cdpUrl = await getDebuggerUrl();
    console.log('CDP URL:', cdpUrl);
    
    return new Promise((resolve, reject) => {
        const ws = new WebSocket(cdpUrl);
        let messageId = 1;
        const allResults = {};

        ws.on('open', () => {
            console.log('✅ 连接成功\n');

            // 1. 提取所有Team相关的NIM API
            const code1 = `
(function() {
    const result = {
        teamAPIs: [],
        teamOptions: [],
        teamEvents: [],
        teamConstants: {}
    };
    
    if (window.nim) {
        // 收集所有Team相关方法
        for (let key in window.nim) {
            if (typeof window.nim[key] === 'function') {
                const keyLower = key.toLowerCase();
                if (keyLower.includes('team') || 
                    keyLower.includes('mute') ||
                    keyLower.includes('manager') ||
                    keyLower.includes('member') ||
                    keyLower.includes('group')) {
                    try {
                        const fnStr = window.nim[key].toString();
                        result.teamAPIs.push({
                            name: key,
                            params: fnStr.match(/^function\\s*\\w*\\s*\\(([^)]*)\\)/) ? 
                                   fnStr.match(/^function\\s*\\w*\\s*\\(([^)]*)\\)/)[1] : '',
                            isAsync: fnStr.includes('async') || fnStr.includes('Promise'),
                            preview: fnStr.substring(0, 300)
                        });
                    } catch(e) {
                        result.teamAPIs.push({ name: key, error: e.message });
                    }
                }
            }
        }
        
        // 收集所有Team相关事件
        if (window.nim.options) {
            for (let key in window.nim.options) {
                const keyLower = key.toLowerCase();
                if (keyLower.includes('team') || 
                    keyLower.includes('member') ||
                    keyLower.includes('mute') ||
                    keyLower.includes('manager')) {
                    result.teamOptions.push({
                        name: key,
                        type: typeof window.nim.options[key]
                    });
                }
            }
        }
    }
    
    return JSON.stringify(result, null, 2);
})()
            `;

            ws.send(JSON.stringify({
                id: messageId++,
                method: 'Runtime.evaluate',
                params: { expression: code1, returnByValue: true }
            }));
        });

        ws.on('message', (data) => {
            const response = JSON.parse(data.toString());
            
            if (response.id === 1) {
                if (response.result && response.result.result) {
                    console.log('📋 Team/群组相关API:\n');
                    const result = JSON.parse(response.result.result.value);
                    allResults.teamAPIs = result;
                    console.log(JSON.stringify(result, null, 2));
                }

                // 2. 提取群信息结构和群成员结构
                const code2 = `
(function() {
    const result = {
        teamStructure: {},
        memberStructure: {},
        teamTypes: [],
        memberTypes: [],
        muteTypes: []
    };
    
    // 尝试获取当前群信息
    if (window.nim) {
        // 获取第一个群的信息结构
        window.nim.getTeams({
            done: function(err, obj) {
                if (!err && obj && obj.teams && obj.teams.length > 0) {
                    const team = obj.teams[0];
                    result.teamStructure = {
                        fields: Object.keys(team),
                        sample: {}
                    };
                    // 只获取结构，不获取敏感值
                    for (let key in team) {
                        result.teamStructure.sample[key] = typeof team[key];
                    }
                }
            }
        });
    }
    
    // 群类型
    result.teamTypes = [
        { type: 'normal', desc: '普通群' },
        { type: 'advanced', desc: '高级群' }
    ];
    
    // 成员类型
    result.memberTypes = [
        { type: 'owner', desc: '群主' },
        { type: 'manager', desc: '管理员' },
        { type: 'normal', desc: '普通成员' }
    ];
    
    // 禁言类型
    result.muteTypes = [
        { type: 'none', desc: '不禁言' },
        { type: 'normal', desc: '禁言普通成员' },
        { type: 'all', desc: '全员禁言' }
    ];
    
    return JSON.stringify(result, null, 2);
})()
                `;

                ws.send(JSON.stringify({
                    id: messageId++,
                    method: 'Runtime.evaluate',
                    params: { expression: code2, returnByValue: true }
                }));
            }

            if (response.id === 2) {
                if (response.result && response.result.result) {
                    console.log('\n\n📋 群结构信息:\n');
                    const result = JSON.parse(response.result.result.value);
                    allResults.teamStructure = result;
                    console.log(JSON.stringify(result, null, 2));
                }

                // 3. 提取详细的群操作API参数
                const code3 = `
(function() {
    const result = {
        updateTeamParams: {},
        muteParams: {},
        memberOperations: {},
        inviteParams: {},
        applyParams: {}
    };
    
    // updateTeam 可更新的字段
    result.updateTeamParams = {
        name: '群名称 (string)',
        avatar: '群头像URL (string)',
        intro: '群简介 (string)',
        announcement: '群公告 (string)',
        custom: '自定义扩展字段 (string/JSON)',
        joinMode: '加群方式: noVerify(无需验证)/needVerify(需要验证)/rejectAll(拒绝所有)',
        beInviteMode: '被邀请方式: needVerify(需要验证)/noVerify(无需验证)',
        inviteMode: '邀请方式: manager(管理员)/all(所有人)',
        updateTeamMode: '更新群信息方式: manager(管理员)/all(所有人)',
        updateCustomMode: '更新自定义字段方式: manager(管理员)/all(所有人)'
    };
    
    // muteTeamAll 参数
    result.muteParams = {
        teamId: '群ID (string)',
        mute: '是否禁言 (boolean): true=禁言, false=解禁',
        done: '回调函数 (function)'
    };
    
    // 成员操作
    result.memberOperations = {
        addTeamMembers: {
            params: {
                teamId: '群ID',
                accounts: '账号数组',
                ps: '附言',
                done: '回调'
            }
        },
        removeTeamMembers: {
            params: {
                teamId: '群ID',
                accounts: '要移除的账号数组',
                done: '回调'
            }
        },
        updateMuteStateInTeam: {
            params: {
                teamId: '群ID',
                account: '要禁言的账号',
                mute: '是否禁言',
                done: '回调'
            }
        },
        addTeamManagers: {
            params: {
                teamId: '群ID',
                accounts: '要设为管理员的账号数组',
                done: '回调'
            }
        },
        removeTeamManagers: {
            params: {
                teamId: '群ID',
                accounts: '要取消管理员的账号数组',
                done: '回调'
            }
        },
        updateNickInTeam: {
            params: {
                teamId: '群ID',
                nick: '新昵称',
                done: '回调'
            }
        },
        transferTeam: {
            params: {
                teamId: '群ID',
                account: '新群主账号',
                leave: '是否离开群(boolean)',
                done: '回调'
            }
        }
    };
    
    // 邀请参数
    result.inviteParams = {
        acceptTeamInvite: {
            params: {
                teamId: '群ID',
                idServer: '邀请消息服务器ID',
                from: '邀请人账号',
                done: '回调'
            }
        },
        rejectTeamInvite: {
            params: {
                teamId: '群ID',
                idServer: '邀请消息服务器ID',
                from: '邀请人账号',
                ps: '拒绝理由',
                done: '回调'
            }
        }
    };
    
    // 申请参数
    result.applyParams = {
        applyTeam: {
            params: {
                teamId: '群ID',
                ps: '申请理由',
                done: '回调'
            }
        },
        passTeamApply: {
            params: {
                teamId: '群ID',
                idServer: '申请消息服务器ID',
                from: '申请人账号',
                done: '回调'
            }
        },
        rejectTeamApply: {
            params: {
                teamId: '群ID',
                idServer: '申请消息服务器ID',
                from: '申请人账号',
                ps: '拒绝理由',
                done: '回调'
            }
        }
    };
    
    return JSON.stringify(result, null, 2);
})()
                `;

                ws.send(JSON.stringify({
                    id: messageId++,
                    method: 'Runtime.evaluate',
                    params: { expression: code3, returnByValue: true }
                }));
            }

            if (response.id === 3) {
                if (response.result && response.result.result) {
                    console.log('\n\n📋 群操作详细参数:\n');
                    const result = JSON.parse(response.result.result.value);
                    allResults.groupOperations = result;
                    console.log(JSON.stringify(result, null, 2));
                }

                // 4. 提取系统消息类型和群通知类型
                const code4 = `
(function() {
    const result = {
        sysMsgTypes: [
            { type: 'teamInvite', desc: '群邀请', category: 'team' },
            { type: 'rejectTeamInvite', desc: '拒绝群邀请', category: 'team' },
            { type: 'applyTeam', desc: '申请加群', category: 'team' },
            { type: 'rejectTeamApply', desc: '拒绝加群申请', category: 'team' },
            { type: 'passTeamApply', desc: '通过加群申请', category: 'team' },
            { type: 'addTeamMembers', desc: '添加群成员', category: 'team' },
            { type: 'removeTeamMembers', desc: '移除群成员', category: 'team' },
            { type: 'acceptTeamInvite', desc: '接受群邀请', category: 'team' },
            { type: 'leaveTeam', desc: '退出群', category: 'team' },
            { type: 'dismissTeam', desc: '解散群', category: 'team' },
            { type: 'transferTeam', desc: '转让群主', category: 'team' },
            { type: 'updateTeam', desc: '更新群信息', category: 'team' },
            { type: 'muteTeam', desc: '群禁言变更', category: 'team' },
            { type: 'addTeamManagers', desc: '添加群管理员', category: 'team' },
            { type: 'removeTeamManagers', desc: '移除群管理员', category: 'team' },
            { type: 'friendAdd', desc: '添加好友', category: 'friend' },
            { type: 'friendApply', desc: '好友申请', category: 'friend' },
            { type: 'friendPass', desc: '通过好友申请', category: 'friend' },
            { type: 'friendReject', desc: '拒绝好友申请', category: 'friend' },
            { type: 'friendDelete', desc: '删除好友', category: 'friend' }
        ],
        teamNotificationTypes: [
            { type: 'updateTeam', desc: '更新群信息' },
            { type: 'addTeamMembers', desc: '添加群成员' },
            { type: 'removeTeamMembers', desc: '移除群成员' },
            { type: 'acceptTeamInvite', desc: '接受群邀请' },
            { type: 'passTeamApply', desc: '通过加群申请' },
            { type: 'addTeamManagers', desc: '添加管理员' },
            { type: 'removeTeamManagers', desc: '移除管理员' },
            { type: 'leaveTeam', desc: '退出群' },
            { type: 'dismissTeam', desc: '解散群' },
            { type: 'transferTeam', desc: '转让群主' },
            { type: 'muteTeamMember', desc: '禁言成员' },
            { type: 'unmuteTeamMember', desc: '解除禁言' },
            { type: 'muteTeam', desc: '全员禁言' },
            { type: 'unmuteTeam', desc: '解除全员禁言' }
        ],
        customMsgTypes: [
            { type: 'recall', desc: '撤回消息' },
            { type: 'tip', desc: '提示消息' },
            { type: 'at', desc: '@消息' },
            { type: 'reply', desc: '回复消息' }
        ]
    };
    
    return JSON.stringify(result, null, 2);
})()
                `;

                ws.send(JSON.stringify({
                    id: messageId++,
                    method: 'Runtime.evaluate',
                    params: { expression: code4, returnByValue: true }
                }));
            }

            if (response.id === 4) {
                if (response.result && response.result.result) {
                    console.log('\n\n📋 系统消息和通知类型:\n');
                    const result = JSON.parse(response.result.result.value);
                    allResults.messageTypes = result;
                    console.log(JSON.stringify(result, null, 2));
                }

                // 5. 提取Pinia stores中群相关的方法
                const code5 = `
(function() {
    const result = {
        stores: {},
        globalMethods: []
    };
    
    // 查找全局对象中群相关的方法
    const keywords = ['team', 'group', 'member', 'mute', 'manager', 'invite', 'apply', 'kick', 'ban'];
    
    for (let key of Object.keys(window)) {
        try {
            if (typeof window[key] === 'function') {
                const keyLower = key.toLowerCase();
                if (keywords.some(kw => keyLower.includes(kw))) {
                    result.globalMethods.push({
                        name: key,
                        type: 'function'
                    });
                }
            } else if (typeof window[key] === 'object' && window[key] !== null) {
                for (let prop in window[key]) {
                    try {
                        if (typeof window[key][prop] === 'function') {
                            const propLower = prop.toLowerCase();
                            if (keywords.some(kw => propLower.includes(kw))) {
                                result.globalMethods.push({
                                    name: key + '.' + prop,
                                    type: 'function'
                                });
                            }
                        }
                    } catch(e) {}
                }
            }
        } catch(e) {}
    }
    
    return JSON.stringify(result, null, 2);
})()
                `;

                ws.send(JSON.stringify({
                    id: messageId++,
                    method: 'Runtime.evaluate',
                    params: { expression: code5, returnByValue: true }
                }));
            }

            if (response.id === 5) {
                if (response.result && response.result.result) {
                    console.log('\n\n📋 全局群相关方法:\n');
                    const result = JSON.parse(response.result.result.value);
                    allResults.globalMethods = result;
                    console.log(JSON.stringify(result, null, 2));
                }

                // 6. 获取解密相关函数
                const code6 = `
(function() {
    const result = {
        decryptFunctions: [],
        cryptoObjects: [],
        keyPatterns: []
    };
    
    // 搜索所有包含decrypt/encrypt/AES/key的函数和对象
    const decryptKeywords = ['decrypt', 'encrypt', 'aes', 'cipher', 'crypto', 'key', 'iv'];
    
    for (let key of Object.keys(window)) {
        try {
            const keyLower = key.toLowerCase();
            if (decryptKeywords.some(kw => keyLower.includes(kw))) {
                const val = window[key];
                if (typeof val === 'function') {
                    result.decryptFunctions.push({
                        name: key,
                        type: 'function',
                        preview: val.toString().substring(0, 500)
                    });
                } else if (typeof val === 'object' && val !== null) {
                    result.cryptoObjects.push({
                        name: key,
                        type: 'object',
                        methods: Object.keys(val).filter(k => typeof val[k] === 'function')
                    });
                }
            }
        } catch(e) {}
    }
    
    // 查找字符串中的密钥模式
    const codeStr = document.body.innerHTML;
    const keyMatches = codeStr.match(/[a-f0-9]{32}/gi);
    if (keyMatches) {
        result.keyPatterns = [...new Set(keyMatches)].slice(0, 10);
    }
    
    return JSON.stringify(result, null, 2);
})()
                `;

                ws.send(JSON.stringify({
                    id: messageId++,
                    method: 'Runtime.evaluate',
                    params: { expression: code6, returnByValue: true }
                }));
            }

            if (response.id === 6) {
                if (response.result && response.result.result) {
                    console.log('\n\n📋 解密相关函数:\n');
                    const result = JSON.parse(response.result.result.value);
                    allResults.decryptFunctions = result;
                    console.log(JSON.stringify(result, null, 2));
                }

                // 保存所有结果
                fs.writeFileSync('C:\\wangshangliao\\group_settings_full.json', 
                    JSON.stringify(allResults, null, 2));
                console.log('\n\n✅ 所有结果已保存到 group_settings_full.json');
                
                ws.close();
                resolve(allResults);
            }
        });

        ws.on('error', (err) => {
            console.error('WebSocket错误:', err);
            reject(err);
        });

        ws.on('close', () => {
            console.log('\n连接已关闭');
        });
    });
}

extractGroupSettings().catch(console.error);

