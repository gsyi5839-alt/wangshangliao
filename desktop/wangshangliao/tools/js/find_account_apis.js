/**
 * 查找获取账户/联系人列表的所有API
 */
const WebSocket = require('ws');
const http = require('http');

let ws = null;
let msgId = 0;

async function getWebSocketUrl() {
    return new Promise((resolve, reject) => {
        const req = http.get('http://127.0.0.1:9222/json', (res) => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', () => {
                const pages = JSON.parse(data);
                const mainPage = pages.find(p => p.url?.includes('index.html')) || pages[0];
                resolve(mainPage?.webSocketDebuggerUrl);
            });
        });
        req.on('error', reject);
    });
}

function evaluate(expression, awaitPromise = true) {
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
        ws.send(JSON.stringify({ id, method: 'Runtime.evaluate', params: { expression, awaitPromise, returnByValue: true } }));
    });
}

async function main() {
    console.log('🔍 查找获取账户/联系人列表的API\n');
    
    const wsUrl = await getWebSocketUrl();
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    console.log('✅ 已连接\n');
    
    // 1. 搜索所有相关API
    console.log('=== 1. 搜索账户/联系人相关API ===\n');
    try {
        const script = `(() => {
            var apis = { friends: [], users: [], sessions: [], team: [], contacts: [], search: [], other: [] };
            var keywords = {
                friends: ['friend', 'Friend'],
                users: ['user', 'User', 'account', 'Account'],
                sessions: ['session', 'Session', 'conversation', 'Conversation'],
                team: ['team', 'Team', 'member', 'Member', 'group', 'Group'],
                contacts: ['contact', 'Contact', 'relation', 'Relation'],
                search: ['search', 'Search', 'find', 'Find', 'query', 'Query']
            };
            
            for (var key in window.nim) {
                if (typeof window.nim[key] === 'function') {
                    var found = false;
                    for (var cat in keywords) {
                        if (keywords[cat].some(k => key.includes(k))) {
                            apis[cat].push(key);
                            found = true;
                            break;
                        }
                    }
                    if (!found && (key.includes('get') || key.includes('Get'))) {
                        apis.other.push(key);
                    }
                }
            }
            return apis;
        })()`;
        const result = await evaluate(script, false);
        
        console.log('📋 好友相关API:', result?.friends?.length || 0, '个');
        result?.friends?.forEach(a => console.log('    - ' + a));
        
        console.log('\n📋 用户相关API:', result?.users?.length || 0, '个');
        result?.users?.forEach(a => console.log('    - ' + a));
        
        console.log('\n📋 会话相关API:', result?.sessions?.length || 0, '个');
        result?.sessions?.forEach(a => console.log('    - ' + a));
        
        console.log('\n📋 群组/成员相关API:', result?.team?.length || 0, '个');
        result?.team?.forEach(a => console.log('    - ' + a));
        
        console.log('\n📋 联系人相关API:', result?.contacts?.length || 0, '个');
        result?.contacts?.forEach(a => console.log('    - ' + a));
        
        console.log('\n📋 搜索相关API:', result?.search?.length || 0, '个');
        result?.search?.forEach(a => console.log('    - ' + a));
    } catch (e) {
        console.log('❌ 搜索失败:', e.message);
    }
    
    // 2. 测试获取好友列表
    console.log('\n\n=== 2. 获取好友列表 (getFriends) ===\n');
    try {
        const script = `(async () => {
            return new Promise(r => {
                window.nim.getFriends({
                    done: (err, friends) => {
                        if (err) r({ error: err.message });
                        else r({
                            count: (friends||[]).length,
                            friends: (friends||[]).slice(0, 20).map(f => ({
                                account: f.account,
                                alias: f.alias,
                                valid: f.valid,
                                createTime: f.createTime
                            }))
                        });
                    }
                });
                setTimeout(() => r({ error: 'Timeout' }), 8000);
            });
        })()`;
        const result = await evaluate(script);
        console.log('好友总数:', result?.count);
        console.log('好友列表:');
        result?.friends?.forEach(f => {
            console.log(`  - ${f.account} (备注: ${f.alias || '无'})`);
        });
    } catch (e) {
        console.log('❌ 失败:', e.message);
    }
    
    // 3. 测试获取会话列表（最近联系人）
    console.log('\n\n=== 3. 获取会话列表 (getLocalSessions) ===\n');
    try {
        const script = `(async () => {
            return new Promise(r => {
                window.nim.getLocalSessions({
                    limit: 100,
                    done: (err, sessions) => {
                        if (err) r({ error: err.message });
                        else {
                            var arr = Array.isArray(sessions) ? sessions : (sessions?.sessions || Object.values(sessions || {}));
                            r({
                                count: arr.length,
                                sessions: arr.slice(0, 20).map(s => ({
                                    id: s.id,
                                    scene: s.scene,
                                    to: s.to,
                                    unread: s.unread,
                                    updateTime: s.updateTime,
                                    lastMsgType: s.lastMsg?.type
                                }))
                            });
                        }
                    }
                });
                setTimeout(() => r({ error: 'Timeout' }), 8000);
            });
        })()`;
        const result = await evaluate(script);
        console.log('会话总数:', result?.count);
        console.log('会话列表 (可直接发送消息的目标):');
        result?.sessions?.forEach(s => {
            const type = s.scene === 'p2p' ? '私聊' : '群聊';
            console.log(`  - [${type}] ${s.to} (未读:${s.unread || 0})`);
        });
    } catch (e) {
        console.log('❌ 失败:', e.message);
    }
    
    // 4. 测试获取群列表
    console.log('\n\n=== 4. 获取群列表 (getTeams) ===\n');
    try {
        const script = `(async () => {
            return new Promise(r => {
                window.nim.getTeams({
                    done: (err, teams) => {
                        if (err) r({ error: err.message });
                        else r({
                            count: (teams||[]).length,
                            teams: (teams||[]).map(t => ({
                                teamId: t.teamId,
                                name: t.name,
                                memberNum: t.memberNum,
                                owner: t.owner
                            }))
                        });
                    }
                });
                setTimeout(() => r({ error: 'Timeout' }), 8000);
            });
        })()`;
        const result = await evaluate(script);
        console.log('群总数:', result?.count);
        console.log('群列表:');
        result?.teams?.forEach(t => {
            console.log(`  - ${t.teamId} "${t.name}" (${t.memberNum}人)`);
        });
    } catch (e) {
        console.log('❌ 失败:', e.message);
    }
    
    // 5. 获取指定群的成员列表
    console.log('\n\n=== 5. 获取群成员列表 (getTeamMembers) ===\n');
    try {
        // 先获取第一个群
        const teamsScript = `(async () => {
            return new Promise(r => {
                window.nim.getTeams({ done: (e, t) => r(t || []) });
                setTimeout(() => r([]), 3000);
            });
        })()`;
        const teams = await evaluate(teamsScript);
        
        if (teams && teams.length > 0) {
            const teamId = teams[0].teamId;
            console.log(`测试群: ${teamId}\n`);
            
            const script = `(async () => {
                return new Promise(r => {
                    window.nim.getTeamMembers({
                        teamId: '${teamId}',
                        done: (err, obj) => {
                            if (err) r({ error: err.message });
                            else r({
                                count: (obj?.members||[]).length,
                                members: (obj?.members||[]).slice(0, 30).map(m => ({
                                    account: m.account,
                                    nick: m.nick,
                                    nickInTeam: m.nickInTeam,
                                    type: m.type
                                }))
                            });
                        }
                    });
                    setTimeout(() => r({ error: 'Timeout' }), 8000);
                });
            })()`;
            const result = await evaluate(script);
            console.log('成员总数:', result?.count);
            console.log('成员列表 (可向其发送消息):');
            result?.members?.forEach(m => {
                const role = m.type === 'owner' ? '群主' : (m.type === 'manager' ? '管理员' : '成员');
                console.log(`  - ${m.account} [${role}] (昵称: ${m.nickInTeam || m.nick || '无'})`);
            });
        }
    } catch (e) {
        console.log('❌ 失败:', e.message);
    }
    
    // 6. 获取用户信息
    console.log('\n\n=== 6. 根据账号获取用户信息 (getUser/getUsers) ===\n');
    try {
        const script = `(async () => {
            // 先获取自己的信息
            var myInfo = await new Promise(r => {
                window.nim.getMyInfo({ done: (e, i) => r(i) });
                setTimeout(() => r(null), 3000);
            });
            
            // 测试getUser
            var testAccount = myInfo?.account || '1948408648';
            var userInfo = await new Promise(r => {
                window.nim.getUser({
                    account: testAccount,
                    done: (err, user) => {
                        if (err) r({ error: err.message });
                        else r(user);
                    }
                });
                setTimeout(() => r({ error: 'Timeout' }), 5000);
            });
            
            return {
                myAccount: myInfo?.account,
                myNick: myInfo?.nick,
                testUser: userInfo
            };
        })()`;
        const result = await evaluate(script);
        console.log('当前账号:', result?.myAccount);
        console.log('当前昵称:', result?.myNick);
        console.log('getUser返回:', JSON.stringify(result?.testUser, null, 2));
    } catch (e) {
        console.log('❌ 失败:', e.message);
    }
    
    // 7. 搜索用户
    console.log('\n\n=== 7. 搜索用户API ===\n');
    try {
        const script = `(() => {
            var searchAPIs = [];
            for (var key in window.nim) {
                if (typeof window.nim[key] === 'function' && 
                    (key.toLowerCase().includes('search') || key.toLowerCase().includes('find') || key.toLowerCase().includes('query'))) {
                    searchAPIs.push({
                        name: key,
                        length: window.nim[key].length
                    });
                }
            }
            return searchAPIs;
        })()`;
        const result = await evaluate(script, false);
        console.log('搜索相关API:');
        result?.forEach(api => console.log(`  - ${api.name}`));
    } catch (e) {
        console.log('❌ 失败:', e.message);
    }
    
    // 总结
    console.log('\n\n========================================');
    console.log('📌 获取账户列表的关键API总结');
    console.log('========================================\n');
    console.log('1️⃣  getFriends() - 获取好友列表');
    console.log('    用途: 获取所有好友，可向其发送私聊消息');
    console.log('    返回: [{account, alias, valid, createTime}]\n');
    
    console.log('2️⃣  getLocalSessions() - 获取会话列表');
    console.log('    用途: 获取最近联系人/群，包含私聊和群聊');
    console.log('    返回: [{id, scene, to, unread}]\n');
    
    console.log('3️⃣  getTeams() - 获取群列表');
    console.log('    用途: 获取所有加入的群');
    console.log('    返回: [{teamId, name, memberNum, owner}]\n');
    
    console.log('4️⃣  getTeamMembers({teamId}) - 获取群成员');
    console.log('    用途: 获取指定群的所有成员');
    console.log('    返回: [{account, nick, nickInTeam, type}]\n');
    
    console.log('5️⃣  getUser({account}) / getUsers({accounts}) - 获取用户信息');
    console.log('    用途: 根据账号获取用户详情');
    console.log('    返回: {account, nick, avatar, custom}\n');
    
    console.log('6️⃣  getMyInfo() - 获取自己的信息');
    console.log('    用途: 获取当前登录账号信息');
    console.log('    返回: {account, nick, avatar, custom}\n');
    
    ws.close();
}

main().catch(console.error);
