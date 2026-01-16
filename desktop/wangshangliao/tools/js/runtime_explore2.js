const WebSocket = require('ws');
const http = require('http');

async function explore() {
    console.log('Connecting to WangShangLiao...');
    
    const targets = await new Promise((resolve, reject) => {
        http.get('http://127.0.0.1:9222/json', (res) => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', () => resolve(JSON.parse(data)));
        }).on('error', reject);
    });
    
    const pageTarget = targets.find(t => t.type === 'page' && t.url.includes('wangshangliao'));
    if (!pageTarget) {
        console.log('WangShangLiao not found');
        return;
    }
    
    console.log('Found:', pageTarget.url.substring(0, 80));
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
    console.log('Connected to DevTools\n');
    
    async function evaluate(expression, awaitPromise = false) {
        return new Promise((resolve, reject) => {
            const id = msgId++;
            const timeout = setTimeout(() => {
                pending.delete(id);
                resolve(null);
            }, 5000);
            
            pending.set(id, (result) => {
                clearTimeout(timeout);
                if (result.result && result.result.result) {
                    resolve(result.result.result.value);
                } else if (result.result && result.result.exceptionDetails) {
                    resolve('ERROR: ' + result.result.exceptionDetails.text);
                } else {
                    resolve(null);
                }
            });
            
            ws.send(JSON.stringify({
                id,
                method: 'Runtime.evaluate',
                params: { 
                    expression, 
                    returnByValue: true, 
                    awaitPromise,
                    includeCommandLineAPI: true
                }
            }));
        });
    }
    
    // 1. NIM SDK 方法
    console.log('=== NIM SDK Methods ===');
    const nimMethods = await evaluate('Object.keys(window.nim || {}).filter(k => typeof window.nim[k] === "function").sort().join("\\n")');
    if (nimMethods) {
        console.log(nimMethods);
    } else {
        console.log('nim not accessible');
    }
    
    // 2. NIM 配置信息
    console.log('\n=== NIM Config ===');
    const nimConfig = await evaluate('JSON.stringify({appKey: window.nim?.options?.appKey, account: window.nim?.options?.account}, null, 2)');
    console.log(nimConfig || 'No config');
    
    // 3. 解密函数测试
    console.log('\n=== Decrypt Functions ===');
    const decryptInfo = await evaluate(
        (function() {
            var result = [];
            if (typeof AES !== 'undefined') result.push('AES: found');
            if (typeof decryptNick !== 'undefined') result.push('decryptNick: found');
            if (typeof decryptTeamNick !== 'undefined') result.push('decryptTeamNick: found');
            if (typeof AES_decryptNick !== 'undefined') result.push('AES_decryptNick: found');
            return result.join('\\n') || 'No decrypt functions found in global scope';
        })()
    );
    console.log(decryptInfo);
    
    // 4. Vue App 探索
    console.log('\n=== Vue App ===');
    const vueInfo = await evaluate(
        (function() {
            var app = document.querySelector('#app');
            if (app && app.__vue_app__) {
                var globals = app.__vue_app__.config.globalProperties;
                return 'Global properties: ' + Object.keys(globals).join(', ');
            }
            return 'Vue app not found';
        })()
    );
    console.log(vueInfo);
    
    // 5. 当前群组信息
    console.log('\n=== Current Team Info ===');
    const teamInfo = await evaluate(
        (async function() {
            if (!window.nim) return 'nim not found';
            
            // 从URL获取teamId
            var url = window.location.href;
            var match = url.match(/team-(\d+)/);
            if (!match) return 'No team in URL';
            
            var teamId = match[1];
            return new Promise((resolve) => {
                window.nim.getTeam({
                    teamId: teamId,
                    done: function(err, team) {
                        if (err) resolve('Error: ' + JSON.stringify(err));
                        else resolve(JSON.stringify({
                            teamId: team.teamId,
                            name: team.name,
                            owner: team.owner,
                            memberNum: team.memberNum
                        }, null, 2));
                    }
                });
            });
        })()
    , true);
    console.log(teamInfo);
    
    // 6. 获取一个成员的详细信息测试解密
    console.log('\n=== Test Member Decrypt ===');
    const memberTest = await evaluate(
        (async function() {
            if (!window.nim) return 'nim not found';
            
            var url = window.location.href;
            var match = url.match(/team-(\d+)/);
            if (!match) return 'No team';
            
            var teamId = match[1];
            return new Promise((resolve) => {
                window.nim.getTeamMembers({
                    teamId: teamId,
                    done: function(err, obj) {
                        if (err) {
                            resolve('Error: ' + JSON.stringify(err));
                            return;
                        }
                        var members = obj.members || obj || [];
                        if (members.length === 0) {
                            resolve('No members');
                            return;
                        }
                        
                        // 取前3个成员
                        var sample = members.slice(0, 3).map(function(m) {
                            var customData = null;
                            try {
                                if (m.custom) customData = JSON.parse(m.custom);
                            } catch(e) {}
                            
                            return {
                                account: m.account,
                                nick: m.nick,
                                nickInTeam: m.nickInTeam,
                                type: m.type,
                                custom: customData
                            };
                        });
                        resolve(JSON.stringify(sample, null, 2));
                    }
                });
            });
        })()
    , true);
    console.log(memberTest);
    
    ws.close();
    console.log('\n=== Done ===');
}

explore().catch(e => console.error('Error:', e.message));
