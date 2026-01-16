/**
 * 解密群成员昵称
 */
const WebSocket = require('ws');
const http = require('http');
const crypto = require('crypto');

// AES 解密配置
const KEY = Buffer.from('d6ba6647b7c43b79d0e42ceb2790e342', 'utf8');
const IV = Buffer.from('kgWRyiiODMjSCh0m', 'utf8');

function decrypt(ciphertext) {
    try {
        const decipher = crypto.createDecipheriv('aes-256-cbc', KEY, IV);
        let decrypted = decipher.update(ciphertext, 'base64', 'utf8');
        decrypted += decipher.final('utf8');
        return decrypted;
    } catch (e) {
        return null;
    }
}

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
    console.log('AES Key:', KEY.toString('utf8'));
    console.log('AES IV:', IV.toString('utf8'));
    console.log('');
    
    // 测试解密几个样本
    const samples = [
        'MIx+fLk8d82ReFVbEgQDXw==',
        'yBs2VqM3EXVVjCk4r7IwiA==',
        'tA0nPvVJ9XAy0/tapH9pew==',
        'xhl6IcmY4lqGknFKbZSPcw==',
        'zxG3QFhGdgEqePLbqgQ8Sg=='
    ];
    
    console.log('=== 测试解密样本 ===\n');
    samples.forEach(cipher => {
        const decrypted = decrypt(cipher);
        console.log(`${cipher} -> ${decrypted || 'FAILED'}`);
    });
    
    console.log('\nConnecting to WangShangLiao...\n');
    
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
    
    // 获取所有群成员并解密
    const script = `
(async function() {
    var result = {
        success: false,
        members: [],
        error: null
    };
    
    try {
        var teamId = '21654357327';
        
        if (window.nim) {
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
            
            // 提取所有有加密昵称的成员
            var ciphers = [];
            members.forEach(function(m) {
                if (m.custom) {
                    try {
                        var c = JSON.parse(m.custom);
                        if (c.nicknameCiphertext) {
                            ciphers.push({
                                account: m.account,
                                cipher: c.nicknameCiphertext
                            });
                        }
                    } catch(e) {}
                }
            });
            
            result.cipherCount = ciphers.length;
            result.ciphers = ciphers.slice(0, 100); // 只返回前100个
        }
        
        result.success = true;
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result);
})();`;

    const data = await evaluate(script);
    const parsed = JSON.parse(data);
    
    console.log('=== 批量解密群成员昵称 ===\n');
    console.log('群成员总数:', parsed.memberCount);
    console.log('有加密昵称的成员数:', parsed.cipherCount);
    console.log('');
    
    // 解密并显示
    console.log('解密结果 (前50个):\n');
    const results = [];
    for (let i = 0; i < Math.min(50, parsed.ciphers.length); i++) {
        const item = parsed.ciphers[i];
        const decrypted = decrypt(item.cipher);
        console.log(`${item.account} -> ${decrypted || 'FAILED'}`);
        if (decrypted) {
            results.push({ account: item.account, nickname: decrypted });
        }
    }
    
    console.log('\n成功解密:', results.length, '个昵称');
    
    ws.close();
}

main().catch(console.error);

