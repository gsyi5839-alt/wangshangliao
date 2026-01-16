/**
 * 列出所有群成员及账号，帮助查找正确的账号
 */
const WebSocket = require('ws');
const http = require('http');
const crypto = require('crypto');

let ws = null;
let msgId = 0;

// MD5解密尝试（逆向查找）
function tryDecryptMD5(hash) {
    // 常见昵称测试
    const common = ['logo', 'Logo', 'LOGO', '测试', 'admin', 'bot', '机器人'];
    for (const word of common) {
        if (crypto.createHash('md5').update(word).digest('hex') === hash) {
            return word;
        }
    }
    return null;
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
    console.log('📋 列出所有群和成员\n');
    
    const wsUrl = await getWebSocketUrl();
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    console.log('✅ 已连接\n');
    
    // 获取所有群
    const script = `(async () => {
        var result = { teams: [], members: [] };
        
        var teams = await new Promise(r => {
            window.nim.getTeams({ done: (e, t) => r(t || []) });
            setTimeout(() => r([]), 5000);
        });
        
        result.teams = teams.map(t => ({ teamId: t.teamId, name: t.name, memberNum: t.memberNum }));
        
        for (var team of teams) {
            var members = await new Promise(r => {
                window.nim.getTeamMembers({
                    teamId: team.teamId,
                    done: (err, obj) => r(obj?.members || [])
                });
                setTimeout(() => r([]), 15000);
            });
            
            for (var m of members) {
                result.members.push({
                    teamId: team.teamId,
                    teamName: team.name,
                    account: m.account,
                    nick: m.nick,
                    nickInTeam: m.nickInTeam,
                    type: m.type
                });
            }
        }
        
        return result;
    })()`;
    
    console.log('获取群列表和成员...\n');
    const result = await evaluate(script);
    
    console.log('=== 群列表 ===\n');
    (result?.teams || []).forEach((t, i) => {
        console.log(`${i + 1}. 群ID: ${t.teamId}`);
        console.log(`   群名: ${t.name || '无'}`);
        console.log(`   成员数: ${t.memberNum}`);
        console.log('');
    });
    
    // 按群分组显示成员
    const membersByTeam = {};
    (result?.members || []).forEach(m => {
        if (!membersByTeam[m.teamId]) {
            membersByTeam[m.teamId] = [];
        }
        membersByTeam[m.teamId].push(m);
    });
    
    console.log('\n=== 成员列表（按群分组） ===\n');
    
    for (const teamId of Object.keys(membersByTeam)) {
        const members = membersByTeam[teamId];
        const teamInfo = result?.teams?.find(t => t.teamId === teamId);
        console.log(`\n【群 ${teamId} - ${teamInfo?.name || '未知'}】 (${members.length}人)\n`);
        console.log('账号 (10位)   | 群昵称/昵称');
        console.log('--------------------------------------------');
        
        // 显示所有成员
        members.forEach(m => {
            const displayNick = m.nickInTeam || m.nick || '无';
            // 尝试解密MD5昵称
            const decrypted = tryDecryptMD5(displayNick);
            const nickDisplay = decrypted ? `${displayNick} → "${decrypted}"` : displayNick;
            console.log(`${m.account} | ${nickDisplay}`);
        });
    }
    
    // 搜索包含8的账号
    console.log('\n\n=== 包含 "82840376" 的搜索结果 ===\n');
    const searchTerm = '82840376';
    const matching = (result?.members || []).filter(m => 
        m.account?.includes(searchTerm) || 
        m.nick?.includes(searchTerm) || 
        m.nickInTeam?.includes(searchTerm)
    );
    
    if (matching.length > 0) {
        console.log('找到匹配成员:');
        matching.forEach(m => {
            console.log(`  账号: ${m.account}`);
            console.log(`  群: ${m.teamId}`);
            console.log(`  昵称: ${m.nickInTeam || m.nick}`);
            console.log('');
        });
    } else {
        console.log('未找到匹配成员');
        console.log('');
        console.log('⚠️ 请检查账号是否正确，或者提供群昵称搜索');
    }
    
    ws.close();
}

main().catch(console.error);
