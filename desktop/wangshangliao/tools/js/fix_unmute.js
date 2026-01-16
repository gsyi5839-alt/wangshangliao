const WebSocket = require('ws');
const http = require('http');

http.get('http://127.0.0.1:9222/json', (res) => {
    let data = '';
    res.on('data', chunk => data += chunk);
    res.on('end', () => {
        const pages = JSON.parse(data);
        const mainPage = pages.find(p => p.url.includes('index.html'));
        const ws = new WebSocket(mainPage.webSocketDebuggerUrl);
        ws.on('open', () => {
            ws.send(JSON.stringify({
                id: 1,
                method: 'Runtime.evaluate',
                params: {
                    expression: `new Promise(r => window.nim.updateMuteStateInTeam({
                        teamId: '40821608989',
                        account: '1954086367',
                        mute: false,
                        done: (e, d) => r(e ? {error: e.message} : {success: true})
                    }))`,
                    returnByValue: true,
                    awaitPromise: true
                }
            }));
            ws.on('message', (data) => {
                const msg = JSON.parse(data.toString());
                if (msg.id === 1) {
                    console.log('解除禁言:', JSON.stringify(msg.result.result.value));
                    ws.close();
                    process.exit(0);
                }
            });
        });
    });
});

