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
    
    // 获取所有群成员的加密昵称
    console.log('=== Fetching All Team Members ===\n');
    const membersJson = await evaluate(`
        new Promise(function(r) {
            window.nim.getTeamMembers({
                teamId: '40821608989',
                done: function(e, obj) {
                    if (e) { r(JSON.stringify({error: e})); return; }
                    var list = (obj.members || obj || []).map(function(m) {
                        var customObj = null;
                        try { if(m.custom) customObj = JSON.parse(m.custom); } catch(ex){}
                        return {
                            account: m.account,
                            nick: m.nick,
                            nickInTeam: m.nickInTeam,
                            type: m.type,
                            nicknameCiphertext: customObj ? (customObj.nicknameCiphertext || customObj.nickname_ciphertext) : null
                        };
                    });
                    r(JSON.stringify(list));
                }
            });
        })
    `, true);
    
    if (!membersJson) {
        console.log('Failed to get members');
        ws.close();
        return;
    }
    
    const members = JSON.parse(membersJson);
    console.log(`Found ${members.length} members\n`);
    
    // 解密每个成员的昵称
    console.log('=== Decrypted Nicknames ===\n');
    console.log('Account\t\t\tType\t\tNickname');
    console.log('-'.repeat(60));
    
    let decryptedCount = 0;
    members.forEach(m => {
        let nickname = '(unknown)';
        
        if (m.nicknameCiphertext) {
            const decrypted = decrypt(m.nicknameCiphertext);
            if (decrypted) {
                nickname = decrypted;
                decryptedCount++;
            } else {
                nickname = `(decrypt failed: ${m.nicknameCiphertext})`;
            }
        } else if (m.nick && !/^[a-f0-9]{32}$/i.test(m.nick)) {
            // nick不是MD5哈希
            nickname = m.nick;
        } else if (m.nickInTeam) {
            nickname = m.nickInTeam;
        }
        
        console.log(`${m.account}\t\t${m.type}\t\t${nickname}`);
    });
    
    console.log('-'.repeat(60));
    console.log(`\nTotal: ${members.length} members, ${decryptedCount} decrypted`);
    
    ws.close();
}

main().catch(e => console.error('Error:', e.message));

