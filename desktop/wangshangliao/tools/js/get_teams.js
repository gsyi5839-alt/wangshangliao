/**
 * 获取群聊列表和成员信息
 */
const WebSocket = require('ws');
const http = require('http');
const crypto = require('crypto');

// AES-256-CBC 配置
const AES_KEY = Buffer.from('d6ba6647b7c43b79d0e42ceb2790e342');
const AES_IV = Buffer.from('kgWRyiiODMjSCh0m');

function decryptNickname(ciphertextBase64) {
    try {
        const cipherBytes = Buffer.from(ciphertextBase64, 'base64');
        const decipher = crypto.createDecipheriv('aes-256-cbc', AES_KEY, AES_IV);
        let decrypted = decipher.update(cipherBytes);
        decrypted = Buffer.concat([decrypted, decipher.final()]);
        return decrypted.toString('utf8');
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
    console.log('Connecting to WangShangLiao...\n');
    
    const pages = await httpGet('http://localhost:9222/json');
    const page = pages.find(p => p.url && p.url.includes('wangshangliao'));
    
    if (!page) {
        console.log('WangShangLiao not found');
        return;
    }
    
    console.log('Current URL:', page.url);
    
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
            }, 15000);
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
    console.log('\nConnected!\n');
    
    // 获取群列表
    const script = `
(async function() {
    var result = {
        teams: [],
        error: null
    };
    
    if (!window.nim) {
        result.error = 'nim not found';
        return JSON.stringify(result, null, 2);
    }
    
    // 方法1：从 store 获取
    try {
        if (window.app && window.app.__vue__ && window.app.__vue__.$store) {
            var store = window.app.__vue__.$store;
            if (store.state && store.state.teams) {
                var teamsMap = store.state.teams;
                Object.values(teamsMap).forEach(function(t) {
                    if (t.teamId) {
                        result.teams.push({
                            teamId: t.teamId,
                            name: t.name,
                            memberNum: t.memberNum
                        });
                    }
                });
            }
        }
    } catch(e) {}
    
    // 方法2：使用 getTeams API
    if (result.teams.length === 0) {
        try {
            var teams = await new Promise((resolve, reject) => {
                window.nim.getTeams({
                    done: (err, data) => err ? reject(err) : resolve(data)
                });
                setTimeout(() => reject(new Error('timeout')), 5000);
            });
            
            result.teams = (teams || []).map(function(t) {
                return {
                    teamId: t.teamId,
                    name: t.name,
                    memberNum: t.memberNum
                };
            });
        } catch(e) {
            result.error = e.message;
        }
    }
    
    return JSON.stringify(result, null, 2);
})();`;

    const data = await evaluate(script);
    const parsed = JSON.parse(data);
    
    console.log('=== 群聊列表 ===\n');
    
    if (parsed.teams.length === 0) {
        console.log('未找到群聊！请确保已加入群聊。');
        console.log('Error:', parsed.error);
        ws.close();
        return;
    }
    
    for (const t of parsed.teams) {
        console.log(`- ${t.name || '未命名群'} (ID: ${t.teamId}, 成员数: ${t.memberNum})`);
    }
    
    // 获取第一个群的成员详情
    const teamId = parsed.teams[0].teamId;
    console.log(`\n=== 获取群 ${teamId} 的成员详情 ===\n`);
    
    const memberScript = `
(async function() {
    var result = {
        members: [],
        error: null
    };
    
    try {
        var teamData = await new Promise((resolve, reject) => {
            window.nim.getTeamMembers({
                teamId: "${teamId}",
                done: (err, data) => err ? reject(err) : resolve(data)
            });
            setTimeout(() => reject(new Error('timeout')), 10000);
        });
        
        var members = teamData.members || teamData || [];
        
        for (var i = 0; i < Math.min(15, members.length); i++) {
            var m = members[i];
            var info = {
                account: m.account,
                nickInTeam: m.nickInTeam || null,
                custom: m.custom || null
            };
            result.members.push(info);
        }
        
        result.total = members.length;
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})();`;

    const memberData = await evaluate(memberScript);
    const memberParsed = JSON.parse(memberData);
    
    if (memberParsed.error) {
        console.log('Error:', memberParsed.error);
        ws.close();
        return;
    }
    
    console.log(`总成员数: ${memberParsed.total}`);
    console.log(`\n--- 前15个成员 ---\n`);
    
    for (const m of memberParsed.members) {
        console.log(`Account: ${m.account}`);
        console.log(`  nickInTeam: ${m.nickInTeam || '(空)'}`);
        
        // 检查是否是 MD5
        if (m.nickInTeam) {
            const isMd5 = /^[a-f0-9]{32}$/i.test(m.nickInTeam);
            console.log(`  是MD5哈希: ${isMd5}`);
        }
        
        // 检查 custom 字段
        if (m.custom) {
            try {
                const customData = JSON.parse(m.custom);
                const ciphertext = customData.nickname_ciphertext || customData.nicknameCiphertext;
                if (ciphertext) {
                    console.log(`  nickname_ciphertext: ${ciphertext}`);
                    const decrypted = decryptNickname(ciphertext);
                    console.log(`  解密后昵称: ${decrypted || '(解密失败)'}`);
                } else {
                    console.log(`  custom字段: ${m.custom.substring(0, 50)}...`);
                }
            } catch(e) {
                console.log(`  custom字段(非JSON): ${m.custom.substring(0, 50)}`);
            }
        } else {
            console.log(`  custom: (空)`);
        }
        
        console.log('');
    }
    
    ws.close();
}

main().catch(console.error);

