/**
 * 搜索昵称为"logo"的成员 - 使用MD5匹配
 */
const WebSocket = require('ws');
const http = require('http');
const crypto = require('crypto');

let ws = null;
let msgId = 0;

// 计算MD5
function md5(str) {
    return crypto.createHash('md5').update(str).digest('hex');
}

// AES解密（旺商聊加密昵称）
const KEY = 'd6ba6647b7c43b79d0e42ceb2790e342';
const IV = 'kgWRyiiODMjSCh0m';

function decryptAES(ciphertext) {
    if (!ciphertext) return null;
    try {
        const decipher = crypto.createDecipheriv('aes-256-cbc', KEY, IV);
        let decrypted = decipher.update(ciphertext, 'base64', 'utf8');
        decrypted += decipher.final('utf8');
        return decrypted;
    } catch (e) {
        return null;
    }
}

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
        const timeout = setTimeout(() => reject(new Error('Timeout')), 60000);
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
    console.log('🔍 搜索昵称为"logo"的成员\n');
    
    // 计算可能的MD5值
    const searchTerms = ['logo', 'Logo', 'LOGO'];
    console.log('MD5哈希值:');
    searchTerms.forEach(t => console.log(`  "${t}" → ${md5(t)}`));
    const md5Hashes = searchTerms.map(t => md5(t));
    
    const wsUrl = await getWebSocketUrl();
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    console.log('\n✅ 已连接\n');
    
    // 获取所有群成员
    console.log('正在获取所有群成员...');
    const script = `(async () => {
        var allMembers = [];
        var teams = await new Promise(r => {
            window.nim.getTeams({ done: (e, t) => r(t || []) });
            setTimeout(() => r([]), 5000);
        });
        
        for (var team of teams) {
            var result = await new Promise(r => {
                window.nim.getTeamMembers({
                    teamId: team.teamId,
                    done: (err, obj) => r(obj?.members || [])
                });
                setTimeout(() => r([]), 15000);
            });
            
            for (var m of result) {
                allMembers.push({
                    teamId: team.teamId,
                    teamName: team.name,
                    account: m.account,
                    nick: m.nick,
                    nickInTeam: m.nickInTeam,
                    type: m.type,
                    custom: m.custom
                });
            }
        }
        return allMembers;
    })()`;
    
    const members = await evaluate(script) || [];
    console.log(`共 ${members.length} 名成员\n`);
    
    // 搜索匹配的成员
    const matched = [];
    
    for (const m of members) {
        let matchReason = null;
        let plainNick = null;
        
        // 1. 检查nickInTeam是否匹配MD5
        if (m.nickInTeam && md5Hashes.includes(m.nickInTeam)) {
            const idx = md5Hashes.indexOf(m.nickInTeam);
            matchReason = `nickInTeam MD5匹配 "${searchTerms[idx]}"`;
            plainNick = searchTerms[idx];
        }
        
        // 2. 检查nick是否匹配MD5
        if (m.nick && md5Hashes.includes(m.nick)) {
            const idx = md5Hashes.indexOf(m.nick);
            matchReason = `nick MD5匹配 "${searchTerms[idx]}"`;
            plainNick = searchTerms[idx];
        }
        
        // 3. 检查nickInTeam是否是Base64加密的昵称
        if (m.nickInTeam && !matchReason) {
            const decrypted = decryptAES(m.nickInTeam);
            if (decrypted && searchTerms.some(t => decrypted.toLowerCase().includes(t.toLowerCase()))) {
                matchReason = 'nickInTeam AES解密匹配';
                plainNick = decrypted;
            }
        }
        
        // 4. 检查custom中的加密昵称
        if (m.custom && !matchReason) {
            try {
                const customObj = typeof m.custom === 'string' ? JSON.parse(m.custom) : m.custom;
                const cipher = customObj.nickname_ciphertext || customObj.nicknameCiphertext;
                if (cipher) {
                    const decrypted = decryptAES(cipher);
                    if (decrypted && searchTerms.some(t => decrypted.toLowerCase().includes(t.toLowerCase()))) {
                        matchReason = 'custom AES解密匹配';
                        plainNick = decrypted;
                    }
                }
            } catch (e) {}
        }
        
        // 5. 直接匹配原文
        if (!matchReason) {
            if (m.nickInTeam && searchTerms.some(t => m.nickInTeam.toLowerCase().includes(t.toLowerCase()))) {
                matchReason = 'nickInTeam 直接匹配';
                plainNick = m.nickInTeam;
            }
            if (m.nick && searchTerms.some(t => m.nick.toLowerCase().includes(t.toLowerCase()))) {
                matchReason = 'nick 直接匹配';
                plainNick = m.nick;
            }
        }
        
        if (matchReason) {
            matched.push({ ...m, matchReason, plainNick });
        }
    }
    
    if (matched.length > 0) {
        console.log(`✅ 找到 ${matched.length} 个匹配成员:\n`);
        matched.forEach((m, i) => {
            console.log(`${i + 1}. 账号: ${m.account}`);
            console.log(`   群ID: ${m.teamId}`);
            console.log(`   显示昵称: ${m.plainNick || '未知'}`);
            console.log(`   原始值: ${m.nickInTeam || m.nick}`);
            console.log(`   匹配原因: ${m.matchReason}`);
            console.log(`   身份: ${m.type}`);
            console.log('');
        });
        
        // 测试发送消息
        if (matched.length > 0) {
            const target = matched[0];
            console.log('\n📤 测试向该成员发送私聊消息...\n');
            
            const sendScript = `(async () => {
                return new Promise(r => {
                    window.nim.sendText({
                        scene: 'p2p',
                        to: '${target.account}',
                        text: '[测试] 这是一条测试私聊消息',
                        done: (err, msg) => {
                            if (err) r({ success: false, error: err.message, code: err.code });
                            else r({ success: true, idServer: msg?.idServer });
                        }
                    });
                    setTimeout(() => r({ success: false, error: 'Timeout' }), 8000);
                });
            })()`;
            
            const sendResult = await evaluate(sendScript);
            if (sendResult?.success) {
                console.log('✅ 私聊消息发送成功!');
            } else {
                console.log('❌ 私聊消息发送失败:', sendResult?.error);
                console.log('   错误码:', sendResult?.code);
            }
        }
    } else {
        console.log('❌ 未找到昵称为"logo"的成员\n');
        
        // 显示账号格式参考
        console.log('📋 群成员账号格式参考（前30个）:');
        members.slice(0, 30).forEach(m => {
            console.log(`  ${m.account} | ${m.nickInTeam || m.nick || '无昵称'}`);
        });
        
        console.log('\n⚠️ 请确认账号格式是否正确（旺商聊账号通常是10位数字）');
    }
    
    ws.close();
}

main().catch(console.error);
