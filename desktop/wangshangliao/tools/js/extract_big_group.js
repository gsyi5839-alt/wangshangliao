/**
 * 从大群中提取昵称
 */
const WebSocket = require('ws');
const http = require('http');

function httpGet(url) {
    return new Promise((resolve, reject) => {
        http.get(url, (res) => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', () => {
                try { resolve(JSON.parse(data)); }
                catch(e) { reject(new Error('Invalid JSON')); }
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
            }, 60000);
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
    console.log('Connected!\n');
    
    // 从大群提取昵称
    const script = `
(async function() {
    var result = {
        success: false,
        nicknames: {},
        error: null
    };
    
    try {
        // 使用大群的 teamId: 21654357327
        var teamId = '21654357327';
        
        if (window.nim) {
            // 获取最近的消息
            var msgs = await new Promise(function(resolve, reject) {
                window.nim.getLocalMsgs({
                    sessionId: 'team-' + teamId,
                    limit: 1000,
                    done: function(err, obj) {
                        if (err) reject(err);
                        else resolve(obj);
                    }
                });
                setTimeout(function() { reject(new Error('timeout')); }, 30000);
            });
            
            var msgList = msgs.msgs || msgs || [];
            result.msgCount = msgList.length;
            
            // 提取发送者昵称
            var nickMap = {};
            msgList.forEach(function(m) {
                if (m.from && m.fromNick) {
                    // 检查昵称是否是 MD5 (32位十六进制)
                    var isMd5 = /^[a-f0-9]{32}$/i.test(m.fromNick);
                    if (!isMd5) {
                        nickMap[m.from] = m.fromNick;
                    }
                }
            });
            
            result.nicknames = nickMap;
            result.nicknameCount = Object.keys(nickMap).length;
            
            // 获取群成员并解密
            var teamData = await new Promise(function(resolve, reject) {
                window.nim.getTeamMembers({
                    teamId: teamId,
                    done: function(err, obj) {
                        if (err) reject(err);
                        else resolve(obj);
                    }
                });
                setTimeout(function() { reject(new Error('timeout')); }, 30000);
            });
            
            var members = teamData.members || teamData || [];
            result.memberCount = members.length;
            
            // 统计有 nicknameCiphertext 的成员
            var cipherCount = 0;
            var sampleCiphers = [];
            members.forEach(function(m) {
                if (m.custom) {
                    try {
                        var c = JSON.parse(m.custom);
                        if (c.nicknameCiphertext) {
                            cipherCount++;
                            if (sampleCiphers.length < 10) {
                                sampleCiphers.push({
                                    account: m.account,
                                    cipher: c.nicknameCiphertext
                                });
                            }
                        }
                    } catch(e) {}
                }
            });
            
            result.cipherCount = cipherCount;
            result.sampleCiphers = sampleCiphers;
        }
        
        result.success = true;
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})();`;

    const data = await evaluate(script);
    console.log('=== 大群数据 ===\n');
    const parsed = JSON.parse(data);
    console.log('消息数:', parsed.msgCount);
    console.log('群成员数:', parsed.memberCount);
    console.log('有加密昵称的成员数:', parsed.cipherCount);
    console.log('从消息中提取的明文昵称数:', parsed.nicknameCount);
    console.log('\n明文昵称示例:');
    console.log(JSON.stringify(parsed.nicknames, null, 2));
    console.log('\n加密昵称示例:');
    console.log(JSON.stringify(parsed.sampleCiphers, null, 2));
    
    ws.close();
}

main().catch(console.error);

