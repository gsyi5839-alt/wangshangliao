// Force kick account 82840376 directly
const WebSocket = require('ws');

const CDP_URL = 'ws://127.0.0.1:9222/devtools/page/8322EEB7A02952E8C4C59B59B616C299';
const TEAM_ID = '40821608989';
const ACCOUNT = '82840376';

async function forceKick() {
    console.log('=== 强制踢出账号 82840376 ===');
    console.log('群ID:', TEAM_ID);
    console.log('账号:', ACCOUNT);
    
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
    
    // Try to kick directly
    console.log('\n执行踢出...');
    const kickScript = `
(async function() {
    try {
        var result = await new Promise((resolve, reject) => {
            window.nim.removeTeamMembers({
                teamId: '${TEAM_ID}',
                accounts: ['${ACCOUNT}'],
                done: function(err, obj) {
                    if (err) {
                        resolve({ success: false, error: err });
                    } else {
                        resolve({ success: true, result: obj });
                    }
                }
            });
            setTimeout(() => resolve({ success: false, error: 'timeout' }), 15000);
        });
        return JSON.stringify(result);
    } catch(e) {
        return JSON.stringify({ success: false, error: e.message || String(e) });
    }
})()`;

    const kickResult = await evaluate(kickScript);
    console.log('踢出结果:', kickResult);
    
    const result = JSON.parse(kickResult);
    if (result.success) {
        console.log('\n✅✅✅ 踢出成功! ✅✅✅');
    } else {
        console.log('\n❌ 踢出失败');
        console.log('错误:', JSON.stringify(result.error, null, 2));
    }
    
    ws.close();
}

forceKick().catch(console.error);
