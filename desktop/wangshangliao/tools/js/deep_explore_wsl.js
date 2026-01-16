// 深度探索旺商聊API和解密逻辑
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
                if (mainPage) {
                    resolve(mainPage.webSocketDebuggerUrl);
                } else {
                    reject(new Error('未找到旺商聊主页面'));
                }
            });
        }).on('error', reject);
    });
}

async function deepExplore() {
    const cdpUrl = await getDebuggerUrl();
    console.log('CDP URL:', cdpUrl);
    
    return new Promise((resolve, reject) => {
        const ws = new WebSocket(cdpUrl);
        let messageId = 1;
        const allResults = {};

        ws.on('open', () => {
            console.log('✅ 连接成功');

            // 深度探索代码
            const deepExploreCode = `
(function() {
    const result = {
        cryptoFunctions: [],
        decryptFunctions: [],
        encryptFunctions: [],
        nimMethods: {},
        nimOptions: {},
        piniaStores: {},
        globalAES: null,
        vueComponents: [],
        apiEndpoints: [],
        eventListeners: [],
        customHandlers: {}
    };

    // 1. 搜索全局对象中的AES相关
    function findAESInObject(obj, path = 'window', depth = 0) {
        if (depth > 3 || !obj) return;
        try {
            for (let key in obj) {
                if (key === 'AES' || key.toLowerCase().includes('aes') || 
                    key.toLowerCase().includes('crypt') || key.toLowerCase().includes('decrypt')) {
                    result.cryptoFunctions.push({
                        path: path + '.' + key,
                        type: typeof obj[key],
                        value: typeof obj[key] === 'function' ? 'function' : 
                               typeof obj[key] === 'object' ? Object.keys(obj[key] || {}).slice(0, 10) : obj[key]
                    });
                }
            }
        } catch(e) {}
    }

    // 搜索常见位置
    ['CryptoJS', 'crypto', 'AES', 'Crypto'].forEach(name => {
        if (window[name]) {
            result.cryptoFunctions.push({
                path: 'window.' + name,
                type: typeof window[name],
                keys: typeof window[name] === 'object' ? Object.keys(window[name]).slice(0, 20) : null
            });
        }
    });

    // 2. 详细获取NIM所有方法及其参数信息
    if (window.nim) {
        for (let key in window.nim) {
            const val = window.nim[key];
            if (typeof val === 'function') {
                result.nimMethods[key] = {
                    type: 'function',
                    length: val.length,  // 参数数量
                    str: val.toString().substring(0, 200)  // 函数开头
                };
            } else if (typeof val === 'object' && val !== null) {
                result.nimMethods[key] = {
                    type: 'object',
                    keys: Object.keys(val).slice(0, 30)
                };
            }
        }

        // NIM options
        if (window.nim.options) {
            for (let key in window.nim.options) {
                const val = window.nim.options[key];
                result.nimOptions[key] = {
                    type: typeof val,
                    isNull: val === null,
                    isFunction: typeof val === 'function',
                    preview: typeof val === 'function' ? val.toString().substring(0, 100) : 
                             typeof val === 'string' ? val.substring(0, 100) : null
                };
            }
        }
    }

    // 3. 获取Pinia stores详细信息
    if (window.pinia && window.pinia._s) {
        window.pinia._s.forEach((store, name) => {
            result.piniaStores[name] = {
                state: {},
                actions: [],
                getters: []
            };
            
            // 获取状态
            if (store.$state) {
                for (let key in store.$state) {
                    const val = store.$state[key];
                    result.piniaStores[name].state[key] = {
                        type: typeof val,
                        isArray: Array.isArray(val),
                        length: Array.isArray(val) ? val.length : null,
                        keys: typeof val === 'object' && val ? Object.keys(val).slice(0, 10) : null,
                        preview: typeof val === 'string' ? val.substring(0, 50) : 
                                 typeof val === 'number' || typeof val === 'boolean' ? val : null
                    };
                }
            }
            
            // 获取方法
            for (let key in store) {
                if (key.startsWith('$')) continue;
                if (typeof store[key] === 'function') {
                    result.piniaStores[name].actions.push(key);
                }
            }
        });
    }

    // 4. 搜索Vue组件
    if (window.__vue_app__) {
        try {
            const app = window.__vue_app__;
            if (app._component) {
                result.vueComponents.push({
                    name: app._component.name || 'root',
                    hasSetup: !!app._component.setup
                });
            }
        } catch(e) {}
    }

    // 5. 搜索全局函数中的decrypt/encrypt
    for (let key in window) {
        try {
            if (key.toLowerCase().includes('decrypt') || key.toLowerCase().includes('encrypt') ||
                key.toLowerCase().includes('aes') || key.toLowerCase().includes('crypto')) {
                result.cryptoFunctions.push({
                    name: key,
                    type: typeof window[key],
                    str: typeof window[key] === 'function' ? window[key].toString().substring(0, 300) : null
                });
            }
        } catch(e) {}
    }

    // 6. 搜索NIM中的解密相关
    if (window.nim) {
        // 检查nim对象中的所有属性
        function searchDecryptInNim(obj, path) {
            if (!obj || typeof obj !== 'object') return;
            try {
                for (let key in obj) {
                    if (key.toLowerCase().includes('decrypt') || 
                        key.toLowerCase().includes('nick') ||
                        key.toLowerCase().includes('crypt')) {
                        result.decryptFunctions.push({
                            path: path + '.' + key,
                            type: typeof obj[key],
                            str: typeof obj[key] === 'function' ? obj[key].toString().substring(0, 500) : null
                        });
                    }
                }
            } catch(e) {}
        }
        searchDecryptInNim(window.nim, 'window.nim');
    }

    // 7. 获取群聊设置相关方法
    result.teamMethods = [];
    if (window.nim) {
        const teamKeywords = ['team', 'group', 'mute', 'kick', 'invite', 'apply', 'member', 'admin', 'owner', 'manager'];
        for (let key in window.nim) {
            if (teamKeywords.some(kw => key.toLowerCase().includes(kw))) {
                result.teamMethods.push({
                    name: key,
                    type: typeof window.nim[key],
                    argCount: typeof window.nim[key] === 'function' ? window.nim[key].length : null
                });
            }
        }
    }

    // 8. 获取消息处理相关方法
    result.messageMethods = [];
    if (window.nim) {
        const msgKeywords = ['msg', 'message', 'send', 'receive', 'history', 'read', 'recall', 'forward'];
        for (let key in window.nim) {
            if (msgKeywords.some(kw => key.toLowerCase().includes(kw))) {
                result.messageMethods.push({
                    name: key,
                    type: typeof window.nim[key],
                    argCount: typeof window.nim[key] === 'function' ? window.nim[key].length : null
                });
            }
        }
    }

    // 9. 搜索SDK store
    if (window.pinia && window.pinia._s) {
        const sdkStore = window.pinia._s.get('sdk');
        if (sdkStore) {
            result.sdkStoreActions = [];
            result.sdkStoreState = {};
            for (let key in sdkStore) {
                if (key.startsWith('$')) continue;
                if (typeof sdkStore[key] === 'function') {
                    result.sdkStoreActions.push(key);
                } else {
                    result.sdkStoreState[key] = typeof sdkStore[key];
                }
            }
        }
    }

    // 10. 搜索app store
    if (window.pinia && window.pinia._s) {
        const appStore = window.pinia._s.get('app');
        if (appStore) {
            result.appStoreActions = [];
            for (let key in appStore) {
                if (key.startsWith('$')) continue;
                if (typeof appStore[key] === 'function') {
                    result.appStoreActions.push(key);
                }
            }
        }
    }

    // 11. 搜索cache store
    if (window.pinia && window.pinia._s) {
        const cacheStore = window.pinia._s.get('cache');
        if (cacheStore) {
            result.cacheStoreActions = [];
            for (let key in cacheStore) {
                if (key.startsWith('$')) continue;
                if (typeof cacheStore[key] === 'function') {
                    result.cacheStoreActions.push(key);
                }
            }
        }
    }

    return result;
})()
`;

            // 发送Runtime.evaluate
            ws.send(JSON.stringify({
                id: messageId++,
                method: 'Runtime.evaluate',
                params: {
                    expression: deepExploreCode,
                    returnByValue: true,
                    awaitPromise: false
                }
            }));
        });

        ws.on('message', (data) => {
            const msg = JSON.parse(data.toString());
            
            if (msg.result && msg.result.result && msg.result.result.value) {
                const result = msg.result.result.value;
                console.log('\n===== 深度探索结果 =====\n');
                
                // 打印各部分结果
                console.log('【加密相关函数】');
                if (result.cryptoFunctions && result.cryptoFunctions.length > 0) {
                    result.cryptoFunctions.forEach(cf => {
                        console.log(`  - ${cf.path || cf.name}: ${cf.type}`);
                        if (cf.keys) console.log(`    keys: ${cf.keys.join(', ')}`);
                        if (cf.str) console.log(`    code: ${cf.str.substring(0, 150)}...`);
                    });
                }

                console.log('\n【NIM方法统计】');
                const nimKeys = Object.keys(result.nimMethods || {});
                console.log(`  总数: ${nimKeys.length}`);
                
                // 分类打印
                const categories = {
                    '消息': ['msg', 'send', 'text', 'file', 'image', 'audio', 'video', 'custom'],
                    '群组': ['team', 'group', 'member', 'mute', 'kick'],
                    '超大群': ['superteam', 'super'],
                    '用户': ['user', 'info', 'profile'],
                    '好友': ['friend', 'addFriend', 'deleteFriend'],
                    '会话': ['session', 'recent', 'curr'],
                    '系统': ['sys', 'system', 'notification'],
                    '数据库': ['db', 'local', 'cache'],
                    '文件': ['file', 'upload', 'download', 'preview']
                };
                
                for (let cat in categories) {
                    const methods = nimKeys.filter(k => 
                        categories[cat].some(kw => k.toLowerCase().includes(kw))
                    );
                    if (methods.length > 0) {
                        console.log(`\n  【${cat}相关方法】 (${methods.length}个):`);
                        methods.forEach(m => console.log(`    - ${m}`));
                    }
                }

                console.log('\n【群聊设置方法】');
                if (result.teamMethods) {
                    result.teamMethods.forEach(m => {
                        console.log(`  - ${m.name}(${m.argCount || 0}个参数)`);
                    });
                }

                console.log('\n【消息处理方法】');
                if (result.messageMethods) {
                    result.messageMethods.forEach(m => {
                        console.log(`  - ${m.name}(${m.argCount || 0}个参数)`);
                    });
                }

                console.log('\n【NIM Options事件监听】');
                for (let key in result.nimOptions || {}) {
                    const opt = result.nimOptions[key];
                    if (opt.isFunction) {
                        console.log(`  - ${key}: function`);
                    }
                }

                console.log('\n【SDK Store Actions】');
                if (result.sdkStoreActions) {
                    result.sdkStoreActions.forEach(a => console.log(`  - ${a}`));
                }

                console.log('\n【App Store Actions】');
                if (result.appStoreActions) {
                    result.appStoreActions.forEach(a => console.log(`  - ${a}`));
                }

                console.log('\n【Cache Store Actions】');
                if (result.cacheStoreActions) {
                    result.cacheStoreActions.forEach(a => console.log(`  - ${a}`));
                }

                console.log('\n【Pinia Stores详情】');
                for (let name in result.piniaStores || {}) {
                    const store = result.piniaStores[name];
                    console.log(`\n  [${name}]`);
                    console.log(`    状态字段: ${Object.keys(store.state || {}).join(', ')}`);
                    console.log(`    方法: ${(store.actions || []).join(', ')}`);
                }

                // 保存完整结果
                fs.writeFileSync('deep_explore_result.json', JSON.stringify(result, null, 2));
                console.log('\n✅ 完整结果已保存到 deep_explore_result.json');
                
                allResults.deepExplore = result;
                
                // 继续获取更多细节
                continueExplore(ws, messageId, allResults);
            }
        });

        ws.on('error', reject);
        
        setTimeout(() => {
            ws.close();
            resolve(allResults);
        }, 30000);
    });
}

