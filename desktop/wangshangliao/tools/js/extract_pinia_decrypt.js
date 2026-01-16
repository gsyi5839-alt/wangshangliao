// 提取Pinia stores和解密函数
const WebSocket = require('ws');

async function getDebuggerUrl() {
    const http = require('http');
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

async function extractMoreAPIs() {
    const cdpUrl = await getDebuggerUrl();
    console.log('CDP URL:', cdpUrl);
    
    return new Promise((resolve, reject) => {
        const ws = new WebSocket(cdpUrl);
        let messageId = 1;

        ws.on('open', () => {
            console.log('✅ 连接成功');

            // 提取Pinia stores详细信息
            const extractPiniaCode = `
(function() {
    const result = {
        piniaStores: {},
        decryptFunctions: [],
        customFields: {}
    };

    // 1. 提取 Pinia stores
    if (window.pinia && window.pinia._s) {
        window.pinia._s.forEach((store, name) => {
            result.piniaStores[name] = {
                stateKeys: Object.keys(store.$state || {}),
                methods: [],
                getters: []
            };
            
            for (let key in store) {
                if (key.startsWith('$')) continue;
                
                const type = typeof store[key];
                if (type === 'function') {
                    result.piniaStores[name].methods.push(key);
                } else if (type !== 'object' || store[key] === null) {
                    // 简单值
                } else {
                    // 可能是getter
                }
            }
        });
    }

    // 2. 查找 __vue_app__ 中的全局属性
    if (window.__vue_app__) {
        result.vueApp = {
            exists: true,
            config: Object.keys(window.__vue_app__.config || {}),
            components: Object.keys(window.__vue_app__._component?.components || {})
        };
    }

    // 3. 查找加密解密相关
    // 搜索所有包含 AES/decrypt/encrypt 的全局函数
    const decryptKeywords = ['AES', 'decrypt', 'encrypt', 'cipher', 'crypto'];
    for (let key of Object.keys(window)) {
        try {
            const val = window[key];
            if (typeof val === 'function') {
                const fnStr = val.toString();
                if (decryptKeywords.some(kw => fnStr.toLowerCase().includes(kw.toLowerCase()))) {
                    result.decryptFunctions.push({
                        name: key,
                        preview: fnStr.substring(0, 300)
                    });
                }
            } else if (typeof val === 'object' && val !== null) {
                // 检查对象内的方法
                for (let prop in val) {
                    try {
                        if (typeof val[prop] === 'function') {
                            const fnStr = val[prop].toString();
                            if (decryptKeywords.some(kw => fnStr.toLowerCase().includes(kw.toLowerCase()))) {
                                result.decryptFunctions.push({
                                    name: key + '.' + prop,
                                    preview: fnStr.substring(0, 300)
                                });
                            }
                        }
                    } catch(e) {}
                }
            }
        } catch(e) {}
    }

    // 4. 查找 custom 字段结构（昵称加密字段）
    // 尝试从会话或消息中获取custom字段结构
    if (window.nim && window.nim.options) {
        result.customFields.nimOptions = Object.keys(window.nim.options).filter(k => 
            k.includes('custom') || k.includes('nick') || k.includes('cipher')
        );
    }

    return JSON.stringify(result, null, 2);
})()
            `;

            ws.send(JSON.stringify({
                id: messageId++,
                method: 'Runtime.evaluate',
                params: {
                    expression: extractPiniaCode,
                    returnByValue: true
                }
            }));
        });

        ws.on('message', (data) => {
            const response = JSON.parse(data.toString());
            
            if (response.id === 1) {
                if (response.result && response.result.result) {
                    console.log('\n📋 Pinia和解密函数提取结果:\n');
                    console.log(response.result.result.value);
                    
                    const fs = require('fs');
                    fs.writeFileSync('C:\\wangshangliao\\pinia_decrypt_result.json', 
                        response.result.result.value);
                    console.log('\n✅ 已保存到 pinia_decrypt_result.json');
                }

                // 继续提取appStore详细方法
                ws.send(JSON.stringify({
                    id: messageId++,
                    method: 'Runtime.evaluate',
                    params: {
                        expression: `
(function() {
    const result = {};
    
    // 获取appStore的详细信息
    if (window.pinia && window.pinia._s) {
        const appStore = window.pinia._s.get('app');
        if (appStore) {
            result.appStore = {
                state: {},
                methods: []
            };
            
            // 获取state
            if (appStore.$state) {
                for (let key in appStore.$state) {
                    const val = appStore.$state[key];
                    result.appStore.state[key] = typeof val === 'function' ? 'function' : 
                        (val === null ? 'null' : typeof val);
                }
            }
            
            // 获取方法
            for (let key in appStore) {
                if (!key.startsWith('$') && typeof appStore[key] === 'function') {
                    try {
                        const fnStr = appStore[key].toString();
                        result.appStore.methods.push({
                            name: key,
                            isAsync: fnStr.includes('async') || fnStr.includes('Promise'),
                            preview: fnStr.substring(0, 200)
                        });
                    } catch(e) {
                        result.appStore.methods.push({ name: key, error: e.message });
                    }
                }
            }
        }
        
        // 获取sdkStore
        const sdkStore = window.pinia._s.get('sdk');
        if (sdkStore) {
            result.sdkStore = {
                methods: []
            };
            for (let key in sdkStore) {
                if (!key.startsWith('$') && typeof sdkStore[key] === 'function') {
                    result.sdkStore.methods.push(key);
                }
            }
        }
        
        // 获取cacheStore
        const cacheStore = window.pinia._s.get('cache');
        if (cacheStore) {
            result.cacheStore = {
                methods: []
            };
            for (let key in cacheStore) {
                if (!key.startsWith('$') && typeof cacheStore[key] === 'function') {
                    result.cacheStore.methods.push(key);
                }
            }
        }
    }
    
    return JSON.stringify(result, null, 2);
})()
                        `,
                        returnByValue: true
                    }
                }));
            }

            if (response.id === 2) {
                if (response.result && response.result.result) {
                    console.log('\n📋 Store详细信息:\n');
                    console.log(response.result.result.value);
                    
                    const fs = require('fs');
                    fs.writeFileSync('C:\\wangshangliao\\store_details.json', 
                        response.result.result.value);
                }

                // 获取nim.options中所有事件处理器的详情
                ws.send(JSON.stringify({
                    id: messageId++,
                    method: 'Runtime.evaluate',
                    params: {
                        expression: `
(function() {
    const result = {
        eventHandlers: {},
        messageTypes: []
    };
    
    if (window.nim && window.nim.options) {
        const handlers = ['onmsg', 'onmsgs', 'onsysmsg', 'oncustomsysmsg', 'onofflinemsgs', 
                         'onroamingmsgs', 'onbroadcastmsg', 'onUpdateTeam', 'onupdatesessions',
                         'onupdateteammember', 'onsyncfriendaction', 'onconnect', 'ondisconnect'];
        
        for (let h of handlers) {
            if (typeof window.nim.options[h] === 'function') {
                const fnStr = window.nim.options[h].toString();
                result.eventHandlers[h] = {
                    exists: true,
                    preview: fnStr.substring(0, 500)
                };
            }
        }
    }
    
    // 获取消息类型
    result.messageTypes = ['text', 'image', 'audio', 'video', 'file', 'geo', 'custom', 
                           'tip', 'notification', 'robot'];
    
    return JSON.stringify(result, null, 2);
})()
                        `,
                        returnByValue: true
                    }
                }));
            }

            if (response.id === 3) {
                if (response.result && response.result.result) {
                    console.log('\n📋 事件处理器信息:\n');
                    console.log(response.result.result.value);
                    
                    const fs = require('fs');
                    fs.writeFileSync('C:\\wangshangliao\\event_handlers.json', 
                        response.result.result.value);
                }
                ws.close();
                resolve();
            }
        });

        ws.on('error', (err) => {
            console.error('WebSocket错误:', err);
            reject(err);
        });

        ws.on('close', () => {
            console.log('\n连接已关闭');
        });
    });
}

extractMoreAPIs().catch(console.error);

