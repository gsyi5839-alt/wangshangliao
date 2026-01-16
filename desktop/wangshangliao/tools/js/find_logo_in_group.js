/**
 * 在群 40821608989 中查找 logo 成员的真实账号
 */
const WebSocket = require('ws');
const http = require('http');
const crypto = require('crypto');

let ws = null;
let msgId = 0;

// 计算 "logo" 的 MD5
const logoMD5 = crypto.createHash('md5').update('logo').digest('hex');
console.log('logo 的 MD5:', logoMD5);

async function getWebSocketUrl() {
    return new Promise((resolve, reject) => {
        const req = http.get('http://127.0.0.1:9222/json', (res) => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', () => {
                const pages = JSON.parse(data);
                const mainPage = pages.find(p => p.url?.includes('index.html')) || pages[0];
                resolve(mainPage?.webSocketDebuggerUrl);
            });
        });
        req.on('error', reject);
    });
}

function evaluate(expression, awaitPromise = true) {
    return new Promise((resolve, reject) => {
        const id = ++msgId;
        const timeout = setTimeout(() => reject(new Error('Timeout')), 30000);
        const handler = (data) => {
            const msg = JSON.parse(data.toString());
            if (msg.id === id) {
                clearTimeout(timeout);
                ws.off('message', handler);
                resolve(msg.result?.result?.value);
            }
        };
        ws.on('message', handler);
        ws.send(JSON.stringify({ id, method: 'Runtime.evaluate', params: { expression, awaitPromise, returnByValue: true } }));
    });
}

async function main() {
    console.log('\n🔍 在群 40821608989 中查找 logo 成员\n');
    
    const wsUrl = await getWebSocketUrl();
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    console.log('✅ 已连接\n');
    
    // 获取目标群的所有成员
    const script = `(async () => {
        var result = { members: [], admins: [] };
        
        var members = await new Promise(r => {
            window.nim.getTeamMembers({
                teamId: '40821608989',
                done: (err, obj) => {
                    if (err) r({ error: err.message });
                    else r(obj?.members || []);
                }
            });
            setTimeout(() => r([]), 15000);
        });
        
        // 找出管理员和群主
        for (var m of members) {
            var info = {
                account: m.account,
                nick: m.nick,
                nickInTeam: m.nickInTeam,
                type: m.type,  // owner, manager, normal
                custom: m.custom
            };
            
            if (m.type === 'owner' || m.type === 'manager') {
                result.admins.push(info);
            }
            result.members.push(info);
        }
        
        return result;
    })()`;
    
    const result = await evaluate(script);
    
    console.log('=== 群主和管理员 ===\n');
    (result?.admins || []).forEach((m, i) => {
        console.log(`${i + 1}. 账号: ${m.account}`);
        console.log(`   昵称: ${m.nickInTeam || m.nick || '无'}`);
        console.log(`   身份: ${m.type === 'owner' ? '👑 群主' : '⭐ 管理员'}`);
        console.log('');
    });
    
    // 查找 logo（MD5匹配）
    console.log('=== 查找 logo ===\n');
    const logoMD5 = '96d6f2e7e1f705ab5e59c84a6dc009b2'; // MD5("logo")
    
    const logoMember = (result?.members || []).find(m => 
        m.nickInTeam === logoMD5 || 
        m.nick === logoMD5 ||
        m.nickInTeam?.toLowerCase() === 'logo' ||
        m.nick?.toLowerCase() === 'logo'
    );
    
    if (logoMember) {
        console.log('✅ 找到 logo:');
        console.log('   NIM账号:', logoMember.account);
        console.log('   群昵称:', logoMember.nickInTeam);
        console.log('   昵称:', logoMember.nick);
        console.log('   身份:', logoMember.type);
    } else {
        console.log('❌ 未找到 logo（MD5匹配）');
        console.log('\n显示所有管理员和群主的账号:');
        (result?.admins || []).forEach(m => {
            console.log(`  ${m.account} | ${m.nickInTeam || m.nick}`);
        });
    }
    
    // 测试向管理员发送私聊
    console.log('\n=== 测试向管理员发送私聊 ===\n');
    
    for (const admin of (result?.admins || [])) {
        if (admin.type === 'owner') continue; // 跳过群主
        
        console.log(`测试账号: ${admin.account}`);
        
        const sendScript = `(async () => {
            return new Promise(r => {
                window.nim.sendText({
                    scene: 'p2p',
                    to: '${admin.account}',
                    text: '[机器人测试] 私聊消息 ${new Date().toLocaleTimeString()}',
                    done: (err, msg) => {
                        if (err) r({ success: false, error: err.message, code: err.code });
                        else r({ success: true, idServer: msg?.idServer, to: msg?.to });
                    }
                });
                setTimeout(() => r({ success: false, error: 'Timeout' }), 8000);
            });
        })()`;
        
        const sendResult = await evaluate(sendScript);
        if (sendResult?.success) {
            console.log('✅ 私聊发送成功!');
            console.log('   目标:', sendResult.to);
            console.log('   消息ID:', sendResult.idServer);
        } else {
            console.log('❌ 发送失败:', sendResult?.error);
        }
        console.log('');
    }
    
    // 获取当前登录账号
    console.log('=== 当前登录账号 ===\n');
    const myInfo = await evaluate(`(async () => {
        return new Promise(r => {
            window.nim.getMyInfo({ done: (e, i) => r(i || {}) });
            setTimeout(() => r({}), 5000);
        });
    })()`);
    console.log('当前账号:', myInfo?.account);
    console.log('昵称:', myInfo?.nick);
    
    ws.close();
}

main().catch(console.error);
