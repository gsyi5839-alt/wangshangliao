const WebSocket = require('ws');
const http = require('http');

async function explore() {
    console.log('Connecting to WangShangLiao...\n');
    
    const targets = await new Promise((resolve, reject) => {
        http.get('http://127.0.0.1:9222/json', (res) => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', () => resolve(JSON.parse(data)));
        }).on('error', reject);
    });
    
    const pageTarget = targets.find(t => t.type === 'page' && t.url.includes('index.html'));
    if (!pageTarget) {
        console.log('Page not found');
        return;
    }
    
    const ws = new WebSocket(pageTarget.webSocketDebuggerUrl);
    let msgId = 1;
    const pending = new Map();
    
    ws.on('message', (data) => {
        const msg = JSON.parse(data.toString());
        if (msg.id && pending.has(msg.id)) {
            pending.get(msg.id)(msg);
            pending.delete(msg.id);
        }
    });
    
    await new Promise(resolve => ws.on('open', resolve));
    console.log('Connected!\n');
    
    async function evaluate(expression, awaitPromise = false) {
        return new Promise((resolve) => {
            const id = msgId++;
            const timeout = setTimeout(() => {
                pending.delete(id);
                resolve(null);
            }, 15000);
            
            pending.set(id, (result) => {
                clearTimeout(timeout);
                if (result.result && result.result.result && result.result.result.value !== undefined) {
                    resolve(result.result.result.value);
                } else {
                    resolve(null);
                }
            });
            
            ws.send(JSON.stringify({
                id,
                method: 'Runtime.evaluate',
                params: { expression, returnByValue: true, awaitPromise }
            }));
        });
    }
    
    // 1. 获取所有群组列表
    console.log('=== All Teams (Groups) ===');
    const teams = await evaluate(`
        new Promise(function(r) {
            window.nim.getTeams({
                done: function(e, obj) {
                    if (e) { r('Error'); return; }
                    var teams = obj.teams || obj || [];
                    var list = teams.map(function(t) {
                        return {
                            teamId: t.teamId,
                            name: t.name,
                            owner: t.owner,
                            memberNum: t.memberNum,
                            type: t.type
                        };
                    });
                    r(JSON.stringify(list, null, 2));
                }
            });
        })
    `, true);
    console.log(teams || 'Failed');
    
    // 2. 获取好友列表
    console.log('\n=== Friends List ===');
    const friends = await evaluate(`
        new Promise(function(r) {
            window.nim.getFriends({
                done: function(e, friends) {
                    if (e) { r('Error: ' + JSON.stringify(e)); return; }
                    var list = (friends || []).slice(0, 10).map(function(f) {
                        return {
                            account: f.account,
                            alias: f.alias,
                            createTime: f.createTime
                        };
                    });
                    r(JSON.stringify(list, null, 2));
                }
            });
        })
    `, true);
    console.log(friends || 'No friends or failed');
    
    // 3. 获取会话列表
    console.log('\n=== Sessions List ===');
    const sessions = await evaluate(`
        new Promise(function(r) {
            window.nim.getLocalSessions({
                limit: 10,
                done: function(e, sessions) {
                    if (e) { r('Error'); return; }
                    var list = (sessions || []).map(function(s) {
                        return {
                            id: s.id,
                            scene: s.scene,
                            to: s.to,
                            unread: s.unread,
                            lastMsg: s.lastMsg ? {
                                type: s.lastMsg.type,
                                text: (s.lastMsg.text || '').substring(0, 30)
                            } : null
                        };
                    });
                    r(JSON.stringify(list, null, 2));
                }
            });
        })
    `, true);
    console.log(sessions || 'Failed');
    
    // 4. 获取黑名单
    console.log('\n=== Blacklist ===');
    const blacklist = await evaluate(`
        new Promise(function(r) {
            window.nim.getBlacklist({
                done: function(e, list) {
                    if (e) { r('Error'); return; }
                    r(JSON.stringify(list || [], null, 2));
                }
            });
        })
    `, true);
    console.log(blacklist || 'Empty or failed');
    
    // 5. 获取静音列表
    console.log('\n=== Mute List ===');
    const mutelist = await evaluate(`
        new Promise(function(r) {
            window.nim.getMutelist({
                done: function(e, list) {
                    if (e) { r('Error: ' + JSON.stringify(e)); return; }
                    r(JSON.stringify(list || [], null, 2));
                }
            });
        })
    `, true);
    console.log(mutelist || 'Empty or failed');
    
    // 6. 获取当前用户信息
    console.log('\n=== My Info ===');
    const myInfo = await evaluate(`
        new Promise(function(r) {
            window.nim.getMyInfo({
                done: function(e, info) {
                    if (e) { r('Error'); return; }
                    r(JSON.stringify({
                        account: info.account,
                        nick: info.nick,
                        avatar: info.avatar,
                        gender: info.gender,
                        email: info.email,
                        tel: info.tel,
                        sign: info.sign
                    }, null, 2));
                }
            });
        })
    `, true);
    console.log(myInfo || 'Failed');
    
    // 7. 获取系统消息
    console.log('\n=== System Messages ===');
    const sysMsgs = await evaluate(`
        new Promise(function(r) {
            window.nim.getLocalSysMsgs({
                limit: 5,
                done: function(e, msgs) {
                    if (e) { r('Error'); return; }
                    var list = (msgs || []).map(function(m) {
                        return {
                            type: m.type,
                            from: m.from,
                            to: m.to,
                            time: new Date(m.time).toLocaleString(),
                            state: m.state
                        };
                    });
                    r(JSON.stringify(list, null, 2));
                }
            });
        })
    `, true);
    console.log(sysMsgs || 'Empty or failed');
    
    // 8. 获取置顶会话
    console.log('\n=== Stick Top Sessions ===');
    const stickTop = await evaluate(`
        new Promise(function(r) {
            window.nim.getStickTopSessions({
                done: function(e, sessions) {
                    if (e) { r('Error: ' + JSON.stringify(e)); return; }
                    r(JSON.stringify(sessions || [], null, 2));
                }
            });
        })
    `, true);
    console.log(stickTop || 'Empty or failed');
    
    // 9. 获取服务端会话
    console.log('\n=== Server Sessions ===');
    const serverSessions = await evaluate(`
        new Promise(function(r) {
            window.nim.getServerSessions({
                limit: 5,
                done: function(e, obj) {
                    if (e) { r('Error: ' + JSON.stringify(e)); return; }
                    var sessions = obj.sessions || obj || [];
                    var list = sessions.map(function(s) {
                        return {
                            id: s.id,
                            scene: s.scene,
                            to: s.to,
                            time: s.time
                        };
                    });
                    r(JSON.stringify(list, null, 2));
                }
            });
        })
    `, true);
    console.log(serverSessions || 'Failed');
    
    // 10. 探索Pinia stores
    console.log('\n=== Pinia Stores ===');
    const piniaStores = await evaluate(`
        (function() {
            var result = {};
            if (window.__PINIA__) {
                window.__PINIA__._s.forEach(function(store, id) {
                    var stateKeys = Object.keys(store.$state || {});
                    var actionKeys = Object.keys(store).filter(function(k) {
                        return typeof store[k] === 'function' && !k.startsWith('$') && !k.startsWith('_');
                    });
                    result[id] = {
                        stateKeys: stateKeys.slice(0, 10),
                        actions: actionKeys.slice(0, 10)
                    };
                });
            }
            return JSON.stringify(result, null, 2);
        })()
    `);
    console.log(piniaStores || 'Pinia not found');
    
    // 11. 获取群组详细信息
    console.log('\n=== Team Detail (40821608989) ===');
    const teamDetail = await evaluate(`
        new Promise(function(r) {
            window.nim.getTeam({
                teamId: '40821608989',
                done: function(e, team) {
                    if (e) { r('Error'); return; }
                    r(JSON.stringify({
                        teamId: team.teamId,
                        name: team.name,
                        avatar: team.avatar,
                        owner: team.owner,
                        intro: team.intro,
                        announcement: team.announcement,
                        memberNum: team.memberNum,
                        level: team.level,
                        createTime: team.createTime,
                        updateTime: team.updateTime,
                        joinMode: team.joinMode,
                        beInviteMode: team.beInviteMode,
                        inviteMode: team.inviteMode,
                        updateTeamMode: team.updateTeamMode,
                        updateCustomMode: team.updateCustomMode,
                        mute: team.mute,
                        muteType: team.muteType,
                        serverCustom: team.serverCustom
                    }, null, 2));
                }
            });
        })
    `, true);
    console.log(teamDetail || 'Failed');
    
    // 12. IndexedDB数据库信息
    console.log('\n=== IndexedDB Databases ===');
    const idbInfo = await evaluate(`
        (async function() {
            var dbs = await indexedDB.databases();
            return JSON.stringify(dbs.map(function(db) {
                return { name: db.name, version: db.version };
            }), null, 2);
        })()
    `, true);
    console.log(idbInfo || 'Failed');
    
    ws.close();
    console.log('\n=== Done ===');
}

explore().catch(function(e) { console.error('Error:', e.message); });

