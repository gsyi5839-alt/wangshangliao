// Check the correct IndexedDB database
const WebSocket = require('ws');

async function run() {
    console.log('=== Check Correct IndexedDB Database ===\n');
    
    const http = require('http');
    const targets = await new Promise((resolve, reject) => {
        http.get('http://127.0.0.1:9222/json', (res) => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', () => resolve(JSON.parse(data)));
        }).on('error', reject);
    });
    
    const pageTarget = targets.find(t => t.type === 'page' && t.url.includes('wangshangliao'));
    if (!pageTarget) {
        console.log('No WangShangLiao page found');
        return;
    }
    
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
    
    function sendCommand(method, params = {}) {
        return new Promise((resolve) => {
            const id = msgId++;
            pending.set(id, resolve);
            ws.send(JSON.stringify({ id, method, params }));
        });
    }
    
    async function evaluate(expression, awaitPromise = false) {
        const result = await sendCommand('Runtime.evaluate', {
            expression,
            returnByValue: true,
            awaitPromise
        });
        return result.result?.result?.value;
    }
    
    // Open the correct database
    console.log('=== 1. Open nim-1948408648 Database ===\n');
    
    const dbData = await evaluate(`
(async function() {
    var results = {};
    
    return new Promise(function(resolve) {
        var request = indexedDB.open('nim-1948408648');
        
        request.onerror = function(e) {
            results.error = e.target.error?.message || 'Unknown error';
            resolve(JSON.stringify(results, null, 2));
        };
        
        request.onsuccess = function(e) {
            var db = e.target.result;
            results.name = db.name;
            results.version = db.version;
            results.stores = Array.from(db.objectStoreNames);
            
            db.close();
            resolve(JSON.stringify(results, null, 2));
        };
    });
})();
    `, true);
    console.log('Database Info:', dbData);
    
    // Get data from all stores
    console.log('\n=== 2. Get Data from All Stores ===\n');
    
    const allStoresData = await evaluate(`
(async function() {
    var results = {};
    
    return new Promise(function(resolve) {
        var request = indexedDB.open('nim-1948408648');
        
        request.onerror = function(e) {
            results.error = e.target.error?.message;
            resolve(JSON.stringify(results, null, 2));
        };
        
        request.onsuccess = function(e) {
            var db = e.target.result;
            var storeNames = Array.from(db.objectStoreNames);
            
            if (storeNames.length === 0) {
                results.note = 'No stores found';
                db.close();
                resolve(JSON.stringify(results, null, 2));
                return;
            }
            
            var promises = storeNames.map(function(storeName) {
                return new Promise(function(resolveStore) {
                    try {
                        var tx = db.transaction(storeName, 'readonly');
                        var store = tx.objectStore(storeName);
                        var getAllRequest = store.getAll();
                        
                        getAllRequest.onsuccess = function() {
                            var data = getAllRequest.result || [];
                            
                            // Check if this store has user/nick related data
                            var hasUserData = data.some(function(item) {
                                if (item && typeof item === 'object') {
                                    return 'account' in item || 'nick' in item || 'nickInTeam' in item;
                                }
                                return false;
                            });
                            
                            results[storeName] = {
                                count: data.length,
                                hasUserData: hasUserData,
                                sampleKeys: data.length > 0 && typeof data[0] === 'object' ? 
                                    Object.keys(data[0]).slice(0, 20) : [],
                                samples: hasUserData ? data.slice(0, 5).map(function(item) {
                                    return {
                                        account: item.account,
                                        nick: item.nick,
                                        nickInTeam: item.nickInTeam,
                                        from: item.from,
                                        fromNick: item.fromNick
                                    };
                                }) : null
                            };
                            resolveStore();
                        };
                        
                        getAllRequest.onerror = function() {
                            results[storeName] = { error: 'Failed to read' };
                            resolveStore();
                        };
                    } catch (e) {
                        results[storeName] = { error: e.message };
                        resolveStore();
                    }
                });
            });
            
            Promise.all(promises).then(function() {
                db.close();
                resolve(JSON.stringify(results, null, 2));
            });
        };
    });
})();
    `, true);
    console.log('All Stores Data:', allStoresData);
    
    // Check stores with user data in detail
    console.log('\n=== 3. Check User-Related Stores in Detail ===\n');
    
    const userStoresDetail = await evaluate(`
(async function() {
    var results = {};
    
    return new Promise(function(resolve) {
        var request = indexedDB.open('nim-1948408648');
        
        request.onerror = function(e) {
            results.error = e.target.error?.message;
            resolve(JSON.stringify(results, null, 2));
        };
        
        request.onsuccess = function(e) {
            var db = e.target.result;
            var storeNames = Array.from(db.objectStoreNames);
            
            // Focus on stores likely to have user data
            var targetStores = storeNames.filter(function(name) {
                var n = name.toLowerCase();
                return n.includes('user') || n.includes('friend') || 
                       n.includes('team') || n.includes('member') ||
                       n.includes('msg') || n.includes('session');
            });
            
            results.targetStores = targetStores;
            
            if (targetStores.length === 0) {
                // If no obvious stores, check all
                targetStores = storeNames;
            }
            
            var promises = targetStores.slice(0, 10).map(function(storeName) {
                return new Promise(function(resolveStore) {
                    try {
                        var tx = db.transaction(storeName, 'readonly');
                        var store = tx.objectStore(storeName);
                        var getAllRequest = store.getAll();
                        
                        getAllRequest.onsuccess = function() {
                            var data = getAllRequest.result || [];
                            
                            // Find items with plaintext Chinese nicknames
                            var chineseNickItems = data.filter(function(item) {
                                if (!item || typeof item !== 'object') return false;
                                
                                var nick = item.nick || item.fromNick || item.nickInTeam || item.name;
                                if (!nick) return false;
                                
                                // Check if nick is Chinese (not MD5)
                                var md5Pattern = /^[a-f0-9]{32}$/i;
                                if (md5Pattern.test(nick)) return false;
                                
                                // Check for Chinese characters
                                var chinesePattern = /[\\u4e00-\\u9fa5]/;
                                return chinesePattern.test(nick);
                            });
                            
                            results[storeName] = {
                                totalCount: data.length,
                                chineseNickCount: chineseNickItems.length,
                                chineseNickSamples: chineseNickItems.slice(0, 10).map(function(item) {
                                    return {
                                        account: item.account || item.from,
                                        nick: item.nick,
                                        fromNick: item.fromNick,
                                        nickInTeam: item.nickInTeam,
                                        name: item.name
                                    };
                                })
                            };
                            resolveStore();
                        };
                        
                        getAllRequest.onerror = function() {
                            results[storeName] = { error: 'Failed to read' };
                            resolveStore();
                        };
                    } catch (e) {
                        results[storeName] = { error: e.message };
                        resolveStore();
                    }
                });
            });
            
            Promise.all(promises).then(function() {
                db.close();
                resolve(JSON.stringify(results, null, 2));
            });
        };
    });
})();
    `, true);
    console.log('User Stores Detail:', userStoresDetail);
    
    // Final: Build account-nickname mapping from all sources
    console.log('\n=== 4. Build Complete Account-Nickname Mapping ===\n');
    
    const finalMapping = await evaluate(`
(async function() {
    var mapping = {};
    var md5Pattern = /^[a-f0-9]{32}$/i;
    var chinesePattern = /[\\u4e00-\\u9fa5]/;
    
    // Source 1: Messages from NIM SDK
    try {
        if (window.nim) {
            var msgs = await new Promise(function(resolve, reject) {
                window.nim.getLocalMsgs({
                    sessionId: 'team-40821608989',
                    limit: 500,
                    done: function(err, obj) {
                        if (err) reject(err);
                        else resolve(obj);
                    }
                });
                setTimeout(function() { reject('timeout'); }, 10000);
            });
            
            var msgList = msgs.msgs || msgs || [];
            msgList.forEach(function(m) {
                if (m.from && m.fromNick && !md5Pattern.test(m.fromNick)) {
                    mapping[m.from] = m.fromNick;
                }
            });
        }
    } catch (e) {}
    
    // Source 2: Team members with nickInTeam
    try {
        if (window.nim) {
            var teamData = await new Promise(function(resolve, reject) {
                window.nim.getTeamMembers({
                    teamId: '40821608989',
                    done: function(err, obj) {
                        if (err) reject(err);
                        else resolve(obj);
                    }
                });
                setTimeout(function() { reject('timeout'); }, 5000);
            });
            
            var members = teamData.members || [];
            members.forEach(function(m) {
                if (m.account && m.nickInTeam && !md5Pattern.test(m.nickInTeam)) {
                    mapping[m.account] = m.nickInTeam;
                }
            });
        }
    } catch (e) {}
    
    // Source 3: IndexedDB
    try {
        var db = await new Promise(function(resolve, reject) {
            var request = indexedDB.open('nim-1948408648');
            request.onerror = function() { reject(request.error); };
            request.onsuccess = function() { resolve(request.result); };
        });
        
        var storeNames = Array.from(db.objectStoreNames);
        
        for (var storeName of storeNames) {
            try {
                var tx = db.transaction(storeName, 'readonly');
                var store = tx.objectStore(storeName);
                var data = await new Promise(function(resolve, reject) {
                    var getAllRequest = store.getAll();
                    getAllRequest.onsuccess = function() { resolve(getAllRequest.result); };
                    getAllRequest.onerror = function() { reject(getAllRequest.error); };
                });
                
                data.forEach(function(item) {
                    if (!item || typeof item !== 'object') return;
                    
                    var account = item.account || item.from;
                    var nick = item.nick || item.fromNick || item.nickInTeam;
                    
                    if (account && nick && !md5Pattern.test(nick) && chinesePattern.test(nick)) {
                        if (!mapping[account]) {
                            mapping[account] = nick;
                        }
                    }
                });
            } catch (e) {}
        }
        
        db.close();
    } catch (e) {}
    
    // Output results
    var entries = Object.entries(mapping);
    return JSON.stringify({
        totalMappings: entries.length,
        mappings: entries.map(function([account, nick]) {
            return { account: account, nick: nick };
        })
    }, null, 2);
})();
    `, true);
    console.log('Final Mapping:', finalMapping);
    
    ws.close();
    console.log('\n=== Done ===');
}

run().catch(console.error);

