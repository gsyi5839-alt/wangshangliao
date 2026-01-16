// Find and kick "logo" - match DOM nickname with NIM members
const WebSocket = require('ws');
const crypto = require('crypto');

const CDP_URL = 'ws://127.0.0.1:9222/devtools/page/8322EEB7A02952E8C4C59B59B616C299';
const TEAM_ID = '40821608989';  // From URL
const TARGET_NICK = 'logo';
const TARGET_ACCOUNT = '82840376';  // User provided this

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

async function kickLogo() {
    console.log('=== 踢出 "logo" (账号: 82840376) ===');
    console.log('群ID:', TEAM_ID);
    
    const ws = new WebSocket(CDP_URL);
    
    await new Promise((resolve, reject) => {
        ws.on('open', resolve);
        ws.on('error', reject);
    });
    
    console.log('已连接!');
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
    
    // First verify the member exists
    console.log('\n1. 验证账号 82840376 是否存在...');
    const verifyScript = `
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
        var target = members.find(m => m.account === '${TARGET_ACCOUNT}');
        
        if (target) {
            return JSON.stringify({
                found: true,
                account: target.account,
                nick: target.nick,
                type: target.type,
                custom: target.custom
            });
        }
        
        // Also search by decrypted nickname
        var logoMembers = [];
        for (var i = 0; i < members.length; i++) {
            var m = members[i];
            var nickDecrypted = null;
            if (m.custom) {
                try {
                    var customData = JSON.parse(m.custom);
                    var cipher = customData.nickname_ciphertext || customData.nicknameCiphertext;
                    if (cipher) {
                        // Will decrypt in Node.js
                        logoMembers.push({
                            account: m.account,
                            nick: m.nick,
                            custom: m.custom,
                            type: m.type
                        });
                    }
                } catch(e) {}
            }
        }
        
        return JSON.stringify({
            found: false,
            totalMembers: members.length,
            membersWithCustom: logoMembers.length,
            sample: logoMembers.slice(0, 200)
        });
    } catch(e) {
        return JSON.stringify({ error: e.message || String(e) });
    }
})()`;

    const verifyResult = await evaluate(verifyScript);
    console.log('验证结果:', verifyResult.substring(0, 500));
    
    let result = JSON.parse(verifyResult);
    
    if (result.error) {
        console.log('❌ 错误:', result.error);
        ws.close();
        return;
    }
    
    let accountToKick = null;
    
    if (result.found) {
        accountToKick = result.account;
        console.log('✅ 找到账号 82840376');
        console.log('   昵称:', result.nick);
        console.log('   类型:', result.type);
    } else {
        console.log('账号 82840376 不在成员列表中');
        console.log('正在解密昵称搜索 "logo"...');
        
        // Search by decrypted nickname
        if (result.sample) {
            for (const m of result.sample) {
                if (m.custom) {
                    try {
                        const customData = JSON.parse(m.custom);
                        const cipher = customData.nickname_ciphertext || customData.nicknameCiphertext;
                        if (cipher) {
                            const decrypted = decrypt(cipher);
                            if (decrypted && decrypted.toLowerCase() === TARGET_NICK.toLowerCase()) {
                                accountToKick = m.account;
                                console.log('✅ 通过昵称找到 "logo"!');
                                console.log('   账号:', m.account);
                                console.log('   解密昵称:', decrypted);
                                break;
                            }
                        }
                    } catch(e) {}
                }
            }
        }
    }
    
    if (!accountToKick) {
        console.log('\n❌ 未找到 "logo" 用户');
        ws.close();
        return;
    }
    
    // Kick the user
    console.log('\n2. 执行踢出...');
    console.log('   踢出账号:', accountToKick);
    
    const kickScript = `
(async function() {
    try {
        var result = await new Promise((resolve, reject) => {
            window.nim.removeTeamMembers({
                teamId: '${TEAM_ID}',
                accounts: ['${accountToKick}'],
                done: function(err, obj) {
                    if (err) reject(err);
                    else resolve(obj);
                }
            });
            setTimeout(() => reject('timeout'), 15000);
        });
        return JSON.stringify({ success: true, result: result });
    } catch(e) {
        return JSON.stringify({ success: false, error: e.message || String(e) });
    }
})()`;

    const kickResult = await evaluate(kickScript);
    console.log('踢出结果:', kickResult);
    
    const kick = JSON.parse(kickResult);
    if (kick.success) {
        console.log('\n✅✅✅ 踢出成功! ✅✅✅');
        console.log('用户 "logo" (账号:', accountToKick, ') 已被踢出!');
    } else {
        console.log('\n❌ 踢出失败:', kick.error);
    }
    
    ws.close();
}

kickLogo().catch(console.error);
