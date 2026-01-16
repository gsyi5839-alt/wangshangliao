// Check IndexedDB for nickname mappings
const WebSocket = require('ws');

async function run() {
    console.log('=== Check IndexedDB for Nickname Mappings ===\n');
    
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
    
    // Check IndexedDB databases
    console.log('=== 1. List IndexedDB Databases ===\n');
    
    const dbList = await evaluate(`
(async function() {
    var results = {};
    
    try {
        var dbs = await indexedDB.databases();
        results.databases = dbs.map(function(db) {
            return { name: db.name, version: db.version };
        });
    } catch (e) {
        results.error = e.message;
    }
    
    return JSON.stringify(results, null, 2);
})();
    `, true);
    console.log('IndexedDB Databases:', dbList);
    
    // Open NIM database and look for user data
    console.log('\n=== 2. Check NIM Database for Users ===\n');
    
    const nimDbData = await evaluate(`
(async function() {
    var results = { stores: [], sampleData: {} };
    
    return new Promise(function(resolve) {
        // Try to open the NIM database
        var request = indexedDB.open('NIM-b03cfcd909dbf05c25163cc8c7e7b6cf-1948408648');
        
        request.onerror = function(e) {
            results.error = 'Failed to open DB: ' + e.target.error;
            resolve(JSON.stringify(results, null, 2));
        };
        
        request.onsuccess = function(e) {
            var db = e.target.result;
            results.stores = Array.from(db.objectStoreNames);
            
            // Look for user-related stores
            var userStores = results.stores.filter(function(name) {
                return name.toLowerCase().includes('user') ||
                       name.toLowerCase().includes('friend') ||
                       name.toLowerCase().includes('member') ||
                       name.toLowerCase().includes('nick') ||
                       name.toLowerCase().includes('team');
            });
            
            results.userRelatedStores = userStores;
            
            if (userStores.length === 0) {
                db.close();
                resolve(JSON.stringify(results, null, 2));
                return;
            }
            
            // Read data from each user-related store
            var promises = userStores.map(function(storeName) {
                return new Promise(function(resolveStore) {
                    try {
                        var tx = db.transaction(storeName, 'readonly');
                        var store = tx.objectStore(storeName);
                        var getAllRequest = store.getAll();
                        
                        getAllRequest.onsuccess = function() {
                            var data = getAllRequest.result;
                            // Get first 5 items as sample
                            results.sampleData[storeName] = {
                                count: data.length,
                                samples: data.slice(0, 5)
                            };
                            resolveStore();
                        };
                        
                        getAllRequest.onerror = function() {
                            results.sampleData[storeName] = { error: 'Failed to read' };
                            resolveStore();
                        };
                    } catch (e) {
                        results.sampleData[storeName] = { error: e.message };
                        resolveStore();
                    }
                });
            });
            
            Promise.all(promises).then(function() {
                db.close();
                resolve(JSON.stringify(results, null, 2));
            });
        };
        
        request.onupgradeneeded = function(e) {
            results.note = 'Database was just created';
            e.target.transaction.abort();
            resolve(JSON.stringify(results, null, 2));
        };
    });
})();
    `, true);
    console.log('NIM Database Data:', nimDbData);
    
    // Check for a separate nickname cache database
    console.log('\n=== 3. Check All Stores in Main DB ===\n');
    
    const allStoresData = await evaluate(`
(async function() {
    var results = { stores: {} };
    
    return new Promise(function(resolve) {
        var request = indexedDB.open('NIM-b03cfcd909dbf05c25163cc8c7e7b6cf-1948408648');
        
        request.onerror = function(e) {
            results.error = e.target.error?.message || 'Unknown error';
            resolve(JSON.stringify(results, null, 2));
        };
        
        request.onsuccess = function(e) {
            var db = e.target.result;
            var storeNames = Array.from(db.objectStoreNames);
            
            var promises = storeNames.map(function(storeName) {
                return new Promise(function(resolveStore) {
                    try {
                        var tx = db.transaction(storeName, 'readonly');
                        var store = tx.objectStore(storeName);
                        var countRequest = store.count();
                        
                        countRequest.onsuccess = function() {
                            var count = countRequest.result;
                            
                            // If store has data, get first item
                            if (count > 0) {
                                var getOneRequest = store.openCursor();
                                getOneRequest.onsuccess = function(evt) {
                                    var cursor = evt.target.result;
                                    results.stores[storeName] = {
                                        count: count,
                                        sampleKey: cursor?.key,
                                        sampleValue: cursor?.value ? JSON.stringify(cursor.value).slice(0, 300) : null
                                    };
                                    resolveStore();
                                };
                                getOneRequest.onerror = function() {
                                    results.stores[storeName] = { count: count, error: 'Failed to get cursor' };
                                    resolveStore();
                                };
                            } else {
                                results.stores[storeName] = { count: 0 };
                                resolveStore();
                            }
                        };
                        
                        countRequest.onerror = function() {
                            results.stores[storeName] = { error: 'Failed to count' };
                            resolveStore();
                        };
                    } catch (e) {
                        results.stores[storeName] = { error: e.message };
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
    
    // Check friends store for nickname data
    console.log('\n=== 4. Check Friend/User Stores in Detail ===\n');
    
    const friendDetailData = await evaluate(`
(async function() {
    var results = {};
    
    return new Promise(function(resolve) {
        var request = indexedDB.open('NIM-b03cfcd909dbf05c25163cc8c7e7b6cf-1948408648');
        
        request.onerror = function(e) {
            results.error = e.target.error?.message;
            resolve(JSON.stringify(results, null, 2));
        };
        
        request.onsuccess = function(e) {
            var db = e.target.result;
            
            // Look for specific stores
            var storeNames = ['friends', 'users', 'userInfos', 'teamMembers', 'roster'];
            var existingStores = storeNames.filter(function(name) {
                return db.objectStoreNames.contains(name);
            });
            
            if (existingStores.length === 0) {
                // Try to find any store that might have user data
                var allNames = Array.from(db.objectStoreNames);
                results.allStoreNames = allNames;
                db.close();
                resolve(JSON.stringify(results, null, 2));
                return;
            }
            
            var promises = existingStores.map(function(storeName) {
                return new Promise(function(resolveStore) {
                    try {
                        var tx = db.transaction(storeName, 'readonly');
                        var store = tx.objectStore(storeName);
                        var getAllRequest = store.getAll();
                        
                        getAllRequest.onsuccess = function() {
                            var data = getAllRequest.result || [];
                            
                            // Filter to show only items with nick/name fields
                            var nicksWithData = data.filter(function(item) {
                                return item && (item.nick || item.nickname || item.name || item.displayName);
                            }).slice(0, 20).map(function(item) {
                                return {
                                    account: item.account || item.id,
                                    nick: item.nick,
                                    nickname: item.nickname,
                                    name: item.name,
                                    displayName: item.displayName,
                                    alias: item.alias
                                };
                            });
                            
                            results[storeName] = {
                                totalCount: data.length,
                                withNickCount: nicksWithData.length,
                                samples: nicksWithData
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
    console.log('Friend Detail Data:', friendDetailData);
    
    // Check localStorage and sessionStorage
    console.log('\n=== 5. Check localStorage for Nickname Cache ===\n');
    
    const storageData = await evaluate(`
(function() {
    var results = {
        localStorage: {},
        sessionStorage: {}
    };
    
    // Check localStorage
    for (var i = 0; i < localStorage.length; i++) {
        var key = localStorage.key(i);
        if (key.includes('nick') || key.includes('name') || key.includes('user') || key.includes('member')) {
            var value = localStorage.getItem(key);
            results.localStorage[key] = value?.slice(0, 500);
        }
    }
    
    // Check sessionStorage
    for (var i = 0; i < sessionStorage.length; i++) {
        var key = sessionStorage.key(i);
        if (key.includes('nick') || key.includes('name') || key.includes('user') || key.includes('member')) {
            var value = sessionStorage.getItem(key);
            results.sessionStorage[key] = value?.slice(0, 500);
        }
    }
    
    // Also list all keys
    results.allLocalStorageKeys = [];
    for (var i = 0; i < localStorage.length; i++) {
        results.allLocalStorageKeys.push(localStorage.key(i));
    }
    
    return JSON.stringify(results, null, 2);
})();
    `);
    console.log('Storage Data:', storageData);
    
    // Final: Try to find the decrypted name cache
    console.log('\n=== 6. Search for Decrypted Name Cache ===\n');
    
    const decryptedCache = await evaluate(`
(async function() {
    var results = {
        sources: []
    };
    
    // Check if there's a Map or object that stores decrypted names
    // This might be in a closure but let's check global scope
    
    // Check window for any nick/name related properties
    for (var key in window) {
        if (key.toLowerCase().includes('nick') || key.toLowerCase().includes('decrypt')) {
            try {
                results.sources.push({
                    key: key,
                    type: typeof window[key],
                    sample: String(window[key]).slice(0, 100)
                });
            } catch (e) {}
        }
    }
    
    // Check nim object for cached data
    if (window.nim) {
        var nimKeys = Object.keys(window.nim);
        var cacheKeys = nimKeys.filter(function(k) {
            return k.includes('cache') || k.includes('map') || k.includes('store');
        });
        
        results.nimCacheKeys = cacheKeys;
        
        cacheKeys.forEach(function(k) {
            try {
                var val = window.nim[k];
                if (val && typeof val === 'object') {
                    var innerKeys = Object.keys(val).slice(0, 10);
                    results['nim.' + k] = innerKeys;
                }
            } catch (e) {}
        });
    }
    
    return JSON.stringify(results, null, 2);
})();
    `);
    console.log('Decrypted Cache:', decryptedCache);
    
    ws.close();
    console.log('\n=== Done ===');
}

run().catch(console.error);

