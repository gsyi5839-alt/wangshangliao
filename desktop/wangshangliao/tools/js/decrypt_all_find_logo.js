// Decrypt ALL member nicknames and find "logo"
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

async function findLogo() {
    console.log('=== 解密所有昵称查找 "logo" ===\n');
    
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
    
    // Get ALL members
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
        return JSON.stringify(members.map(m => ({
            account: m.account,
            nick: m.nick,
            nickInTeam: m.nickInTeam,
            custom: m.custom,
            type: m.type
        })));
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
    console.log('\n解密所有昵称...\n');
    
    let logoAccount = null;
    let allDecrypted = [];
    
    for (const m of members) {
        let decryptedNick = null;
        
        // Try to decrypt from custom
        if (m.custom) {
            try {
                const customData = JSON.parse(m.custom);
                const cipher = customData.nickname_ciphertext || customData.nicknameCiphertext;
                if (cipher) {
                    decryptedNick = decrypt(cipher);
                }
            } catch(e) {}
        }
        
        const finalNick = decryptedNick || m.nickInTeam || m.nick || '';
        
        allDecrypted.push({
            account: m.account,
            nick: finalNick,
            type: m.type
        });
        
        // Check for "logo" (case insensitive)
        if (finalNick.toLowerCase() === 'logo') {
            logoAccount = m.account;
            console.log('✅✅✅ 找到 "logo"! ✅✅✅');
            console.log('账号:', m.account);
            console.log('昵称:', finalNick);
            console.log('类型:', m.type);
        }
    }
    
    if (!logoAccount) {
        console.log('❌ 未找到昵称为 "logo" 的用户\n');
        
        // Show all decrypted nicknames
        console.log('所有已解密的昵称:');
        console.log('-------------------');
        
        const withNicks = allDecrypted.filter(m => m.nick && m.nick.length > 0);
        withNicks.sort((a, b) => a.nick.localeCompare(b.nick));
        
        for (const m of withNicks) {
            // Highlight if contains "logo"
            if (m.nick.toLowerCase().includes('logo')) {
                console.log(`*** ${m.account}: "${m.nick}" (${m.type}) ***`);
            } else {
                console.log(`${m.account}: "${m.nick}"`);
            }
        }
        
        console.log('\n-------------------');
        console.log('有昵称的成员:', withNicks.length);
        console.log('无昵称的成员:', allDecrypted.length - withNicks.length);
    }
    
    ws.close();
}

findLogo().catch(console.error);
