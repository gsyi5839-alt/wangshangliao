// Find and kick "logo" from team 3962369093 (法拉利福利 ③裙)
const WebSocket = require('ws');
const crypto = require('crypto');

const CDP_URL = 'ws://127.0.0.1:9222/devtools/page/8322EEB7A02952E8C4C59B59B616C299';
const TEAM_ID = '3962369093';
const TARGET_NICK = 'logo';

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

async function findAndKick() {
    console.log('=== 查找并踢出 "logo" ===');
    console.log('群: 法拉利福利 ③裙');
    console.log('群ID:', TEAM_ID);
    
    const ws = new WebSocket(CDP_URL);
    
    await new Promise((resolve, reject) => {
        ws.on('open', resolve);
        ws.on('error', reject);
    });
    
    console.log('\n已连接!');
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
    
    // Get members
    console.log('\n1. 获取群成员...');
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
        return JSON.stringify({ error: e.message || String(e) });
    }
})()`;

    const membersResult = await evaluate(membersScript);
    let members;
    
    try {
        members = JSON.parse(membersResult);
    } catch(e) {
        console.log('解析错误:', membersResult);
        ws.close();
        return;
    }
    
    if (members.error) {
        console.log('❌ 获取成员失败:', members.error);
        ws.close();
        return;
    }
    
    console.log('成员总数:', members.length);
    
    // Find logo
    console.log('\n2. 搜索 "logo"...');
    let targetAccount = null;
    let allNicks = [];
    
    for (const m of members) {
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
        
        const finalNick = decryptedNick || m.nickInTeam || m.nick || '';
        allNicks.push({ account: m.account, nick: finalNick, type: m.type });
        
        if (finalNick.toLowerCase() === TARGET_NICK.toLowerCase()) {
            targetAccount = m.account;
            console.log('✅ 找到 "logo"!');
            console.log('   账号:', m.account);
            console.log('   昵称:', finalNick);
            console.log('   类型:', m.type);
        }
    }
    
    if (!targetAccount) {
        console.log('\n❌ 未找到 "logo"');
        console.log('\n群内昵称列表 (有昵称的):');
        allNicks.filter(m => m.nick).slice(0, 30).forEach(m => {
            console.log(`  ${m.account}: "${m.nick}"`);
        });
        ws.close();
        return;
    }
    
    // Kick
    console.log('\n3. 执行踢出...');
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
        return JSON.stringify({ success: true });
    } catch(e) {
        return JSON.stringify({ success: false, error: e.message || String(e) });
    }
})()`;

    const kickResult = await evaluate(kickScript);
    const kick = JSON.parse(kickResult);
    
    if (kick.success) {
        console.log('\n✅✅✅ 踢出成功! ✅✅✅');
        console.log('用户 "logo" (账号:', targetAccount, ') 已被踢出!');
    } else {
        console.log('\n❌ 踢出失败:', kick.error);
    }
    
    ws.close();
}

findAndKick().catch(console.error);
