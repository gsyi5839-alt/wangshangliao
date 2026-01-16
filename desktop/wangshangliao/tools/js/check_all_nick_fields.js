// Check ALL nick fields including nickInTeam
const WebSocket = require('ws');
const crypto = require('crypto');

const CDP_URL = 'ws://127.0.0.1:9222/devtools/page/8322EEB7A02952E8C4C59B59B616C299';
const TEAM_ID = '40821608989';

const KEY = Buffer.from('d6ba6647b7c43b79d0e42ceb2790e342', 'utf8');
const IV = Buffer.from('kgWRyiiODMjSCh0m', 'utf8');

function decrypt(ciphertext) {
    try {
        const decipher = crypto.createDecipheriv('aes-256-cbc', KEY, IV);
        let decrypted = decipher.update(Buffer.from(ciphertext, 'base64'));
        decrypted = Buffer.concat([decrypted, decipher.final()]);
        return decrypted.toString('utf8');
    } catch (e) {
        return null;
    }
}

async function checkAllNicks() {
    console.log('=== 检查所有昵称字段 ===\n');
    
    const ws = new WebSocket(CDP_URL);
    
    await new Promise((resolve, reject) => {
        ws.on('open', resolve);
        ws.on('error', reject);
    });
    
    let msgId = 1;
    
    function send(method, params = {}) {
        return new Promise((resolve) => {
            const id = msgId++;
            const handler = (data) => {
                const msg = JSON.parse(data);
                if (msg.id === id) {
                    ws.off('message', handler);
                    resolve(msg.result);
                }
            };
            ws.on('message', handler);
            ws.send(JSON.stringify({ id, method, params }));
        });
    }
    
    async function evaluate(expression) {
        const result = await send('Runtime.evaluate', {
            expression,
            returnByValue: true,
            awaitPromise: true
        });
        return result?.result?.value;
    }
    
    // Get ALL member data including all nick fields
    const membersScript = `
(async function() {
    try {
        var result = await new Promise((resolve, reject) => {
            window.nim.getTeamMembers({
                teamId: '${TEAM_ID}',
                done: function(err, obj) {
                    if (err) reject(err);
                    else resolve(obj);
                }
            });
            setTimeout(() => reject('timeout'), 10000);
        });
        
        var members = result.members || result || [];
        
        // Return ALL fields for each member
        return JSON.stringify(members.map(m => {
            var obj = {};
            for (var key in m) {
                if (typeof m[key] !== 'function') {
                    obj[key] = m[key];
                }
            }
            return obj;
        }));
    } catch(e) {
        return JSON.stringify({ error: e.message });
    }
})()`;

    const membersResult = await evaluate(membersScript);
    const members = JSON.parse(membersResult);
    
    if (members.error) {
        console.log('错误:', members.error);
        ws.close();
        return;
    }
    
    console.log('总成员数:', members.length);
    
    // Find any member with "logo" in any field
    console.log('\n搜索包含 "logo" 的任何字段...\n');
    
    let found = false;
    
    for (const m of members) {
        // Check all string fields
        for (const key in m) {
            const val = m[key];
            if (typeof val === 'string' && val.toLowerCase().includes('logo')) {
                found = true;
                console.log('✅ 找到 "logo" 在字段', key);
                console.log('   账号:', m.account);
                console.log('   字段值:', val);
                console.log('   所有字段:', JSON.stringify(m, null, 2));
                console.log('');
            }
        }
        
        // Also decrypt custom field
        if (m.custom) {
            try {
                const customData = JSON.parse(m.custom);
                const cipher = customData.nickname_ciphertext || customData.nicknameCiphertext;
                if (cipher) {
                    const decrypted = decrypt(cipher);
                    if (decrypted && decrypted.toLowerCase().includes('logo')) {
                        found = true;
                        console.log('✅ 找到 "logo" (解密昵称)');
                        console.log('   账号:', m.account);
                        console.log('   解密昵称:', decrypted);
                        console.log('');
                    }
                }
            } catch(e) {}
        }
    }
    
    if (!found) {
        console.log('❌ 在任何字段中都没找到 "logo"\n');
        
        // Show a sample member's full data
        console.log('示例成员完整数据:');
        console.log(JSON.stringify(members[0], null, 2));
        
        // Show all nickInTeam values
        console.log('\n\n所有 nickInTeam 值:');
        members.forEach(m => {
            if (m.nickInTeam) {
                console.log(`${m.account}: "${m.nickInTeam}"`);
            }
        });
    }
    
    ws.close();
}

checkAllNicks().catch(console.error);