function continueExplore(ws, startId, allResults) {
    let messageId = startId;
    
    // 获取当前群聊详情
    const getTeamDetailsCode = `
(function() {
    const result = {};
    
    // 获取当前会话
    if (window.pinia && window.pinia._s) {
        const appStore = window.pinia._s.get('app');
        if (appStore && appStore.currentSession) {
            result.currentSession = {
                to: appStore.currentSession.to,
                scene: appStore.currentSession.scene,
                unread: appStore.currentSession.unread,
                lastMsg: appStore.currentSession.lastMsg ? {
                    type: appStore.currentSession.lastMsg.type,
                    text: appStore.currentSession.lastMsg.text,
                    from: appStore.currentSession.lastMsg.from,
                    time: appStore.currentSession.lastMsg.time
                } : null
            };
        }
    }
    
    // 获取群列表
    if (window.nim && window.nim.getTeams) {
        // 异步获取
        return new Promise((resolve) => {
            window.nim.getTeams({
                done: function(err, teams) {
                    if (!err && teams) {
                        result.teams = teams.slice(0, 5).map(t => ({
                            teamId: t.teamId,
                            name: t.name,
                            memberNum: t.memberNum,
                            owner: t.owner,
                            mute: t.mute,
                            muteType: t.muteType,
                            joinMode: t.joinMode,
                            beInviteMode: t.beInviteMode,
                            inviteMode: t.inviteMode,
                            updateTeamMode: t.updateTeamMode,
                            updateCustomMode: t.updateCustomMode,
                            level: t.level,
                            serverCustom: t.serverCustom,
                            custom: t.custom
                        }));
                        result.totalTeams = teams.length;
                    }
                    resolve(result);
                }
            });
        });
    }
    
    return result;
})()
`;

    ws.send(JSON.stringify({
        id: messageId++,
        method: 'Runtime.evaluate',
        params: {
            expression: getTeamDetailsCode,
            returnByValue: true,
            awaitPromise: true
        }
    }));
    
    // 获取解密函数源码
    setTimeout(() => {
        const getDecryptSourceCode = `
(function() {
    const result = {};
    
    // 尝试获取各种可能的解密函数
    const sources = {};
    
    // 1. 搜索window上的所有可能的AES对象
    if (window.CryptoJS) {
        sources.CryptoJS = {
            available: true,
            methods: Object.keys(window.CryptoJS).filter(k => typeof window.CryptoJS[k] === 'function' || typeof window.CryptoJS[k] === 'object')
        };
    }
    
    // 2. 搜索全局的AES对象
    for (let key in window) {
        try {
            if (key.includes('AES') || key.includes('aes') || key.includes('Crypto')) {
                sources[key] = {
                    type: typeof window[key],
                    keys: typeof window[key] === 'object' ? Object.keys(window[key]).slice(0, 20) : null
                };
            }
        } catch(e) {}
    }
    
    // 3. 在nim对象中搜索
    if (window.nim) {
        for (let key in window.nim) {
            try {
                if (key.toLowerCase().includes('decrypt') || key.toLowerCase().includes('encrypt')) {
                    sources['nim.' + key] = {
                        type: typeof window.nim[key],
                        source: typeof window.nim[key] === 'function' ? window.nim[key].toString() : null
                    };
                }
            } catch(e) {}
        }
    }
    
    // 4. 获取nim的prototype
    if (window.nim && window.nim.__proto__) {
        result.nimProto = Object.keys(window.nim.__proto__).slice(0, 50);
    }
    
    result.sources = sources;
    return result;
})()
`;
        
        ws.send(JSON.stringify({
            id: messageId++,
            method: 'Runtime.evaluate',
            params: {
                expression: getDecryptSourceCode,
                returnByValue: true,
                awaitPromise: false
            }
        }));
    }, 2000);
    
    // 获取网络请求拦截信息
    setTimeout(() => {
        const getNetworkInfoCode = `
(function() {
    const result = {
        xhrIntercepted: false,
        fetchIntercepted: false,
        websockets: []
    };
    
    // 检查XHR是否被拦截
    if (XMLHttpRequest.prototype.open.toString().includes('native code')) {
        result.xhrIntercepted = false;
    } else {
        result.xhrIntercepted = true;
    }
    
    // 检查fetch是否被拦截
    if (window.fetch.toString().includes('native code')) {
        result.fetchIntercepted = false;
    } else {
        result.fetchIntercepted = true;
    }
    
    return result;
})()
`;

        ws.send(JSON.stringify({
            id: messageId++,
            method: 'Runtime.evaluate',
            params: {
                expression: getNetworkInfoCode,
                returnByValue: true,
                awaitPromise: false
            }
        }));
    }, 4000);
}

// 执行
deepExplore().then(results => {
    console.log('\n===== 探索完成 =====');
}).catch(err => {
    console.error('错误:', err);
});

