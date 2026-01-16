// 探索旺商聊运行时对象
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
                console.log('所有页面:', pages.map(p => ({ title: p.title, url: p.url })));
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

async function explore() {
    const cdpUrl = await getDebuggerUrl();
    console.log('\nCDP URL:', cdpUrl);
    
    return new Promise((resolve, reject) => {
        const ws = new WebSocket(cdpUrl);
        let messageId = 1;
        const responses = {};
    
        ws.on('open', () => {
            console.log('✅ 连接成功\n');

            // 先执行Runtime.enable
            ws.send(JSON.stringify({
                id: messageId++,
                method: 'Runtime.enable'
            }));

            // 探索全局对象
            setTimeout(() => {
                const code = `
(function() {
    const globalKeys = [];
    
    // 获取所有全局对象
    for (let key in window) {
        try {
            const val = window[key];
            if (val && typeof val === 'object' && !Array.isArray(val)) {
                // 检查是否有特殊属性
                const hasNim = 'nim' in val || key === 'nim';
                const hasPinia = 'pinia' in val || key === 'pinia' || key === '__pinia';
                const hasVue = key.includes('vue') || key.includes('Vue');
                
                globalKeys.push({
                    key: key,
                    type: typeof val,
                    isNim: hasNim,
                    isPinia: hasPinia,
                    isVue: hasVue,
                    constructor: val.constructor ? val.constructor.name : null
                });
            }
        } catch(e) {}
    }
    
    // 专门检查这些关键对象
    const result = {
        globalObjects: globalKeys.filter(k => k.isNim || k.isPinia || k.isVue || 
            k.key === 'nim' || k.key === 'pinia' || k.key === '__vue_app__'),
        nimExists: typeof window.nim !== 'undefined',
        piniaExists: typeof window.pinia !== 'undefined',
        vueExists: typeof window.__vue_app__ !== 'undefined',
        
        // 直接检查
        nimType: typeof window.nim,
        piniaType: typeof window.pinia,
        
        // 检查其他可能的位置
        possibleNimLocations: []
    };
    
    // 搜索可能的nim位置
    ['nim', 'NIM', 'Nim', '$nim', '_nim', 'sdk', 'SDK', 'netease'].forEach(name => {
        if (window[name]) {
            result.possibleNimLocations.push({
                name: name,
                type: typeof window[name],
                keys: Object.keys(window[name]).slice(0, 30)
            });
        }
    });
    
    // 检查全局变量数量
    result.totalGlobalVars = Object.keys(window).length;
    
    // 检查一些特定的全局函数
    result.specialFunctions = {};
    ['sendText', 'sendMsg', 'getTeam', 'getTeams', 'AES', 'decrypt', 'encrypt'].forEach(name => {
        if (typeof window[name] === 'function') {
            result.specialFunctions[name] = true;
        }
    });
    
    return result;
})()
`;
            ws.send(JSON.stringify({
                    id: messageId++,
                method: 'Runtime.evaluate',
                    params: {
                        expression: code,
                        returnByValue: true
                    }
            }));
            }, 500);

            // 获取所有frames
            setTimeout(() => {
                ws.send(JSON.stringify({
                    id: messageId++,
                    method: 'Page.getFrameTree'
                }));
            }, 1000);

            // 检查DOM中的script标签
            setTimeout(() => {
                const scriptCode = `
(function() {
    const scripts = Array.from(document.querySelectorAll('script'));
    return {
        scriptCount: scripts.length,
        scriptSrcs: scripts.map(s => s.src).filter(s => s),
        inlineScriptCount: scripts.filter(s => !s.src && s.textContent.length > 0).length
    };
})()
`;
                ws.send(JSON.stringify({
                    id: messageId++,
                    method: 'Runtime.evaluate',
                    params: {
                        expression: scriptCode,
                        returnByValue: true
                    }
                }));
            }, 1500);

            // 尝试在所有context中执行
            setTimeout(() => {
                ws.send(JSON.stringify({
                    id: messageId++,
                    method: 'Runtime.globalLexicalScopeNames'
                }));
            }, 2000);
    
            // 获取页面URL和标题
            setTimeout(() => {
                const pageInfoCode = `
({
    url: window.location.href,
    title: document.title,
    readyState: document.readyState
})
`;
                ws.send(JSON.stringify({
                    id: messageId++,
                    method: 'Runtime.evaluate',
                    params: {
                        expression: pageInfoCode,
                        returnByValue: true
                    }
                }));
            }, 2500);

            // 深度搜索nim
            setTimeout(() => {
                const deepSearchCode = `
(function() {
    const result = {
        found: [],
        windowKeys: Object.keys(window).length
    };
    
    // 搜索window的所有属性
    for (let key in window) {
        try {
            const val = window[key];
            // 检查是否是NIM SDK实例
            if (val && typeof val === 'object') {
                if (typeof val.sendText === 'function' || 
                    typeof val.getTeams === 'function' ||
                    typeof val.connect === 'function' ||
                    typeof val.disconnect === 'function') {
                    result.found.push({
                        path: 'window.' + key,
                        hasSendText: typeof val.sendText === 'function',
                        hasGetTeams: typeof val.getTeams === 'function',
                        hasConnect: typeof val.connect === 'function',
                        methods: Object.keys(val).filter(k => typeof val[k] === 'function').slice(0, 50)
            });
                }
                
                // 检查第二层
                for (let subKey in val) {
                    try {
                        const subVal = val[subKey];
                        if (subVal && typeof subVal === 'object' && 
                            (typeof subVal.sendText === 'function' || typeof subVal.getTeams === 'function')) {
                            result.found.push({
                                path: 'window.' + key + '.' + subKey,
                                hasSendText: typeof subVal.sendText === 'function',
                                hasGetTeams: typeof subVal.getTeams === 'function',
                                methods: Object.keys(subVal).filter(k => typeof subVal[k] === 'function').slice(0, 50)
                            });
                        }
                    } catch(e) {}
                }
            }
        } catch(e) {}
    }
    
    // 检查Vue app
    if (window.__vue_app__) {
        result.vueApp = {
            exists: true,
            provides: Object.keys(window.__vue_app__._context.provides || {}).slice(0, 20)
        };
    }
    
    // 检查pinia
    if (window.__pinia) {
        result.pinia = {
            exists: true,
            stores: []
        };
        if (window.__pinia._s) {
            window.__pinia._s.forEach((store, name) => {
                result.pinia.stores.push(name);
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
                        expression: deepSearchCode,
                        returnByValue: true
                        }
                }));
            }, 3000);
        });

        ws.on('message', (data) => {
            const msg = JSON.parse(data.toString());
            
            if (msg.id) {
                console.log(`\n=== 响应 #${msg.id} ===`);
                
                if (msg.result) {
                    if (msg.result.result && msg.result.result.value) {
                        console.log(JSON.stringify(msg.result.result.value, null, 2));
                    } else if (msg.result.frameTree) {
                        console.log('Frame Tree:', JSON.stringify(msg.result.frameTree, null, 2));
                    } else if (msg.result.names) {
                        console.log('Lexical scope names:', msg.result.names);
                    } else {
                        console.log(JSON.stringify(msg.result, null, 2));
                    }
                }
                
                if (msg.error) {
                    console.log('错误:', msg.error);
                }
                
                responses[msg.id] = msg;
            }
        });

        ws.on('error', (err) => {
            console.error('WebSocket错误:', err);
            reject(err);
        });

        // 5秒后结束
        setTimeout(() => {
            console.log('\n===== 探索完成 =====');
    ws.close();
            resolve(responses);
        }, 6000);
    });
}

explore().then(() => {
    process.exit(0);
}).catch(err => {
    console.error('错误:', err);
    process.exit(1);
});
