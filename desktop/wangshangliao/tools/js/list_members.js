// List all team members
const WebSocket = require('ws');

const CDP_URL = 'ws://127.0.0.1:9222/devtools/page/8322EEB7A02952E8C4C59B59B616C299';
const TEAM_ID = '40821608989';

async function listMembers() {
    console.log('=== 群成员列表 ===');
    console.log('群ID:', TEAM_ID);
    
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
    
    const listScript = `
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
        
        // Sort by type (owner first, then manager, then normal)
        var typeOrder = { owner: 0, manager: 1, normal: 2 };
        members.sort((a, b) => (typeOrder[a.type] || 2) - (typeOrder[b.type] || 2));
        
        // Get first 30 members
        var sample = members.slice(0, 30).map(m => ({
            account: m.account,
            nick: m.nick,
            type: m.type // owner, manager, normal
        }));
        
        return JSON.stringify({
            total: members.length,
            members: sample
        }, null, 2);
    } catch(e) {
        return JSON.stringify({ error: e.message || e });
    }
})()`;

    const result = await evaluate(listScript);
    console.log('\n群成员 (前30人):');
    
    const data = JSON.parse(result);
    console.log('总人数:', data.total);
    console.log('\n账号           | 类型     | 昵称');
    console.log('---------------|----------|------');
    
    for (const m of data.members) {
        const typeLabel = m.type === 'owner' ? '群主' : (m.type === 'manager' ? '管理员' : '成员');
        console.log(`${m.account.padEnd(14)} | ${typeLabel.padEnd(8)} | ${m.nick || '(无昵称)'}`);
    }
    
    ws.close();
}

listMembers().catch(console.error);
