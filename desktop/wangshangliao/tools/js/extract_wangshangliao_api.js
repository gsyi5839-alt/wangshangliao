// 旺商聊API提取脚本 - 通过CDP提取运行时所有API
const WebSocket = require('ws');

const CDP_URL = 'ws://127.0.0.1:9222/devtools/page/8322EEB7A02952E8C4C59B59B616C299';

async function extractAPIs() {
    return new Promise((resolve, reject) => {
        const ws = new WebSocket(CDP_URL);
        let messageId = 1;
        const results = {};

        ws.on('open', () => {
            console.log('✅ 连接旺商聊成功');

            // 提取所有API的JS代码
            const extractCode = `
(function() {
    const result = {
        nimAPIs: {},
        piniaStores: {},
        vueApp: {},
        globalObjects: {},
        decryptFunctions: {},
        eventHandlers: {}
    };

    // 1. 提取 window.nim 对象的所有方法
    if (window.nim) {
        result.nimAPIs.methods = [];
        result.nimAPIs.options = {};
        
        for (let key in window.nim) {
            if (typeof window.nim[key] === 'function') {
                result.nimAPIs.methods.push(key);
            }
        }
        
        // 提取nim.options中的事件处理器
        if (window.nim.options) {
            for (let key in window.nim.options) {
                if (typeof window.nim.options[key] === 'function') {
                    result.nimAPIs.options[key] = 'function';
                } else if (window.nim.options[key] !== null && window.nim.options[key] !== undefined) {
                    result.nimAPIs.options[key] = typeof window.nim.options[key];
                }
            }
        }
    }

    // 2. 提取 Pinia stores
    if (window.pinia && window.pinia._s) {
        result.piniaStores.storeNames = Array.from(window.pinia._s.keys());
        
        // 获取每个store的方法
        window.pinia._s.forEach((store, name) => {
            result.piniaStores[name] = {
                state: Object.keys(store.$state || {}),
                actions: [],
                getters: []
            };
            for (let key in store) {
                if (typeof store[key] === 'function' && !key.startsWith('$')) {
                    result.piniaStores[name].actions.push(key);
                }
            }
        });
    }

    // 3. 提取Vue app相关
    if (window.__vue_app__) {
        result.vueApp.exists = true;
        result.vueApp.version = window.__vue_app__.version || 'unknown';
    }

    // 4. 查找全局解密相关函数
    const globalKeys = Object.keys(window);
    result.globalObjects.allKeys = globalKeys.filter(k => 
        k.toLowerCase().includes('aes') ||
        k.toLowerCase().includes('decrypt') ||
        k.toLowerCase().includes('encrypt') ||
        k.toLowerCase().includes('crypto') ||
        k.toLowerCase().includes('nim') ||
        k.toLowerCase().includes('pinia')
    );

    // 5. 查找AES相关
    if (window.CryptoJS) {
        result.decryptFunctions.CryptoJS = Object.keys(window.CryptoJS);
    }

    // 6. 查找解密函数
    const decryptKeywords = ['AES', 'decrypt', 'decryptNick', 'decryptTeamNick'];
    for (let key of globalKeys) {
        try {
            if (typeof window[key] === 'function') {
                const fnStr = window[key].toString().substring(0, 500);
                if (decryptKeywords.some(kw => fnStr.includes(kw))) {
                    result.decryptFunctions[key] = fnStr.substring(0, 200);
                }
            }
        } catch(e) {}
    }

    return JSON.stringify(result, null, 2);
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

        ws.on('message', (data) => {
            const response = JSON.parse(data.toString());
            
            if (response.id === 1) {
                if (response.result && response.result.result) {
                    console.log('\n📋 旺商聊API提取结果:\n');
                    console.log(response.result.result.value);
                    
                    // 保存结果
                    const fs = require('fs');
                    fs.writeFileSync('C:\\wangshangliao\\wangshangliao_api_result.json', 
                        response.result.result.value);
                    console.log('\n✅ 结果已保存到 wangshangliao_api_result.json');
                }
                
                // 继续提取更多详细信息
                ws.send(JSON.stringify({
                    id: messageId++,
                    method: 'Runtime.evaluate',
                    params: {
                        expression: `
(function() {
    const nimMethods = [];
    if (window.nim) {
        const methodDetails = {};
        
        // 获取所有nim方法的详细信息
        for (let key in window.nim) {
            if (typeof window.nim[key] === 'function') {
                try {
                    const fnStr = window.nim[key].toString();
                    // 提取参数
                    const match = fnStr.match(/^function\\s*\\w*\\s*\\(([^)]*)\\)/);
                    const params = match ? match[1] : '';
                    methodDetails[key] = {
                        params: params,
                        isAsync: fnStr.includes('async') || fnStr.includes('Promise')
                    };
                } catch(e) {
                    methodDetails[key] = { error: e.message };
                }
            }
        }
        return JSON.stringify(methodDetails, null, 2);
    }
    return '{}';
})()
                        `,
                        returnByValue: true
                    }
                }));
            }
            
            if (response.id === 2) {
                if (response.result && response.result.result) {
                    console.log('\n📋 NIM方法详细信息:\n');
                    console.log(response.result.result.value);
                    
                    const fs = require('fs');
                    fs.writeFileSync('C:\\wangshangliao\\nim_methods_detail.json', 
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

extractAPIs().catch(console.error);

