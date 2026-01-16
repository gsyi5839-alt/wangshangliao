/**
 * 检查 IndexedDB 中的群成员数据
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
    
    // 检查 IndexedDB 
    const script = `
(async function() {
    var result = {
        success: false,
        error: null
    };
    
    try {
        // 获取 IndexedDB 数据库列表
        var dbs = await indexedDB.databases();
        result.databases = dbs.map(function(db) { return db.name; });
        
        // 打开 NIM 数据库
        var nimDbName = 'NIM-b03cfcd909dbf05c25163cc8c7e7b6cf-1948408648';
        
        var db = await new Promise(function(resolve, reject) {
            var request = indexedDB.open(nimDbName);
            request.onerror = function() { reject(request.error); };
            request.onsuccess = function() { resolve(request.result); };
        });
        
        // 获取所有 object stores
        result.objectStores = Array.from(db.objectStoreNames);
        
        // 检查群成员相关的 store
        var memberStores = result.objectStores.filter(function(s) {
            return s.toLowerCase().includes('member') || 
                   s.toLowerCase().includes('team') ||
                   s.toLowerCase().includes('user');
        });
        result.memberStores = memberStores;
        
        // 读取 team-members store 的数据
        if (db.objectStoreNames.contains('team-members')) {
            var tx = db.transaction('team-members', 'readonly');
            var store = tx.objectStore('team-members');
            
            var count = await new Promise(function(resolve, reject) {
                var request = store.count();
                request.onsuccess = function() { resolve(request.result); };
                request.onerror = function() { reject(request.error); };
            });
            
            result.teamMembersCount = count;
            
            // 获取前20条记录
            var members = await new Promise(function(resolve, reject) {
                var request = store.getAll(null, 50);
                request.onsuccess = function() { resolve(request.result); };
                request.onerror = function() { reject(request.error); };
            });
            
            result.sampleMembers = members.slice(0, 10).map(function(m) {
                return {
                    id: m.id,
                    teamId: m.teamId,
                    account: m.account,
                    nick: m.nick,
                    nickInTeam: m.nickInTeam,
                    customKeys: m.custom ? Object.keys(JSON.parse(m.custom || '{}')) : [],
                    allKeys: Object.keys(m)
                };
            });
        }
        
        // 读取 users store 的数据
        if (db.objectStoreNames.contains('users')) {
            var tx = db.transaction('users', 'readonly');
            var store = tx.objectStore('users');
            
            var count = await new Promise(function(resolve, reject) {
                var request = store.count();
                request.onsuccess = function() { resolve(request.result); };
                request.onerror = function() { reject(request.error); };
            });
            
            result.usersCount = count;
            
            // 获取前20条记录
            var users = await new Promise(function(resolve, reject) {
                var request = store.getAll(null, 50);
                request.onsuccess = function() { resolve(request.result); };
                request.onerror = function() { reject(request.error); };
            });
            
            result.sampleUsers = users.slice(0, 10).map(function(u) {
                return {
                    account: u.account,
                    nick: u.nick,
                    customKeys: u.custom ? Object.keys(JSON.parse(u.custom || '{}')) : [],
                    allKeys: Object.keys(u)
                };
            });
        }
        
        db.close();
        result.success = true;
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})();`;

    const data = await evaluate(script);
    console.log('=== IndexedDB 分析 ===\n');
    console.log(data);
    
    ws.close();
}

main().catch(console.error);

