const WebSocket = require('ws');
const http = require('http');

async function explore() {
    console.log('Connecting...');
    
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
    console.log('Connected\n');
    
    async function evaluate(expression, awaitPromise = false) {
        return new Promise((resolve) => {
            const id = msgId++;
            const timeout = setTimeout(() => {
                pending.delete(id);
                resolve(null);
            }, 10000);
            
            pending.set(id, (result) => {
                clearTimeout(timeout);
                if (result.result && result.result.result && result.result.result.value !== undefined) {
                    resolve(result.result.result.value);
                } else if (result.result && result.result.exceptionDetails) {
                    resolve('ERR: ' + (result.result.exceptionDetails.text || 'unknown'));
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
    
    // 检查window.nim
    console.log('=== Check NIM ===');
    const nimCheck = await evaluate('typeof window.nim');
    console.log('window.nim type:', nimCheck);
    
    if (nimCheck === 'object') {
        // 获取所有方法
        console.log('\n=== NIM Methods ===');
        const methods = await evaluate('Object.keys(window.nim).filter(function(k){return typeof window.nim[k]==="function"}).sort().join(", ")');
        console.log(methods);
        
        // 获取群成员
        console.log('\n=== Team Members (first 3) ===');
        const members = await evaluate(`
            new Promise(function(r) {
                window.nim.getTeamMembers({
                    teamId: '40821608989',
                    done: function(e, obj) {
                        if (e) { r('Error: ' + JSON.stringify(e)); return; }
                        var list = (obj.members || obj || []).slice(0, 3).map(function(m) {
                            var customObj = null;
                            try { if(m.custom) customObj = JSON.parse(m.custom); } catch(ex){}
                            return {
                                account: m.account,
                                nick: m.nick,
                                nickInTeam: m.nickInTeam,
                                type: m.type,
                                customKeys: customObj ? Object.keys(customObj) : []
                            };
                        });
                        r(JSON.stringify(list, null, 2));
                    }
                });
            })
        `, true);
        console.log(members);
        
        // 获取一个完整的custom字段
        console.log('\n=== Sample Custom Field ===');
        const customField = await evaluate(`
            new Promise(function(r) {
                window.nim.getTeamMembers({
                    teamId: '40821608989',
                    done: function(e, obj) {
                        if (e) { r('Error'); return; }
                        var members = obj.members || obj || [];
                        for (var i = 0; i < members.length; i++) {
                            if (members[i].custom) {
                                r(members[i].custom);
                                return;
                            }
                        }
                        r('No custom field');
                    }
                });
            })
        `, true);
        console.log(customField);
        
        // 获取最近消息
        console.log('\n=== Recent Messages ===');
        const msgs = await evaluate(`
            new Promise(function(r) {
                window.nim.getLocalMsgs({
                    sessionId: 'team-40821608989',
                    limit: 3,
                    done: function(e, obj) {
                        if (e) { r('Error'); return; }
                        var list = (obj.msgs || obj || []).map(function(m) {
                            return {
                                from: m.from,
                                fromNick: m.fromNick,
                                text: (m.text || '').substring(0, 30)
                            };
                        });
                        r(JSON.stringify(list, null, 2));
                    }
                });
            })
        `, true);
        console.log(msgs);
    }
    
    ws.close();
    console.log('\nDone');
}

explore().catch(function(e) { console.error(e.message); });
