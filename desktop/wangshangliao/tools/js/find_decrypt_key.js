// 搜索旺商聊中的解密密钥和解密函数
const WebSocket = require('ws');
const http = require('http');
const fs = require('fs');

async function getDebuggerUrl() {
    return new Promise((resolve, reject) => {
        http.get('http://127.0.0.1:9222/json', (res) => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', () => {
                const pages = JSON.parse(data);
                const mainPage = pages.find(p => p.url.includes('index.html'));
                if (mainPage) resolve(mainPage.webSocketDebuggerUrl);
                else reject(new Error('未找到旺商聊主页面'));
            });
        }).on('error', reject);
    });
}

async function findDecryptKey() {
    const cdpUrl = await getDebuggerUrl();
    console.log('CDP URL:', cdpUrl);
    
    return new Promise((resolve, reject) => {
        const ws = new WebSocket(cdpUrl);
        let messageId = 1;

        ws.on('open', () => {
            console.log('✅ 连接成功\n');

            // 搜索所有可能存在密钥的地方
            const searchCode = `
(function() {
    const result = {
        keys: [],
        decryptFunctions: [],
        cryptoObjects: [],
        configValues: []
    };
    
    // 1. 搜索全局变量中的密钥模式
    const keyPatterns = [
        'd6ba6647b7c43b79d0e42ceb2790e342',
        'kgWRyiiODMjSCh0m',
        'aes', 'AES', 'key', 'KEY', 'iv', 'IV',
        'encrypt', 'decrypt', 'cipher'
    ];
    
    function searchInObject(obj, path, depth) {
        if (depth > 4 || !obj) return;
        
        try {
            for (let key in obj) {
                const val = obj[key];
                const currentPath = path + '.' + key;
                
                // 检查key名
                if (keyPatterns.some(p => key.toLowerCase().includes(p.toLowerCase()))) {
                    result.keys.push({
                        path: currentPath,
                        keyName: key,
                        type: typeof val,
                        value: typeof val === 'string' ? val : 
                               typeof val === 'function' ? val.toString().substring(0, 200) : null
                    });
                }
                
                // 检查string值是否包含密钥
                if (typeof val === 'string') {
                    if (val.includes('d6ba6647') || val.includes('kgWRyiiODMjSCh0m') ||
                        val.length === 32 || val.length === 16) {
                        result.keys.push({
                            path: currentPath,
                            type: 'string',
                            value: val,
                            possibleKey: true
                        });
                    }
                }
                
                // 递归搜索
                if (typeof val === 'object' && val !== null && !Array.isArray(val)) {
                    searchInObject(val, currentPath, depth + 1);
                }
            }
        } catch(e) {}
    }
    
    // 搜索常见位置
    ['config', 'Config', 'CONFIG', 'settings', 'Settings', 
     'options', 'Options', 'app', 'App', '__', '_'].forEach(name => {
        if (window[name]) {
            searchInObject(window[name], 'window.' + name, 0);
        }
    });
    
    // 2. 搜索所有script标签中的源码
    const scripts = Array.from(document.querySelectorAll('script'));
    scripts.forEach((script, idx) => {
        const content = script.textContent || '';
        if (content.includes('d6ba6647') || content.includes('kgWRyiiODMjSCh0m')) {
            result.keys.push({
                source: 'inline_script_' + idx,
                found: true,
                preview: content.substring(0, 500)
            });
        }
    });
    
    // 3. 搜索CryptoJS或类似库
    const cryptoNames = ['CryptoJS', 'crypto', 'Crypto', 'aesjs', 'AES', 'aes'];
    cryptoNames.forEach(name => {
        if (window[name]) {
            const obj = window[name];
            result.cryptoObjects.push({
                name: name,
                type: typeof obj,
                methods: typeof obj === 'object' ? Object.keys(obj).filter(k => 
                    typeof obj[k] === 'function' || typeof obj[k] === 'object'
                ).slice(0, 30) : null
            });
        }
    });
    
    // 4. 检查localStorage中的配置
    try {
        for (let i = 0; i < localStorage.length; i++) {
            const key = localStorage.key(i);
            const val = localStorage.getItem(key);
            if (key.includes('key') || key.includes('Key') || 
                key.includes('encrypt') || key.includes('decrypt') ||
                key.includes('config') || key.includes('Config')) {
                result.configValues.push({
                    key: key,
                    value: val.substring(0, 200)
                });
            }
        }
    } catch(e) {}
    
    return result;
})()
`;
            ws.send(JSON.stringify({
                id: messageId++,
                method: 'Runtime.evaluate',
                params: {
                    expression: searchCode,
                    returnByValue: true
                }
            }));

            // 获取一个真实的群成员来测试解密
            setTimeout(() => {
                const getMemberCode = `
(function() {
    return new Promise((resolve) => {
        if (window.nim && window.nim.getTeamMembers) {
            // 获取当前会话的群ID
            let teamId = null;
            const url = window.location.href;
            const match = url.match(/team-([\\d]+)/);
            if (match) {
                teamId = match[1];
            }
            
            if (teamId) {
                window.nim.getTeamMembers({
                    teamId: teamId,
                    done: function(err, result) {
                        if (!err && result && result.members) {
                            const members = result.members.slice(0, 5).map(m => ({
                                account: m.account,
                                nick: m.nick,
                                nickInTeam: m.nickInTeam,
                                custom: m.custom ? {
                                    nickname_ciphertext: m.custom.nickname_ciphertext,
                                    full: JSON.stringify(m.custom).substring(0, 500)
                                } : null,
                                type: m.type,
                                joinTime: m.joinTime
                            }));
                            resolve({ teamId, members, total: result.members.length });
                        } else {
                            resolve({ error: err ? err.message : 'No members', teamId });
                        }
                    }
                });
            } else {
                resolve({ error: 'No team ID found in URL' });
            }
        } else {
            resolve({ error: 'nim.getTeamMembers not available' });
        }
    });
})()
`;
                ws.send(JSON.stringify({
                    id: messageId++,
                    method: 'Runtime.evaluate',
                    params: {
                        expression: getMemberCode,
                        returnByValue: true,
                        awaitPromise: true
                    }
                }));
            }, 2000);

            // 尝试找到实际的AES实现
            setTimeout(() => {
                const findAESCode = `
(function() {
    const result = {
        aesImplementations: []
    };
    
    // 在所有加载的模块中搜索
    function searchForAES(obj, path, depth) {
        if (depth > 3 || !obj) return;
        
        try {
            const keys = Object.getOwnPropertyNames(obj);
            for (let key of keys) {
                try {
                    const val = obj[key];
                    if (key === 'AES' || key === 'aes') {
                        result.aesImplementations.push({
                            path: path + '.' + key,
                            type: typeof val,
                            methods: typeof val === 'object' ? Object.keys(val).slice(0, 20) : null,
                            source: typeof val === 'function' ? val.toString().substring(0, 300) : null
                        });
                    }
                    
                    // 检查是否是CryptoJS风格的对象
                    if (typeof val === 'object' && val !== null) {
                        if (val.encrypt && val.decrypt) {
                            result.aesImplementations.push({
                                path: path + '.' + key,
                                type: 'crypto-like',
                                hasEncrypt: typeof val.encrypt === 'function',
                                hasDecrypt: typeof val.decrypt === 'function'
                            });
                        }
                    }
                } catch(e) {}
            }
        } catch(e) {}
    }
    
    // 搜索window的直接子级
    for (let key in window) {
        try {
            if (window[key] && typeof window[key] === 'object') {
                searchForAES(window[key], 'window.' + key, 0);
            }
        } catch(e) {}
    }
    
    return result;
})()
`;
                ws.send(JSON.stringify({
                    id: messageId++,
                    method: 'Runtime.evaluate',
                    params: {
                        expression: findAESCode,
                        returnByValue: true
                    }
                }));
            }, 4000);
        });

        ws.on('message', (data) => {
            const msg = JSON.parse(data.toString());
            
            if (msg.id && msg.result) {
                console.log(`\n=== 响应 #${msg.id} ===`);
                
                if (msg.result.result && msg.result.result.value) {
                    const result = msg.result.result.value;
                    console.log(JSON.stringify(result, null, 2));
                    
                    // 保存结果
                    fs.writeFileSync(`decrypt_search_${msg.id}.json`, JSON.stringify(result, null, 2));
                }
            }
        });

        ws.on('error', reject);
        
        setTimeout(() => {
            ws.close();
            resolve();
        }, 8000);
    });
}

findDecryptKey().then(() => {
    console.log('\n===== 搜索完成 =====');
}).catch(err => {
    console.error('错误:', err);
});

