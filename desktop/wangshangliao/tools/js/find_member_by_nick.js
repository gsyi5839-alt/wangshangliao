/**
 * 在群里搜索昵称包含"logo"的成员
 */
const WebSocket = require('ws');
const http = require('http');
const crypto = require('crypto');

let ws = null;
let msgId = 0;

// AES解密密钥（旺商聊昵称加密用）
const KEY = 'd6ba6647b7c43b79d0e42ceb2790e342';
const IV = 'kgWRyiiODMjSCh0m';

function decryptNick(ciphertext) {
    if (!ciphertext) return null;
    try {
        const decipher = crypto.createDecipheriv('aes-256-cbc', KEY, IV);
        let decrypted = decipher.update(ciphertext, 'base64', 'utf8');
        decrypted += decipher.final('utf8');
        return decrypted;
    } catch (e) {
        return ciphertext; // 返回原文
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
    console.log('🔍 搜索昵称包含"logo"的群成员\n');
    
    const wsUrl = await getWebSocketUrl();
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    console.log('✅ 已连接\n');
    
    // 获取所有群及其成员
    const script = `(async () => {
        var results = [];
        
        // 获取所有群
        var teams = await new Promise(r => {
            window.nim.getTeams({ done: (e, t) => r(t || []) });
            setTimeout(() => r([]), 5000);
        });
        
        for (var team of teams) {
            console.log('正在搜索群:', team.teamId);
            
            var membersResult = await new Promise(r => {
                window.nim.getTeamMembers({
                    teamId: team.teamId,
                    done: (err, obj) => {
                        if (err) r({ error: err.message });
                        else r({ members: obj?.members || [] });
                    }
                });
                setTimeout(() => r({ members: [] }), 15000);
            });
            
            var members = membersResult.members || [];
            
            for (var m of members) {
                // 检查各种昵称字段
                var nicks = [
                    m.nick,
                    m.nickInTeam,
                    m.alias
                ].filter(n => n);
                
                // 解析custom字段中的加密昵称
                if (m.custom) {
                    try {
                        var customObj = typeof m.custom === 'string' ? JSON.parse(m.custom) : m.custom;
                        if (customObj.nickname_ciphertext || customObj.nicknameCiphertext) {
                            nicks.push(customObj.nickname_ciphertext || customObj.nicknameCiphertext);
                        }
                    } catch(e) {}
                }
                
                results.push({
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
        
        return { total: results.length, members: results };
    })()`;
    
    console.log('正在获取所有群成员...\n');
    const result = await evaluate(script);
    console.log(`共获取 ${result?.total || 0} 名成员\n`);
    
    // 在本地搜索昵称
    const searchTerm = 'logo';
    const members = result?.members || [];
    const matched = [];
    
    console.log(`搜索昵称包含 "${searchTerm}" 的成员...\n`);
    
    for (const m of members) {
        let decryptedNick = null;
        let matchedField = null;
        
        // 检查原始昵称
        if (m.nick?.toLowerCase().includes(searchTerm)) {
            matchedField = 'nick';
        }
        if (m.nickInTeam?.toLowerCase().includes(searchTerm)) {
            matchedField = 'nickInTeam';
        }
        
        // 尝试解密昵称
        if (m.nickInTeam) {
            decryptedNick = decryptNick(m.nickInTeam);
            if (decryptedNick?.toLowerCase().includes(searchTerm)) {
                matchedField = 'nickInTeam(解密)';
            }
        }
        
        // 检查custom中的加密昵称
        if (m.custom) {
            try {
                const customObj = typeof m.custom === 'string' ? JSON.parse(m.custom) : m.custom;
                const cipher = customObj.nickname_ciphertext || customObj.nicknameCiphertext;
                if (cipher) {
                    const decrypted = decryptNick(cipher);
                    if (decrypted?.toLowerCase().includes(searchTerm)) {
                        decryptedNick = decrypted;
                        matchedField = 'custom(解密)';
                    }
                }
            } catch (e) {}
        }
        
        if (matchedField) {
            matched.push({
                ...m,
                decryptedNick,
                matchedField
            });
        }
    }
    
    if (matched.length > 0) {
        console.log(`✅ 找到 ${matched.length} 个匹配成员:\n`);
        matched.forEach((m, i) => {
            console.log(`${i + 1}. 账号: ${m.account}`);
            console.log(`   群ID: ${m.teamId}`);
            console.log(`   原始昵称: ${m.nick || m.nickInTeam || '无'}`);
            console.log(`   解密昵称: ${m.decryptedNick || '无'}`);
            console.log(`   匹配字段: ${m.matchedField}`);
            console.log(`   身份: ${m.type}`);
            console.log('');
        });
    } else {
        console.log('❌ 未找到匹配成员');
        
        // 显示一些示例成员
        console.log('\n📋 部分群成员示例 (前20个):');
        const sample = members.slice(0, 20);
        for (const m of sample) {
            let decrypted = null;
            if (m.nickInTeam) {
                decrypted = decryptNick(m.nickInTeam);
            }
            console.log(`  ${m.account}: ${m.nickInTeam || m.nick || '无'} → ${decrypted || ''}`);
        }
    }
    
    ws.close();
}

main().catch(console.error);
