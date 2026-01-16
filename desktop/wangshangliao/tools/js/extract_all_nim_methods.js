// 提取旺商聊所有NIM SDK方法
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

async function extractAllMethods() {
    const cdpUrl = await getDebuggerUrl();
    console.log('CDP URL:', cdpUrl);
    
    return new Promise((resolve, reject) => {
        const ws = new WebSocket(cdpUrl);
        let messageId = 1;
        const allResults = {};

        ws.on('open', () => {
            console.log('✅ 连接成功\n');

            // 提取所有NIM方法
            const extractCode = `
(function() {
    const result = {
        nimMethods: {},
        nimOptions: {},
        nimPrototype: [],
        nimDb: {},
        cryptoFunctions: [],
        globalVars: {}
    };
    
    // 1. 提取window.nim的所有方法 - 使用prototype chain
    if (window.nim) {
        // 获取直接属性
        const ownProps = Object.getOwnPropertyNames(window.nim);
        ownProps.forEach(key => {
            try {
                const val = window.nim[key];
                if (typeof val === 'function') {
                    result.nimMethods[key] = {
                        type: 'function',
                        argCount: val.length,
                        source: 'own'
                    };
                } else if (typeof val === 'object' && val !== null) {
                    result.nimMethods[key] = {
                        type: 'object',
                        keys: Object.keys(val).slice(0, 30),
                        source: 'own'
                    };
                } else {
                    result.nimMethods[key] = {
                        type: typeof val,
                        value: val,
                        source: 'own'
                    };
                }
            } catch(e) {}
        });
        
        // 获取prototype上的方法
        let proto = Object.getPrototypeOf(window.nim);
        let level = 0;
        while (proto && proto !== Object.prototype && level < 5) {
            const protoProps = Object.getOwnPropertyNames(proto);
            protoProps.forEach(key => {
                if (key === 'constructor') return;
                try {
                    const val = proto[key];
                    if (typeof val === 'function' && !result.nimMethods[key]) {
                        result.nimMethods[key] = {
                            type: 'function',
                            argCount: val.length,
                            source: 'prototype_level_' + level
                        };
                    }
                } catch(e) {}
            });
            proto = Object.getPrototypeOf(proto);
            level++;
        }
        
        // 2. 获取nim.options的所有回调
        if (window.nim.options) {
            for (let key in window.nim.options) {
                try {
                    const val = window.nim.options[key];
                    result.nimOptions[key] = {
                        type: typeof val,
                        isFunction: typeof val === 'function',
                        isNull: val === null,
                        value: typeof val === 'string' ? val.substring(0, 100) : 
                               typeof val === 'number' || typeof val === 'boolean' ? val : null
                    };
                } catch(e) {}
            }
        }
        
        // 3. 获取nim.db的所有方法
        if (window.nim.db) {
            const dbProps = Object.getOwnPropertyNames(window.nim.db);
            let dbProto = Object.getPrototypeOf(window.nim.db);
            while (dbProto && dbProto !== Object.prototype) {
                Object.getOwnPropertyNames(dbProto).forEach(key => {
                    if (!dbProps.includes(key)) dbProps.push(key);
                });
                dbProto = Object.getPrototypeOf(dbProto);
            }
            
            dbProps.forEach(key => {
                if (key === 'constructor') return;
                try {
                    const val = window.nim.db[key];
                    if (typeof val === 'function') {
                        result.nimDb[key] = {
                            type: 'function',
                            argCount: val.length
                        };
                    }
                } catch(e) {}
            });
        }
    }
    
    // 4. 搜索全局的AES和加密相关对象
    const cryptoNames = ['CryptoJS', 'crypto', 'AES', 'Crypto', 'aes', 'cryptojs'];
    cryptoNames.forEach(name => {
        if (window[name]) {
            result.cryptoFunctions.push({
                name: name,
                type: typeof window[name],
                keys: typeof window[name] === 'object' ? Object.keys(window[name]).slice(0, 30) : null
            });
        }
    });
    
    // 5. 搜索decrypt/encrypt函数
    for (let key in window) {
        try {
            if (key.toLowerCase().includes('decrypt') || 
                key.toLowerCase().includes('encrypt') ||
                key.toLowerCase().includes('aes')) {
                result.cryptoFunctions.push({
                    name: key,
                    type: typeof window[key],
                    source: typeof window[key] === 'function' ? window[key].toString().substring(0, 200) : null
                });
            }
        } catch(e) {}
    }
    
    // 6. 搜索pinia stores
    const searchPinia = ['pinia', '__pinia', '_pinia', 'Pinia'];
    searchPinia.forEach(name => {
        if (window[name]) {
            result.globalVars[name] = {
                exists: true,
                type: typeof window[name],
                hasStores: !!(window[name]._s)
            };
            if (window[name]._s) {
                result.globalVars[name].storeNames = [];
                window[name]._s.forEach((store, storeName) => {
                    result.globalVars[name].storeNames.push(storeName);
                });
            }
        }
    });
    
    // 7. 搜索Vue app
    if (window.__vue_app__) {
        result.globalVars.__vue_app__ = {
            exists: true,
            provides: Object.keys(window.__vue_app__._context.provides || {}).slice(0, 30)
        };
    }
    
    return result;
})()
`;
            ws.send(JSON.stringify({
                id: messageId++,
                method: 'Runtime.evaluate',
                params: {
                    expression: extractCode,
                    returnByValue: true
                }
            }));
        });

        let resultReceived = false;
        
        ws.on('message', (data) => {
            const msg = JSON.parse(data.toString());
            
            if (msg.id === 1 && msg.result && msg.result.result && msg.result.result.value) {
                resultReceived = true;
                const result = msg.result.result.value;
                
                console.log('='.repeat(60));
                console.log('旺商聊 NIM SDK 完整API清单');
                console.log('='.repeat(60));
                
                // 打印NIM方法
                console.log('\n【NIM SDK 方法】');
                const methods = result.nimMethods;
                const methodNames = Object.keys(methods).sort();
                
                // 按类别分组
                const categories = {
                    '消息操作': ['send', 'msg', 'text', 'file', 'image', 'audio', 'video', 'custom', 'recall', 'forward'],
                    '群组操作': ['team', 'Team'],
                    '超大群操作': ['superTeam', 'SuperTeam', 'superteam'],
                    '用户操作': ['user', 'User', 'myInfo', 'MyInfo'],
                    '好友操作': ['friend', 'Friend'],
                    '黑名单': ['black', 'Black', 'mute'],
                    '会话操作': ['session', 'Session', 'recent', 'Recent'],
                    '系统消息': ['sysMsg', 'SysMsg', 'sys'],
                    '数据库': ['db', 'local', 'Local', 'cache'],
                    '文件操作': ['file', 'File', 'nos', 'upload', 'download', 'preview'],
                    '事件相关': ['on', 'off', 'emit', 'event']
                };
                
                const categorized = {};
                const uncategorized = [];
                
                methodNames.forEach(name => {
                    let found = false;
                    for (let cat in categories) {
                        if (categories[cat].some(kw => name.toLowerCase().includes(kw.toLowerCase()))) {
                            if (!categorized[cat]) categorized[cat] = [];
                            categorized[cat].push({
                                name: name,
                                ...methods[name]
                            });
                            found = true;
                            break;
                        }
                    }
                    if (!found) {
                        uncategorized.push({
                            name: name,
                            ...methods[name]
                        });
                    }
                });
                
                // 打印分类结果
                for (let cat in categorized) {
                    console.log(`\n  【${cat}】 (${categorized[cat].length}个)`);
                    categorized[cat].forEach(m => {
                        if (m.type === 'function') {
                            console.log(`    - ${m.name}(${m.argCount}个参数) [${m.source}]`);
                        } else {
                            console.log(`    - ${m.name}: ${m.type}`);
                        }
                    });
                }
                
                if (uncategorized.length > 0) {
                    console.log(`\n  【其他】 (${uncategorized.length}个)`);
                    uncategorized.forEach(m => {
                        if (m.type === 'function') {
                            console.log(`    - ${m.name}(${m.argCount}个参数) [${m.source}]`);
                        } else if (m.type === 'object' && m.keys) {
                            console.log(`    - ${m.name}: object {${m.keys.slice(0, 5).join(', ')}...}`);
                        } else {
                            console.log(`    - ${m.name}: ${m.type}`);
                        }
                    });
                }
                
                // 打印NIM Options
                console.log('\n\n【NIM Options 事件回调】');
                const options = result.nimOptions;
                for (let key in options) {
                    if (options[key].isFunction) {
                        console.log(`  - ${key}: callback function`);
                    } else if (key.startsWith('on')) {
                        console.log(`  - ${key}: ${options[key].type} (可能是事件监听器占位符)`);
                    }
                }
                
                // 打印NIM DB方法
                console.log('\n\n【NIM DB (数据库) 方法】');
                const dbMethods = Object.keys(result.nimDb).sort();
                dbMethods.forEach(name => {
                    console.log(`  - ${name}(${result.nimDb[name].argCount}个参数)`);
                });
                
                // 打印加密相关
                console.log('\n\n【加密相关对象】');
                result.cryptoFunctions.forEach(cf => {
                    console.log(`  - ${cf.name}: ${cf.type}`);
                    if (cf.keys) console.log(`    keys: ${cf.keys.join(', ')}`);
                    if (cf.source) console.log(`    source: ${cf.source.substring(0, 100)}...`);
                });
                
                // 打印全局变量
                console.log('\n\n【全局状态管理】');
                for (let key in result.globalVars) {
                    const v = result.globalVars[key];
                    console.log(`  - ${key}:`);
                    if (v.storeNames) {
                        console.log(`    stores: ${v.storeNames.join(', ')}`);
                    }
                    if (v.provides) {
                        console.log(`    provides: ${v.provides.join(', ')}`);
                    }
                }
                
                // 统计
                console.log('\n\n【统计】');
                console.log(`  NIM方法总数: ${methodNames.length}`);
                console.log(`  NIM Options总数: ${Object.keys(options).length}`);
                console.log(`  NIM DB方法总数: ${dbMethods.length}`);
                
                // 保存结果
                fs.writeFileSync('nim_api_full_extract.json', JSON.stringify(result, null, 2));
                console.log('\n✅ 完整结果已保存到 nim_api_full_extract.json');
                
                allResults.methods = result;
                
                // 继续提取更多细节
                extractMoreDetails(ws, 2);
            }
        });

        ws.on('error', reject);
        
        setTimeout(() => {
            ws.close();
            resolve(allResults);
        }, 15000);
    });
}

