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

console.log('=== Decrypt Test ===\n');

// 测试群名称解密
const teamNameCipher = 'doeOJLw6MuN+rPE9NGqwSFmPF7Kx8TYQQw/0qxWOjio=';
console.log('Team Name Ciphertext:', teamNameCipher);
console.log('Team Name Decrypted:', decrypt(teamNameCipher));

// 另一个群的名称（MD5: cfcd208495d565ef66e7dff9f98764da）
// 尝试从另一个群获取

async function main() {
    console.log('\n=== Connecting to WangShangLiao ===\n');
    
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
    
    // 1. 获取所有群组并解密名称
    console.log('=== All Teams with Decrypted Names ===\n');
    const teamsJson = await evaluate(`
        new Promise(function(r) {
            window.nim.getTeams({
                done: function(e, obj) {
                    if (e) { r('Error'); return; }
                    var teams = obj.teams || obj || [];
                    var list = teams.map(function(t) {
                        var serverCustom = null;
                        try { if(t.serverCustom) serverCustom = JSON.parse(t.serverCustom); } catch(ex){}
                        return {
                            teamId: t.teamId,
                            name: t.name,
                            owner: t.owner,
                            memberNum: t.memberNum,
                            nicknameCiphertext: serverCustom ? serverCustom.nickname_ciphertext : null,
                            groupId: serverCustom ? serverCustom.group_id : null,
                            forbidChangeNickName: serverCustom ? serverCustom.forbid_change_nick_name : null
                        };
                    });
                    r(JSON.stringify(list));
                }
            });
        })
    `, true);
    
    if (teamsJson) {
        const teams = JSON.parse(teamsJson);
        teams.forEach(t => {
            const decryptedName = t.nicknameCiphertext ? decrypt(t.nicknameCiphertext) : null;
            console.log(`Team ${t.teamId}:`);
            console.log(`  Original Name: ${t.name}`);
            console.log(`  Decrypted Name: ${decryptedName || '(no ciphertext)'}`);
            console.log(`  Owner: ${t.owner}`);
            console.log(`  Members: ${t.memberNum}`);
            console.log(`  Group ID: ${t.groupId}`);
            console.log('');
        });
    }
    
    // 2. 获取用户信息并解密
    console.log('=== User Info with Decrypted Nick ===\n');
    const userInfoJson = await evaluate(`
        new Promise(function(r) {
            window.nim.getUser({
                account: '1948408648',
                done: function(e, user) {
                    if (e) { r('Error'); return; }
                    var customObj = null;
                    try { if(user.custom) customObj = JSON.parse(user.custom); } catch(ex){}
                    r(JSON.stringify({
                        account: user.account,
                        nick: user.nick,
                        avatar: user.avatar,
                        custom: customObj
                    }));
                }
            });
        })
    `, true);
    
    if (userInfoJson) {
        const user = JSON.parse(userInfoJson);
        console.log(`Account: ${user.account}`);
        console.log(`Nick (MD5): ${user.nick}`);
        if (user.custom && user.custom.nicknameCiphertext) {
            console.log(`Decrypted Nick: ${decrypt(user.custom.nicknameCiphertext)}`);
        }
        console.log('');
    }
    
    // 3. 获取好友并解密昵称
    console.log('=== Friends with Decrypted Nicks (first 5) ===\n');
    const friendsJson = await evaluate(`
        new Promise(function(r) {
            window.nim.getFriends({
                done: function(e, friends) {
                    if (e) { r('Error'); return; }
                    r(JSON.stringify((friends || []).slice(0, 5).map(function(f) {
                        return { account: f.account, alias: f.alias };
                    })));
                }
            });
        })
    `, true);
    
    if (friendsJson) {
        const friends = JSON.parse(friendsJson);
        // 批量获取用户信息
        const accounts = friends.map(f => f.account);
        
        const usersJson = await evaluate(`
            new Promise(function(r) {
                window.nim.getUsers({
                    accounts: ${JSON.stringify(accounts)},
                    done: function(e, users) {
                        if (e) { r('Error'); return; }
                        r(JSON.stringify((users || []).map(function(u) {
                            var customObj = null;
                            try { if(u.custom) customObj = JSON.parse(u.custom); } catch(ex){}
                            return {
                                account: u.account,
                                nick: u.nick,
                                nicknameCiphertext: customObj ? (customObj.nicknameCiphertext || customObj.nickname_ciphertext) : null
                            };
                        })));
                    }
                });
            })
        `, true);
        
        if (usersJson) {
            const users = JSON.parse(usersJson);
            users.forEach(u => {
                const decryptedNick = u.nicknameCiphertext ? decrypt(u.nicknameCiphertext) : null;
                console.log(`${u.account}: ${decryptedNick || u.nick || '(unknown)'}`);
            });
        }
    }
    
    // 4. 探索消息内容解密
    console.log('\n=== Message Content Analysis ===\n');
    const msgsJson = await evaluate(`
        new Promise(function(r) {
            window.nim.getLocalMsgs({
                sessionId: 'team-40821608989',
                limit: 5,
                done: function(e, obj) {
                    if (e) { r('Error'); return; }
                    var msgs = obj.msgs || obj || [];
                    r(JSON.stringify(msgs.map(function(m) {
                        var content = null;
                        try { if(m.content) content = JSON.parse(m.content); } catch(ex){}
                        return {
                            from: m.from,
                            fromNick: m.fromNick,
                            type: m.type,
                            text: m.text,
                            content: content,
                            time: new Date(m.time).toLocaleString()
                        };
                    })));
                }
            });
        })
    `, true);
    
    if (msgsJson) {
        const msgs = JSON.parse(msgsJson);
        msgs.forEach((m, idx) => {
            console.log(`Message ${idx + 1}:`);
            console.log(`  From: ${m.from}`);
            console.log(`  FromNick: ${m.fromNick}`);
            console.log(`  Type: ${m.type}`);
            console.log(`  Text: ${m.text || '(empty)'}`);
            if (m.content && m.content.b) {
                // 尝试解密消息内容中的b字段
                const decryptedContent = decrypt(m.content.b);
                console.log(`  Content.b decrypted: ${decryptedContent ? decryptedContent.substring(0, 50) + '...' : '(failed)'}`);
            }
            console.log(`  Time: ${m.time}`);
            console.log('');
        });
    }
    
    // 5. 探索更多NIM方法
    console.log('=== NIM Methods Exploration ===\n');
    const nimMethods = await evaluate(`
        Object.keys(window.nim).filter(function(k) {
            return typeof window.nim[k] === 'function';
        }).sort().join('\\n')
    `);
    console.log('Available NIM Methods:');
    console.log(nimMethods || 'Failed');
    
    // 6. 探索事件监听
    console.log('\n=== NIM Event Handlers ===\n');
    const events = await evaluate(`
        (function() {
            var events = [];
            if (window.nim && window.nim._eventHandlers) {
                events = Object.keys(window.nim._eventHandlers);
            }
            return events.join(', ') || 'No event handlers found';
        })()
    `);
    console.log('Registered Events:', events);
    
    ws.close();
    console.log('\n=== Done ===');
}

main().catch(function(e) { console.error('Error:', e.message); });

