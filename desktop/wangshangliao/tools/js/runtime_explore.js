const WebSocket = require('ws');
const http = require('http');

async function explore() {
    console.log('Connecting to WangShangLiao...');
    
    // 获取调试目标
    const targets = await new Promise((resolve, reject) => {
        http.get('http://127.0.0.1:9222/json', (res) => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', () => resolve(JSON.parse(data)));
        }).on('error', reject);
    });
    
    const pageTarget = targets.find(t => t.type === 'page' && t.url.includes('wangshangliao'));
    if (!pageTarget) {
        console.log('WangShangLiao not found. Please start with --remote-debugging-port=9222');
        return;
    }
    
    console.log('Found:', pageTarget.url);
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
    console.log('Connected to DevTools');
    
    async function evaluate(expression, awaitPromise = false) {
        return new Promise((resolve, reject) => {
            const id = msgId++;
            const timeout = setTimeout(() => {
                pending.delete(id);
                reject(new Error('Timeout'));
            }, 10000);
            
            pending.set(id, (result) => {
                clearTimeout(timeout);
                if (result.result && result.result.result) {
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
    
    // 探索 window.nim 的所有方法
    console.log('\n=== NIM SDK Methods from Runtime ===');
    const nimMethods = await evaluate(
        Object.keys(window.nim || {})
            .filter(k => typeof window.nim[k] === 'function')
            .sort()
            .join('\\n')
    );
    console.log(nimMethods || 'nim not found');
    
    // 探索 NIM SDK 版本和配置
    console.log('\n=== NIM SDK Info ===');
    const nimInfo = await evaluate(
        JSON.stringify({
            version: window.nim?.version,
            appKey: window.nim?.options?.appKey,
            account: window.nim?.options?.account,
            connected: window.nim?.connected
        }, null, 2)
    );
    console.log(nimInfo || 'No info');
    
    // 探索Pinia stores
    console.log('\n=== Pinia Stores ===');
    const stores = await evaluate(
        (function() {
            var result = [];
            if (window.__PINIA__) {
                window.__PINIA__._s.forEach((store, id) => {
                    result.push({
                        id: id,
                        actions: Object.keys(store).filter(k => typeof store[k] === 'function')
                    });
                });
            }
            return JSON.stringify(result, null, 2);
        })()
    );
    console.log(stores || 'Pinia not found');
    
    // 探索全局解密函数
    console.log('\n=== Global Decrypt Functions ===');
    const decryptFuncs = await evaluate(
        (function() {
            var funcs = [];
            ['AES', 'decryptNick', 'decryptTeamNick', 'AES_decryptNick', 'isBase64'].forEach(name => {
                if (typeof window[name] !== 'undefined') {
                    funcs.push(name + ': ' + typeof window[name]);
                }
            });
            return funcs.join('\\n');
        })()
    );
    console.log(decryptFuncs || 'None found');
    
    // 测试解密函数
    console.log('\n=== Test Decrypt Function ===');
    const decryptTest = await evaluate(
        (function() {
            if (typeof AES !== 'undefined' && AES.decrypt) {
                // 测试一个已知的密文
                try {
                    var testCiphertext = 'UzJTYk1lS2VCZ0dTU2hDSnlxUFlnZz09';
                    var result = AES.decrypt(testCiphertext);
                    return 'Decrypt test: ' + (result || 'empty result');
                } catch(e) {
                    return 'Error: ' + e.message;
                }
            }
            return 'AES.decrypt not found';
        })()
    );
    console.log(decryptTest);
    
    ws.close();
    console.log('\n=== Exploration Complete ===');
}

explore().catch(e => console.error('Error:', e.message));