function extractMoreDetails(ws, startId) {
    let messageId = startId;
    
    // 获取群聊相关方法的详细信息
    const teamMethodsCode = `
(function() {
    const result = {
        teamMethods: [],
        teamSettings: []
    };
    
    if (!window.nim) return result;
    
    // 获取所有team相关方法
    const teamKeywords = ['team', 'Team', 'member', 'Member', 'mute', 'Mute', 'kick', 'invite', 'apply', 'manager', 'owner', 'admin'];
    
    let proto = window.nim;
    let checked = new Set();
    
    while (proto && proto !== Object.prototype) {
        Object.getOwnPropertyNames(proto).forEach(key => {
            if (checked.has(key)) return;
            checked.add(key);
            
            try {
                const val = window.nim[key];
                if (typeof val === 'function' && teamKeywords.some(kw => key.includes(kw))) {
                    // 尝试获取函数签名
                    const funcStr = val.toString();
                    const match = funcStr.match(/function\\s*\\([^)]*\\)/);
                    const arrowMatch = funcStr.match(/\\([^)]*\\)\\s*=>/);
                    
                    result.teamMethods.push({
                        name: key,
                        argCount: val.length,
                        signature: match ? match[0] : (arrowMatch ? arrowMatch[0] : null),
                        preview: funcStr.substring(0, 300)
                    });
                }
            } catch(e) {}
        });
        proto = Object.getPrototypeOf(proto);
    }
    
    return result;
})()
`;
    
    ws.send(JSON.stringify({
        id: messageId++,
        method: 'Runtime.evaluate',
        params: {
            expression: teamMethodsCode,
            returnByValue: true
        }
    }));
    
    // 获取消息相关方法详情
    setTimeout(() => {
        const msgMethodsCode = `
(function() {
    const result = {
        messageMethods: [],
        messageTypes: []
    };
    
    if (!window.nim) return result;
    
    const msgKeywords = ['send', 'msg', 'Msg', 'text', 'Text', 'file', 'File', 'image', 'Image', 
                         'audio', 'Audio', 'video', 'Video', 'custom', 'Custom', 'recall', 'forward',
                         'history', 'History', 'read', 'Read', 'receipt'];
    
    let proto = window.nim;
    let checked = new Set();
    
    while (proto && proto !== Object.prototype) {
        Object.getOwnPropertyNames(proto).forEach(key => {
            if (checked.has(key)) return;
            checked.add(key);
            
            try {
                const val = window.nim[key];
                if (typeof val === 'function' && msgKeywords.some(kw => key.includes(kw))) {
                    const funcStr = val.toString();
                    result.messageMethods.push({
                        name: key,
                        argCount: val.length,
                        preview: funcStr.substring(0, 300)
                    });
                }
            } catch(e) {}
        });
        proto = Object.getPrototypeOf(proto);
    }
    
    return result;
})()
`;
        
        ws.send(JSON.stringify({
            id: messageId++,
            method: 'Runtime.evaluate',
            params: {
                expression: msgMethodsCode,
                returnByValue: true
            }
        }));
    }, 2000);
    
    // 获取options中的所有事件处理器
    setTimeout(() => {
        const optionsCode = `
(function() {
    const result = {
        eventHandlers: [],
        configOptions: []
    };
    
    if (!window.nim || !window.nim.options) return result;
    
    for (let key in window.nim.options) {
        const val = window.nim.options[key];
        
        if (key.startsWith('on') || key.endsWith('Func') || key.endsWith('Handler')) {
            result.eventHandlers.push({
                name: key,
                type: typeof val,
                hasValue: val !== null && val !== undefined,
                isHooked: typeof val === 'function' && !val.toString().includes('[native code]')
            });
        } else {
            result.configOptions.push({
                name: key,
                type: typeof val,
                value: typeof val === 'string' ? val.substring(0, 50) :
                       typeof val === 'number' || typeof val === 'boolean' ? val : null
            });
        }
    }
    
    return result;
})()
`;
        
        ws.send(JSON.stringify({
            id: messageId++,
            method: 'Runtime.evaluate',
            params: {
                expression: optionsCode,
                returnByValue: true
            }
        }));
    }, 4000);
    
    // 获取pinia stores的详细信息
    setTimeout(() => {
        const piniaCode = `
(function() {
    const result = {
        stores: {}
    };
    
    // 搜索pinia
    let pinia = window.pinia || window.__pinia;
    if (!pinia) {
        // 尝试从Vue app中获取
        if (window.__vue_app__ && window.__vue_app__._context && window.__vue_app__._context.provides) {
            const provides = window.__vue_app__._context.provides;
            for (let key in provides) {
                if (key.includes('pinia') || (provides[key] && provides[key]._s)) {
                    pinia = provides[key];
                    break;
                }
            }
        }
    }
    
    if (pinia && pinia._s) {
        pinia._s.forEach((store, name) => {
            result.stores[name] = {
                state: {},
                actions: [],
                getters: []
            };
            
            // 获取状态
            if (store.$state) {
                for (let key in store.$state) {
                    const val = store.$state[key];
                    result.stores[name].state[key] = {
                        type: typeof val,
                        isArray: Array.isArray(val),
                        isEmpty: val === null || val === undefined || (Array.isArray(val) && val.length === 0),
                        preview: typeof val === 'string' ? val.substring(0, 30) :
                                 typeof val === 'number' || typeof val === 'boolean' ? val : null
                    };
                }
            }
            
            // 获取方法
            for (let key in store) {
                if (key.startsWith('$') || key.startsWith('_')) continue;
                if (typeof store[key] === 'function') {
                    result.stores[name].actions.push(key);
                }
            }
        });
    }
    
    return result;
})()
`;
        
        ws.send(JSON.stringify({
            id: messageId++,
            method: 'Runtime.evaluate',
            params: {
                expression: piniaCode,
                returnByValue: true
            }
        }));
    }, 6000);
}

// 执行
extractAllMethods().then(results => {
    console.log('\n===== 提取完成 =====');
}).catch(err => {
    console.error('错误:', err);
});

