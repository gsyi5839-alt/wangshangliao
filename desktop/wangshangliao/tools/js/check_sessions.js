/**
 * 检查当前会话信息
 */
const WebSocket = require('ws');
const http = require('http');

function httpGet(url) {
    return new Promise((resolve, reject) => {
        http.get(url, (res) => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', () => {
                try {
                    resolve(JSON.parse(data));
                } catch(e) {
                    reject(new Error('Invalid JSON'));
                }
            });
        }).on('error', reject);
    });
}

async function main() {
    console.log('Connecting to WangShangLiao...\n');
    
    const pages = await httpGet('http://localhost:9222/json');
    const page = pages.find(p => p.url && p.url.includes('wangshangliao'));
    
    if (!page) {
        console.log('WangShangLiao not found');
        return;
    }
    
    console.log('Current URL:', page.url);
    console.log('');
    
    const ws = new WebSocket(page.webSocketDebuggerUrl);
    let messageId = 1;
    const pending = new Map();
    
    ws.on('message', (data) => {
        const msg = JSON.parse(data);
        if (msg.id && pending.has(msg.id)) {
            pending.get(msg.id)(msg);
            pending.delete(msg.id);
        }
    });
    
    function sendCommand(method, params = {}) {
        return new Promise((resolve, reject) => {
            const id = messageId++;
            pending.set(id, resolve);
            ws.send(JSON.stringify({ id, method, params }));
            setTimeout(() => {
                if (pending.has(id)) {
                    pending.delete(id);
                    reject(new Error('Timeout'));
                }
            }, 10000);
        });
    }
    
    async function evaluate(expression, awaitPromise = true) {
        const result = await sendCommand('Runtime.evaluate', {
            expression,
            returnByValue: true,
            awaitPromise
        });
        return result.result?.result?.value;
    }
    
    await new Promise(resolve => ws.on('open', resolve));
    
    // 获取所有会话
    const script = `
(async function() {
    var result = {
        url: window.location.href,
        nimAvailable: !!window.nim,
        sessions: [],
        teams: []
    };
    
    if (!window.nim) return JSON.stringify(result, null, 2);
    
    // 获取会话列表
    try {
        var sessions = await new Promise((resolve, reject) => {
            window.nim.getLocalSessions({
                limit: 20,
                done: (err, data) => err ? reject(err) : resolve(data)
            });
            setTimeout(() => reject(new Error('timeout')), 5000);
        });
        
        result.sessions = (sessions || []).map(s => ({
            id: s.id,
            scene: s.scene,
            to: s.to,
            lastMsg: s.lastMsg ? s.lastMsg.text || '[非文本]' : ''
        }));
    } catch(e) {
        result.sessionError = e.message;
    }
    
    // 获取群列表
    try {
        // 从会话中提取群ID
        var teamIds = result.sessions
            .filter(s => s.id && s.id.startsWith('team-'))
            .map(s => s.id.replace('team-', ''));
        
        if (teamIds.length > 0) {
            var teams = await new Promise((resolve, reject) => {
                window.nim.getLocalTeams({
                    teamIds: teamIds,
                    done: (err, data) => err ? reject(err) : resolve(data)
                });
                setTimeout(() => reject(new Error('timeout')), 5000);
            });
            
            result.teams = (teams || []).map(t => ({
                teamId: t.teamId,
                name: t.name,
                memberNum: t.memberNum
            }));
        }
    } catch(e) {
        result.teamError = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})();`;

    const data = await evaluate(script);
    const parsed = JSON.parse(data);
    
    console.log('=== Current State ===\n');
    console.log('URL:', parsed.url);
    console.log('NIM Available:', parsed.nimAvailable);
    
    console.log('\n=== Sessions (最近会话) ===\n');
    if (parsed.sessions.length === 0) {
        console.log('No sessions found');
    } else {
        for (const s of parsed.sessions) {
            const type = s.id.startsWith('team-') ? '[群聊]' : '[私聊]';
            console.log(`${type} ${s.id}`);
        }
    }
    
    console.log('\n=== Teams (群聊) ===\n');
    if (parsed.teams.length === 0) {
        console.log('No teams found');
    } else {
        for (const t of parsed.teams) {
            console.log(`- ${t.name} (ID: ${t.teamId}, Members: ${t.memberNum})`);
        }
    }
    
    if (parsed.sessionError) {
        console.log('\nSession Error:', parsed.sessionError);
    }
    if (parsed.teamError) {
        console.log('\nTeam Error:', parsed.teamError);
    }
    
    ws.close();
}

main().catch(console.error);

