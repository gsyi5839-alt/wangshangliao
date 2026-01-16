// Find and kick user "logo" from team 40821608989
const WebSocket = require('ws');

const CDP_URL = 'ws://127.0.0.1:9222/devtools/page/8322EEB7A02952E8C4C59B59B616C299';
const TEAM_ID = '40821608989';
const TARGET_NICK = 'logo';

// AES decryption for nicknames
const crypto = require('crypto');
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

async function findAndKickLogo() {
    console.log('=== 查找并踢出用户 "logo" ===');
    console.log('群ID:', TEAM_ID);
    console.log('目标昵称:', TARGET_NICK);
    
    const ws = new WebSocket(CDP_URL);
    
    await new Promise((resolve, reject) => {
        ws.on('open', resolve);
        ws.on('error', reject);
    });
    
    console.log('\n已连接CDP!');
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
    
    // Get all members with custom field for decryption
    console.log('\n1. 获取群成员列表...');
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
        
        // Return all members with custom field
        var memberList = members.map(m => ({
            account: m.account,
            nick: m.nick,
            nickInTeam: m.nickInTeam,
            custom: m.custom,
            type: m.type
        }));
        
        return JSON.stringify(memberList);
    } catch(e) {
        return JSON.stringify({ error: e.message || e });
    }
})()`;

    const membersResult = await evaluate(membersScript);
    const members = JSON.parse(membersResult);
    
    if (members.error) {
        console.log('错误:', members.error);
        ws.close();
        return;
    }
    
    console.log('群成员总数:', members.length);
    
    // Find "logo" by decrypting nicknames
    console.log('\n2. 解密昵称查找 "logo"...');
    let targetAccount = null;
    
    for (const m of members) {
        let decryptedNick = null;
        
        // Try to decrypt from custom field
        if (m.custom) {
            try {
                const customData = JSON.parse(m.custom);
                const ciphertext = customData.nickname_ciphertext || customData.nicknameCiphertext;
                if (ciphertext) {
                    decryptedNick = decrypt(ciphertext);
                }
            } catch(e) {}
        }
        
        // Check if this is our target
        const nickToCheck = decryptedNick || m.nickInTeam || m.nick || '';
        
        if (nickToCheck.toLowerCase() === TARGET_NICK.toLowerCase()) {
            targetAccount = m.account;
            console.log('✅ 找到目标用户!');
            console.log('   账号:', m.account);
            console.log('   昵称:', decryptedNick || m.nick);
            console.log('   类型:', m.type);
            break;
        }
    }
    
    if (!targetAccount) {
        console.log('\n❌ 未找到昵称为 "logo" 的用户');
        console.log('\n显示部分成员昵称用于参考:');
        
        let count = 0;
        for (const m of members) {
            if (count >= 20) break;
            
            let decryptedNick = null;
            if (m.custom) {
                try {
                    const customData = JSON.parse(m.custom);
                    const ciphertext = customData.nickname_ciphertext || customData.nicknameCiphertext;
                    if (ciphertext) {
                        decryptedNick = decrypt(ciphertext);
                    }
                } catch(e) {}
            }
            
            const nick = decryptedNick || m.nickInTeam || m.nick || '(无)';
            console.log(`  ${m.account}: ${nick}`);
            count++;
        }
        
        ws.close();
        return;
    }
    
    // Kick the user
    console.log('\n3. 执行踢出操作...');
    const kickScript = `
(async function() {
    try {
        var result = await new Promise((resolve, reject) => {
            window.nim.removeTeamMembers({
                teamId: '${TEAM_ID}',
                accounts: ['${targetAccount}'],
                done: function(err, obj) {
                    if (err) reject(err);
                    else resolve(obj);
                }
            });
            setTimeout(() => reject('timeout'), 10000);
        });
        
        return JSON.stringify({
            success: true,
            result: result
        });
    } catch(e) {
        return JSON.stringify({ 
            success: false, 
            error: e.message || String(e) 
        });
    }
})()`;

    const kickResult = await evaluate(kickScript);
    console.log('踢出结果:', kickResult);
    
    const kick = JSON.parse(kickResult);
    if (kick.success) {
        console.log('\n✅✅✅ 踢出成功! ✅✅✅');
        console.log('用户 "logo" (账号:', targetAccount, ') 已被踢出群', TEAM_ID);
    } else {
        console.log('\n❌ 踢出失败:', kick.error);
    }
    
    ws.close();
    console.log('\n测试完成!');
}

findAndKickLogo().catch(console.error);
