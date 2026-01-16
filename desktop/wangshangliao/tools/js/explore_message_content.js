const WebSocket = require('ws');
const http = require('http');
const crypto = require('crypto');

// AES解密配置
const key = Buffer.from('d6ba6647b7c43b79d0e42ceb2790e342', 'utf8');
const iv = Buffer.from('kgWRyiiODMjSCh0m', 'utf8');

function decrypt(ciphertext) {
    try {
        const decipher = crypto.createDecipheriv('aes-256-cbc', key, iv);
        decipher.setAutoPadding(true);
        let decrypted = decipher.update(Buffer.from(ciphertext, 'base64'));
        decrypted = Buffer.concat([decrypted, decipher.final()]);
        return decrypted.toString('utf8');
    } catch (e) {
        return null;
    }
}

async function main() {
    console.log('=== Connecting ===\n');
    
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
    
    // 1. 获取不同类型的消息
    console.log('=== Message Types Analysis ===\n');
    const msgsJson = await evaluate(`
        new Promise(function(r) {
            window.nim.getLocalMsgs({
                sessionId: 'team-40821608989',
                limit: 50,
                done: function(e, obj) {
                    if (e) { r('Error'); return; }
                    var msgs = obj.msgs || obj || [];
                    // 按类型分组
                    var byType = {};
                    msgs.forEach(function(m) {
                        if (!byType[m.type]) byType[m.type] = [];
                        if (byType[m.type].length < 3) {
                            byType[m.type].push({
                                from: m.from,
                                fromNick: m.fromNick,
                                type: m.type,
                                text: m.text,
                                content: m.content,
                                file: m.file,
                                geo: m.geo,
                                pushContent: m.pushContent,
                                time: m.time
                            });
                        }
                    });
                    r(JSON.stringify(byType, null, 2));
                }
            });
        })
    `, true);
    
    if (msgsJson) {
        const msgsByType = JSON.parse(msgsJson);
        Object.keys(msgsByType).forEach(type => {
            console.log(`--- Type: ${type} ---`);
            msgsByType[type].forEach((m, idx) => {
                console.log(`  [${idx + 1}] From: ${m.from}, Nick: ${m.fromNick}`);
                if (m.text) console.log(`      Text: ${m.text}`);
                if (m.content) {
                    try {
                        const content = JSON.parse(m.content);
                        console.log(`      Content keys: ${Object.keys(content).join(', ')}`);
                        if (content.type) console.log(`      Content.type: ${content.type}`);
                        if (content.data) console.log(`      Content.data: ${JSON.stringify(content.data).substring(0, 100)}`);
                    } catch(e) {
                        console.log(`      Content: ${m.content.substring(0, 100)}`);
                    }
                }
                if (m.file) console.log(`      File: ${JSON.stringify(m.file).substring(0, 100)}`);
            });
            console.log('');
        });
    }
    
    // 2. 获取历史消息（服务端）
    console.log('=== Server History Messages ===\n');
    const historyJson = await evaluate(`
        new Promise(function(r) {
            window.nim.getHistoryMsgs({
                scene: 'team',
                to: '40821608989',
                limit: 10,
                done: function(e, obj) {
                    if (e) { r('Error: ' + JSON.stringify(e)); return; }
                    var msgs = obj.msgs || obj || [];
                    r(JSON.stringify(msgs.map(function(m) {
                        return {
                            from: m.from,
                            fromNick: m.fromNick,
                            type: m.type,
                            text: m.text ? m.text.substring(0, 50) : null,
                            time: new Date(m.time).toLocaleString()
                        };
                    })));
                }
            });
        })
    `, true);
    console.log(historyJson || 'Failed');
    
    // 3. 获取群公告
    console.log('\n=== Team Announcement ===\n');
    const announcementJson = await evaluate(`
        new Promise(function(r) {
            window.nim.getTeam({
                teamId: '40821608989',
                done: function(e, team) {
                    if (e) { r('Error'); return; }
                    r(JSON.stringify({
                        announcement: team.announcement,
                        intro: team.intro,
                        custom: team.custom,
                        serverCustom: team.serverCustom
                    }, null, 2));
                }
            });
        })
    `, true);
    console.log(announcementJson || 'Failed');
    
    // 4. 获取群成员角色分布
    console.log('\n=== Team Members by Role ===\n');
    const membersRoleJson = await evaluate(`
        new Promise(function(r) {
            window.nim.getTeamMembers({
                teamId: '40821608989',
                done: function(e, obj) {
                    if (e) { r('Error'); return; }
                    var members = obj.members || obj || [];
                    var byRole = { owner: [], manager: [], normal: [] };
                    members.forEach(function(m) {
                        if (byRole[m.type]) {
                            byRole[m.type].push({
                                account: m.account,
                                nick: m.nick,
                                nickInTeam: m.nickInTeam,
                                mute: m.mute
                            });
                        }
                    });
                    r(JSON.stringify({
                        owner: byRole.owner,
                        managers: byRole.manager,
                        normalCount: byRole.normal.length
                    }, null, 2));
                }
            });
        })
    `, true);
    console.log(membersRoleJson || 'Failed');
    
    // 5. 探索私聊消息
    console.log('\n=== P2P Messages (first session) ===\n');
    const p2pMsgsJson = await evaluate(`
        new Promise(function(r) {
            window.nim.getLocalMsgs({
                sessionId: 'p2p-1628907626',
                limit: 5,
                done: function(e, obj) {
                    if (e) { r('Error'); return; }
                    var msgs = obj.msgs || obj || [];
                    r(JSON.stringify(msgs.map(function(m) {
                        return {
                            from: m.from,
                            to: m.to,
                            fromNick: m.fromNick,
                            type: m.type,
                            text: m.text,
                            flow: m.flow,
                            time: new Date(m.time).toLocaleString()
                        };
                    }), null, 2));
                }
            });
        })
    `, true);
    console.log(p2pMsgsJson || 'Failed');
    
    // 6. 获取关系列表
    console.log('\n=== Relations ===\n');
    const relationsJson = await evaluate(`
        new Promise(function(r) {
            window.nim.getRelations({
                done: function(e, obj) {
                    if (e) { r('Error: ' + JSON.stringify(e)); return; }
                    r(JSON.stringify({
                        blacklist: obj.blacklist,
                        mutelist: obj.mutelist
                    }, null, 2));
                }
            });
        })
    `, true);
    console.log(relationsJson || 'Failed');
    
    // 7. 探索消息回执功能
    console.log('\n=== Message Receipt Info ===\n');
    const receiptInfo = await evaluate(`
        (function() {
            var result = {};
            if (window.nim && window.nim.options) {
                result.needMsgReceipt = window.nim.options.needMsgReceipt;
                result.syncMsgReceipts = window.nim.options.syncMsgReceipts;
            }
            return JSON.stringify(result);
        })()
    `);
    console.log(receiptInfo || 'Failed');
    
    // 8. 探索NIM配置
    console.log('\n=== NIM Full Options ===\n');
    const nimOptions = await evaluate(`
        (function() {
            if (window.nim && window.nim.options) {
                var opts = window.nim.options;
                return JSON.stringify({
                    appKey: opts.appKey,
                    account: opts.account,
                    debug: opts.debug,
                    db: opts.db,
                    syncSessionUnread: opts.syncSessionUnread,
                    syncRoamingMsgs: opts.syncRoamingMsgs,
                    autoMarkRead: opts.autoMarkRead,
                    shouldIgnoreNotification: typeof opts.shouldIgnoreNotification,
                    onconnect: typeof opts.onconnect,
                    ondisconnect: typeof opts.ondisconnect,
                    onerror: typeof opts.onerror,
                    onmsg: typeof opts.onmsg,
                    onsysmsg: typeof opts.onsysmsg,
                    onupdatesession: typeof opts.onupdatesession
                }, null, 2);
            }
            return 'No options';
        })()
    `);
    console.log(nimOptions || 'Failed');
    
    ws.close();
    console.log('\n=== Done ===');
}

main().catch(function(e) { console.error('Error:', e.message); });

