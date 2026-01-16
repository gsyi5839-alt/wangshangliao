/**
 * 直接调用旺商聊内部 API 获取群成员
 */
const WebSocket = require('ws');
const http = require('http');

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
    console.log('Connecting to WangShangLiao...\n');
    
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
    
    // 调用旺商聊内部 API 获取群成员
    const script = `
(async function() {
    var result = {
        success: false,
        members: [],
        error: null
    };
    
    try {
        // 获取 API 模块
        // 旺商聊使用 import.meta.glob 导入模块，我们需要找到 handleReq 函数
        
        // 方法1: 尝试从 window 对象找到 API 函数
        var apiModule = null;
        
        // 查找含有 handleReq 的对象
        for (var key in window) {
            try {
                var obj = window[key];
                if (obj && typeof obj.handleReq === 'function') {
                    apiModule = obj;
                    break;
                }
            } catch(e) {}
        }
        
        if (!apiModule) {
            // 尝试从 Vue app 获取
            var app = document.querySelector('#app');
            if (app && app.__vue_app__) {
                var config = app.__vue_app__._context.config;
                if (config && config.globalProperties) {
                    for (var prop in config.globalProperties) {
                        try {
                            var gp = config.globalProperties[prop];
                            if (gp && typeof gp.handleReq === 'function') {
                                apiModule = gp;
                                break;
                            }
                        } catch(e) {}
                    }
                }
            }
        }
        
        result.apiModuleFound = !!apiModule;
        
        // 方法2: 直接发送 HTTP 请求到旺商聊后端
        // 首先获取认证 token
        var token = null;
        try {
            // 从 localStorage 获取 token
            var authData = localStorage.getItem('token') || 
                           localStorage.getItem('auth') || 
                           localStorage.getItem('accessToken');
            if (authData) {
                token = authData;
            }
            
            // 从 sessionStorage 获取
            if (!token) {
                token = sessionStorage.getItem('token') || 
                        sessionStorage.getItem('auth') || 
                        sessionStorage.getItem('accessToken');
            }
        } catch(e) {}
        
        result.tokenFound = !!token;
        
        // 方法3: 查找已缓存的群成员数据
        // 遍历 localStorage 查找群成员缓存
        var cachedMembers = [];
        try {
            for (var i = 0; i < localStorage.length; i++) {
                var key = localStorage.key(i);
                if (key && (key.includes('gMembers') || key.includes('groupMember'))) {
                    var value = localStorage.getItem(key);
                    if (value) {
                        try {
                            var data = JSON.parse(value);
                            if (data && data.groupMemberInfo) {
                                cachedMembers.push({
                                    key: key,
                                    memberCount: data.groupMemberInfo.length,
                                    sampleMembers: data.groupMemberInfo.slice(0, 5).map(function(m) {
                                        return {
                                            accountId: m.accountId,
                                            userNick: m.userNick,
                                            groupMemberNick: m.groupMemberNick,
                                            nimId: m.nimId
                                        };
                                    })
                                });
                            }
                        } catch(e) {}
                    }
                }
            }
        } catch(e) {}
        
        result.cachedMembers = cachedMembers;
        
        // 方法4: 从 IndexedDB 获取
        try {
            var dbs = await indexedDB.databases();
            result.indexedDBs = dbs.map(function(db) { return db.name; });
        } catch(e) {}
        
        result.success = true;
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})();`;

    const data = await evaluate(script);
    console.log('=== 旺商聊内部 API 测试 ===\n');
    console.log(data);
    
    // 检查 localStorage 中的所有 key
    console.log('\n=== 检查 localStorage ===\n');
    const storageScript = `
(function() {
    var keys = [];
    for (var i = 0; i < localStorage.length; i++) {
        var key = localStorage.key(i);
        var value = localStorage.getItem(key);
        keys.push({
            key: key,
            valueLength: value ? value.length : 0,
            valuePreview: value ? value.substring(0, 100) : ''
        });
    }
    return JSON.stringify(keys.filter(function(k) { return k.valueLength > 100; }).slice(0, 20), null, 2);
})();`;
    
    const storageData = await evaluate(storageScript, false);
    console.log(storageData);
    
    ws.close();
}

main().catch(console.error);

