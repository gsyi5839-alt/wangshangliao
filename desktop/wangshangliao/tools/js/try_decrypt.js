// Try to decrypt nicknameCiphertext using appKey
const WebSocket = require('ws');
const http = require('http');
const crypto = require('crypto');

async function tryDecrypt() {
    const pagesJson = await new Promise((resolve, reject) => {
        http.get('http://127.0.0.1:9222/json', (res) => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', () => resolve(JSON.parse(data)));
        }).on('error', reject);
    });
    
    const page = pagesJson.find(p => p.title === '旺商聊');
    if (!page) {
        console.log('WangShangLiao not found');
        return;
    }
    
    const ws = new WebSocket(page.webSocketDebuggerUrl);
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
    
    // Get appKey and other potential keys
    console.log('\n=== Getting Potential Keys ===');
    const keyScript = `
(function() {
    var result = { keys: {} };
    
    if (window.nim && window.nim.options) {
        result.keys.appKey = window.nim.options.appKey;
        result.keys.token = window.nim.options.token;
        result.keys.account = window.nim.options.account;
        
        // Get all options
        result.nimOptionsKeys = Object.keys(window.nim.options);
    }
    
    // Check localStorage
    for (var i = 0; i < localStorage.length; i++) {
        var key = localStorage.key(i);
        if (key.indexOf('key') !== -1 || key.indexOf('token') !== -1) {
            result.keys[key] = localStorage.getItem(key);
        }
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const keys = await evaluate(keyScript);
    console.log('Keys:', keys);
    
    // Get sample encrypted data
    console.log('\n=== Getting Sample Encrypted Data ===');
    const sampleScript = `
(async function() {
    var result = { samples: [] };
    
    if (!window.nim) return JSON.stringify({error: 'no nim'});
    
    var url = window.location.href;
    var match = url.match(/team-([0-9]+)/);
    if (!match) return JSON.stringify({error: 'no team'});
    
    var teamId = match[1];
    
    try {
        var teamData = await new Promise(function(resolve, reject) {
            window.nim.getTeamMembers({
                teamId: teamId,
                done: function(err, obj) {
                    if (err) reject(err);
                    else resolve(obj);
                }
            });
            setTimeout(reject, 10000);
        });
        
        var members = teamData.members || teamData || [];
        
        members.forEach(function(m) {
            if (m.custom) {
                try {
                    var customObj = JSON.parse(m.custom);
                    if (customObj.nicknameCiphertext) {
                        result.samples.push({
                            account: m.account,
                            nickInTeam: m.nickInTeam,
                            ciphertext: customObj.nicknameCiphertext,
                            groupId: customObj.groupId
                        });
                    }
                } catch(e) {}
            }
        });
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const samples = await evaluate(sampleScript);
    console.log('Samples:', samples);
    
    // Parse the keys and samples
    const keysData = JSON.parse(keys);
    const samplesData = JSON.parse(samples);
    
    console.log('\n=== Trying Different Decryption Methods ===');
    
    const appKey = keysData.keys?.appKey;
    console.log('AppKey:', appKey);
    
    if (appKey && samplesData.samples && samplesData.samples.length > 0) {
        const sample = samplesData.samples[0];
        const ciphertext = sample.ciphertext;
        const ciphertextBuffer = Buffer.from(ciphertext, 'base64');
        
        console.log('Ciphertext (hex):', ciphertextBuffer.toString('hex'));
        console.log('Ciphertext length:', ciphertextBuffer.length);
        
        // Try different key derivations and modes
        const tryKeys = [
            // Raw appKey (first 16 bytes)
            { name: 'appKey[0:16]', key: Buffer.from(appKey.substring(0, 16), 'utf8') },
            // MD5 of appKey
            { name: 'MD5(appKey)', key: crypto.createHash('md5').update(appKey).digest() },
            // SHA256 of appKey (first 16 bytes)
            { name: 'SHA256(appKey)[0:16]', key: crypto.createHash('sha256').update(appKey).digest().slice(0, 16) },
            // GroupId as part of key
            { name: 'groupId+appKey', key: crypto.createHash('md5').update(sample.groupId + appKey).digest() },
            // Account as part of key
            { name: 'account+appKey', key: crypto.createHash('md5').update(sample.account + appKey).digest() },
        ];
        
        const modes = ['aes-128-ecb', 'aes-128-cbc'];
        const ivs = [
            null, // ECB doesn't use IV
            Buffer.alloc(16, 0), // Zero IV
            Buffer.from(appKey.substring(0, 16), 'utf8'), // appKey as IV
        ];
        
        for (const keyInfo of tryKeys) {
            for (const mode of modes) {
                for (const iv of ivs) {
                    if (mode === 'aes-128-ecb' && iv !== null) continue;
                    if (mode === 'aes-128-cbc' && iv === null) continue;
                    
                    try {
                        const decipher = iv 
                            ? crypto.createDecipheriv(mode, keyInfo.key, iv)
                            : crypto.createDecipheriv(mode, keyInfo.key, '');
                        
                        decipher.setAutoPadding(true);
                        
                        let decrypted = decipher.update(ciphertextBuffer);
                        decrypted = Buffer.concat([decrypted, decipher.final()]);
                        
                        const decryptedStr = decrypted.toString('utf8');
                        
                        // Check if result looks like a valid nickname (printable, reasonable length)
                        if (decryptedStr.length > 0 && decryptedStr.length < 50 && /^[\u0020-\u007e\u4e00-\u9fff]+$/.test(decryptedStr)) {
                            console.log(`\n*** SUCCESS! ***`);
                            console.log(`Key: ${keyInfo.name}`);
                            console.log(`Mode: ${mode}`);
                            console.log(`IV: ${iv ? iv.toString('hex') : 'none'}`);
                            console.log(`Decrypted: "${decryptedStr}"`);
                        }
                    } catch (e) {
                        // Decryption failed, try next combination
                    }
                }
            }
        }
    }
    
    // Try to find decrypt function in the app
    console.log('\n=== Searching for Decrypt Function in App ===');
    const findDecryptScript = `
(function() {
    var result = { found: false, functions: [] };
    
    // Search through all webpack modules
    if (window.webpackJsonp) {
        result.hasWebpack = true;
    }
    
    // Try to find via require
    if (typeof require === 'function') {
        // Try common module names
        var moduleNames = ['utils', 'crypto', 'helper', 'common', 'api', 'service'];
        moduleNames.forEach(function(name) {
            try {
                var mod = require(name);
                if (mod) {
                    result.functions.push({
                        module: name,
                        keys: Object.keys(mod).slice(0, 20)
                    });
                }
            } catch(e) {}
        });
    }
    
    // Check for AES decryption in CryptoJS-like libraries
    if (window.CryptoJS) {
        result.hasCryptoJS = true;
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const findDecrypt = await evaluate(findDecryptScript);
    console.log('Find Decrypt:', findDecrypt);
    
    ws.close();
}

tryDecrypt().catch(console.error);

