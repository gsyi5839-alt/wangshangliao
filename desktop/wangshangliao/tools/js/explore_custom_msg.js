const WebSocket = require('ws');
const http = require('http');
const crypto = require('crypto');
const zlib = require('zlib');

// AES解密配置
const key = Buffer.from('d6ba6647b7c43b79d0e42ceb2790e342', 'utf8');
const iv = Buffer.from('kgWRyiiODMjSCh0m', 'utf8');

function decrypt(ciphertext) {
    try {
        const decipher = crypto.createDecipheriv('aes-256-cbc', key, iv);
        decipher.setAutoPadding(true);
        let decrypted = decipher.update(Buffer.from(ciphertext, 'base64'));
        decrypted = Buffer.concat([decrypted, decipher.final()]);
        return decrypted;
    } catch (e) {
        return null;
    }
}

async function main() {
    console.log('=== Exploring Custom Message Content ===\n');
    
    const targets = await new Promise((resolve, reject) => {
        http.get('http://127.0.0.1:9222/json', (res) => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', () => resolve(JSON.parse(data)));
        }).on('error', reject);
    });
    
    const pageTarget = targets.find(t => t.type === 'page' && t.url.includes('index.html'));
    if (!pageTarget) {
        console.log('Page not found');
        return;
    }
    
    const ws = new WebSocket(pageTarget.webSocketDebuggerUrl);
    let msgId = 1;
    const pending = new Map();
    
    ws.on('message', (data) => {
        const msg = JSON.parse(data.toString());
        if (msg.id && pending.has(msg.id)) {
            pending.get(msg.id)(msg);
            pending.delete(msg.id);
        }
    });
    
    await new Promise(resolve => ws.on('open', resolve));
    
    async function evaluate(expression, awaitPromise = false) {
        return new Promise((resolve) => {
            const id = msgId++;
            const timeout = setTimeout(() => {
                pending.delete(id);
                resolve(null);
            }, 15000);
            
            pending.set(id, (result) => {
                clearTimeout(timeout);
                if (result.result && result.result.result && result.result.result.value !== undefined) {
                    resolve(result.result.result.value);
                } else {
                    resolve(null);
                }
            });
            
            ws.send(JSON.stringify({
                id,
                method: 'Runtime.evaluate',
                params: { expression, returnByValue: true, awaitPromise }
            }));
        });
    }
    
    // 获取custom类型消息的content.b字段
    console.log('=== Custom Messages Content.b Analysis ===\n');
    const customMsgsJson = await evaluate(`
        new Promise(function(r) {
            window.nim.getLocalMsgs({
                sessionId: 'team-40821608989',
                limit: 100,
                done: function(e, obj) {
                    if (e) { r('Error'); return; }
                    var msgs = obj.msgs || obj || [];
                    var customMsgs = msgs.filter(function(m) { return m.type === 'custom' && m.content; });
                    r(JSON.stringify(customMsgs.slice(0, 10).map(function(m) {
                        var content = null;
                        try { content = JSON.parse(m.content); } catch(ex){}
                        return {
                            from: m.from,
                            fromNick: m.fromNick,
                            contentB: content ? content.b : null,
                            contentKeys: content ? Object.keys(content) : [],
                            time: m.time
                        };
                    })));
                }
            });
        })
    `, true);
    
    if (customMsgsJson) {
        const customMsgs = JSON.parse(customMsgsJson);
        customMsgs.forEach((m, idx) => {
            console.log(`--- Custom Message ${idx + 1} ---`);
            console.log(`From: ${m.from}`);
            console.log(`FromNick: ${m.fromNick}`);
            console.log(`Content Keys: ${m.contentKeys.join(', ')}`);
            
            if (m.contentB) {
                console.log(`Content.b (first 60 chars): ${m.contentB.substring(0, 60)}...`);
                console.log(`Content.b length: ${m.contentB.length}`);
                
                // 尝试Base64解码
                try {
                    const decoded = Buffer.from(m.contentB, 'base64');
                    console.log(`Base64 decoded length: ${decoded.length} bytes`);
                    console.log(`First 20 bytes hex: ${decoded.slice(0, 20).toString('hex')}`);
                    
                    // 检查是否是protobuf或其他二进制格式
                    const firstByte = decoded[0];
                    console.log(`First byte: 0x${firstByte.toString(16)} (${firstByte})`);
                    
                    // 尝试AES解密
                    const aesDecrypted = decrypt(m.contentB);
                    if (aesDecrypted) {
                        console.log(`AES decrypted: ${aesDecrypted.toString('utf8').substring(0, 100)}`);
                    } else {
                        console.log(`AES decrypt failed`);
                    }
                    
                    // 尝试gzip解压
                    try {
                        const gunzipped = zlib.gunzipSync(decoded);
                        console.log(`Gzip decompressed: ${gunzipped.toString('utf8').substring(0, 100)}`);
                    } catch(e) {
                        // 不是gzip
                    }
                    
                    // 尝试inflate解压
                    try {
                        const inflated = zlib.inflateSync(decoded);
                        console.log(`Inflate decompressed: ${inflated.toString('utf8').substring(0, 100)}`);
                    } catch(e) {
                        // 不是deflate
                    }
                    
                } catch(e) {
                    console.log(`Base64 decode error: ${e.message}`);
                }
            }
            console.log('');
        });
    }
    
    // 探索text类型消息
    console.log('\n=== Text Messages ===\n');
    const textMsgsJson = await evaluate(`
        new Promise(function(r) {
            window.nim.getLocalMsgs({
                sessionId: 'team-40821608989',
                limit: 100,
                done: function(e, obj) {
                    if (e) { r('Error'); return; }
                    var msgs = obj.msgs || obj || [];
                    var textMsgs = msgs.filter(function(m) { return m.type === 'text' && m.text; });
                    r(JSON.stringify(textMsgs.slice(0, 10).map(function(m) {
                        return {
                            from: m.from,
                            fromNick: m.fromNick,
                            text: m.text,
                            time: new Date(m.time).toLocaleString()
                        };
                    }), null, 2));
                }
            });
        })
    `, true);
    console.log(textMsgsJson || 'No text messages');
    
    // 探索notification类型消息
    console.log('\n=== Notification Messages ===\n');
    const notifMsgsJson = await evaluate(`
        new Promise(function(r) {
            window.nim.getLocalMsgs({
                sessionId: 'team-40821608989',
                limit: 100,
                done: function(e, obj) {
                    if (e) { r('Error'); return; }
                    var msgs = obj.msgs || obj || [];
                    var notifMsgs = msgs.filter(function(m) { return m.type === 'notification'; });
                    r(JSON.stringify(notifMsgs.slice(0, 5).map(function(m) {
                        return {
                            from: m.from,
                            attach: m.attach,
                            time: new Date(m.time).toLocaleString()
                        };
                    }), null, 2));
                }
            });
        })
    `, true);
    console.log(notifMsgsJson || 'No notification messages');
    
    // 探索源代码中的消息解密函数
    console.log('\n=== Search for Message Decrypt in Source ===\n');
    const decryptFuncs = await evaluate(`
        (function() {
            var funcs = [];
            // 搜索全局作用域
            for (var key in window) {
                if (key.toLowerCase().includes('decrypt') || key.toLowerCase().includes('decode')) {
                    funcs.push(key + ': ' + typeof window[key]);
                }
            }
            return funcs.join('\\n') || 'None found';
        })()
    `);
    console.log(decryptFuncs);
    
    ws.close();
    console.log('\n=== Done ===');
}

main().catch(function(e) { console.error('Error:', e.message); });

