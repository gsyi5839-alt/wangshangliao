// List all teams the bot has joined
const WebSocket = require('ws');

const CDP_URL = 'ws://127.0.0.1:9222/devtools/page/8322EEB7A02952E8C4C59B59B616C299';

async function listTeams() {
    console.log('=== 机器人已加入的群列表 ===\n');
    
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
    
    // Get current account info
    const accountScript = `
(function() {
    if (window.nim) {
        return JSON.stringify({
            account: window.nim.account,
            connected: window.nim.connected
        });
    }
    return JSON.stringify({error: 'nim not found'});
})()`;

    const accountResult = await evaluate(accountScript);
    console.log('当前账号:', accountResult);
    
    // Get all teams
    const teamsScript = `
(async function() {
    try {
        var result = await new Promise((resolve, reject) => {
            window.nim.getTeams({
                done: function(err, obj) {
                    if (err) reject(err);
                    else resolve(obj);
                }
            });
            setTimeout(() => reject('timeout'), 10000);
        });
        
        var teams = result.teams || result || [];
        
        // Get team details
        var teamList = teams.map(t => ({
            teamId: t.teamId,
            name: t.name,
            memberNum: t.memberNum,
            owner: t.owner
        }));
        
        return JSON.stringify({
            total: teams.length,
            teams: teamList
        }, null, 2);
    } catch(e) {
        return JSON.stringify({ error: e.message || e });
    }
})()`;

    const teamsResult = await evaluate(teamsScript);
    const data = JSON.parse(teamsResult);
    
    console.log('\n群总数:', data.total);
    console.log('\n群ID           | 人数  | 群名称');
    console.log('---------------|-------|--------');
    
    if (data.teams) {
        for (const t of data.teams) {
            console.log(`${t.teamId.padEnd(14)} | ${String(t.memberNum || 0).padEnd(5)} | ${t.name || '(未命名)'}`);
        }
    }
    
    // Check if target team exists
    console.log('\n--- 检查目标群 3962369093 ---');
    const found = data.teams?.find(t => t.teamId === '3962369093');
    if (found) {
        console.log('✅ 找到目标群:', found);
    } else {
        console.log('❌ 机器人未加入群 3962369093');
    }
    
    ws.close();
}

listTeams().catch(console.error);
