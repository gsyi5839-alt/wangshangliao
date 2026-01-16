// Monitor network requests for nickname decryption API
const WebSocket = require('ws');

async function run() {
    console.log('=== Monitor Decrypt API Requests ===\n');
    
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
    const networkRequests = [];
    
    ws.on('message', (data) => {
        const msg = JSON.parse(data.toString());
        
        // Handle network events
        if (msg.method === 'Network.responseReceived') {
            const url = msg.params?.response?.url || '';
            if (url.includes('nick') || url.includes('decrypt') || url.includes('user')) {
                networkRequests.push({
                    type: 'response',
                    url: url,
                    status: msg.params?.response?.status
                });
            }
        }
        
        if (msg.id && pending.has(msg.id)) {
            pending.get(msg.id)(msg);
            pending.delete(msg.id);
        }
    });
    
    await new Promise(resolve => ws.on('open', resolve));
    console.log('Connected!\n');
    
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
    
    // Enable network monitoring
    await sendCommand('Network.enable');
    console.log('Network monitoring enabled\n');
    
    // First, let's see if there's a decrypt function being called
    console.log('=== 1. Look for decrypt function in Vue components ===\n');
    
    const decryptFunction = await evaluate(`
(function() {
    var results = [];
    
    // Check all Vue instances for decrypt-related methods
    function findDecryptInVue(el, depth) {
        if (depth > 8) return;
        
        var vueComponent = el.__vueParentComponent;
        if (vueComponent) {
            var type = vueComponent.type || {};
            var methods = type.methods || {};
            var computed = type.computed || {};
            var setup = type.setup;
            
            // Check for decrypt-related functions
            var methodNames = Object.keys(methods);
            var computedNames = Object.keys(computed);
            
            var decryptMethods = [...methodNames, ...computedNames].filter(function(name) {
                return name.toLowerCase().includes('decrypt') ||
                       name.toLowerCase().includes('nick') ||
                       name.toLowerCase().includes('displayname');
            });
            
            if (decryptMethods.length > 0) {
                results.push({
                    component: type.name || 'anonymous',
                    decryptMethods: decryptMethods,
                    element: el.tagName
                });
            }
            
            // Also check if setup returns a decrypt function
            if (vueComponent.setupState) {
                var setupKeys = Object.keys(vueComponent.setupState);
                var decryptSetup = setupKeys.filter(function(key) {
                    return key.toLowerCase().includes('decrypt') ||
                           key.toLowerCase().includes('displayname') ||
                           (key.toLowerCase().includes('nick') && typeof vueComponent.setupState[key] === 'function');
                });
                
                if (decryptSetup.length > 0) {
                    results.push({
                        component: type.name || 'anonymous',
                        setupFunctions: decryptSetup,
                        element: el.tagName
                    });
                }
            }
        }
        
        Array.from(el.children || []).forEach(function(child) {
            findDecryptInVue(child, depth + 1);
        });
    }
    
    findDecryptInVue(document.body, 0);
    
    return JSON.stringify(results, null, 2);
})();
    `);
    console.log('Decrypt Functions:', decryptFunction);
    
    // Check if there's a utility function for nickname decryption
    console.log('\n=== 2. Check for nickname utility functions ===\n');
    
    const utilityFunctions = await evaluate(`
(function() {
    var results = {
        found: [],
        checked: []
    };
    
    // Common places where utility functions might be stored
    var namespaces = [
        { name: 'window.utils', obj: window.utils },
        { name: 'window.Utils', obj: window.Utils },
        { name: 'window.$utils', obj: window.$utils },
        { name: 'window.helper', obj: window.helper },
        { name: 'window.Helper', obj: window.Helper },
        { name: 'window.api', obj: window.api },
        { name: 'window.API', obj: window.API },
        { name: 'window.nim', obj: window.nim },
        { name: 'window.NIM', obj: window.NIM }
    ];
    
    namespaces.forEach(function(ns) {
        if (ns.obj && typeof ns.obj === 'object') {
            results.checked.push(ns.name);
            
            var keys = Object.keys(ns.obj);
            var relevantKeys = keys.filter(function(k) {
                return k.toLowerCase().includes('decrypt') ||
                       k.toLowerCase().includes('nick') ||
                       k.toLowerCase().includes('name') ||
                       k.toLowerCase().includes('cipher');
            });
            
            if (relevantKeys.length > 0) {
                relevantKeys.forEach(function(key) {
                    results.found.push({
                        namespace: ns.name,
                        key: key,
                        type: typeof ns.obj[key]
                    });
                });
            }
        }
    });
    
    // Check nim object methods
    if (window.nim) {
        var nimMethods = Object.keys(window.nim).filter(function(k) {
            return typeof window.nim[k] === 'function';
        });
        
        var nickMethods = nimMethods.filter(function(m) {
            return m.toLowerCase().includes('nick') ||
                   m.toLowerCase().includes('user') ||
                   m.toLowerCase().includes('name');
        });
        
        results.nimNickMethods = nickMethods;
    }
    
    return JSON.stringify(results, null, 2);
})();
    `);
    console.log('Utility Functions:', utilityFunctions);
    
    // Try calling getUser on a specific account
    console.log('\n=== 3. Try to get user info via different methods ===\n');
    
    const userInfoTest = await evaluate(`
(async function() {
    var results = {};
    var testAccount = '1391351554'; // Account with MD5 hash nick
    
    if (!window.nim) return JSON.stringify({ error: 'nim not found' });
    
    // Method 1: getUser
    try {
        var user = await new Promise(function(resolve, reject) {
            window.nim.getUser({
                account: testAccount,
                done: function(err, data) {
                    if (err) reject(err);
                    else resolve(data);
                }
            });
            setTimeout(function() { reject('timeout'); }, 5000);
        });
        results.getUser = user;
    } catch (e) {
        results.getUserError = e.toString();
    }
    
    // Method 2: getUsers (batch)
    try {
        var users = await new Promise(function(resolve, reject) {
            window.nim.getUsers({
                accounts: [testAccount],
                done: function(err, data) {
                    if (err) reject(err);
                    else resolve(data);
                }
            });
            setTimeout(function() { reject('timeout'); }, 5000);
        });
        results.getUsers = users;
    } catch (e) {
        results.getUsersError = e.toString();
    }
    
    // Method 3: fetchUserNameCard
    try {
        if (typeof window.nim.fetchUserNameCard === 'function') {
            var nameCard = await new Promise(function(resolve, reject) {
                window.nim.fetchUserNameCard({
                    accounts: [testAccount],
                    done: function(err, data) {
                        if (err) reject(err);
                        else resolve(data);
                    }
                });
                setTimeout(function() { reject('timeout'); }, 5000);
            });
            results.fetchUserNameCard = nameCard;
        } else {
            results.fetchUserNameCard = 'method not available';
        }
    } catch (e) {
        results.fetchUserNameCardError = e.toString();
    }
    
    // Method 4: Check if there's a decrypt method on nim
    var decryptMethods = Object.keys(window.nim).filter(function(k) {
        return k.toLowerCase().includes('decrypt');
    });
    results.nimDecryptMethods = decryptMethods;
    
    return JSON.stringify(results, null, 2);
})();
    `, true);
    console.log('User Info Test:', userInfoTest);
    
    // Check the Vue app's global properties for decrypt function
    console.log('\n=== 4. Check Vue app global properties ===\n');
    
    const vueGlobals = await evaluate(`
(function() {
    var results = {};
    
    var app = document.querySelector('#app')?.__vue_app__;
    if (!app) return JSON.stringify({ error: 'Vue app not found' });
    
    var config = app.config || {};
    var globalProperties = config.globalProperties || {};
    
    results.globalPropertyKeys = Object.keys(globalProperties);
    
    // Look for decrypt/nick related globals
    var relevantGlobals = {};
    Object.keys(globalProperties).forEach(function(key) {
        if (key.toLowerCase().includes('decrypt') ||
            key.toLowerCase().includes('nick') ||
            key.toLowerCase().includes('util') ||
            key.toLowerCase().includes('api')) {
            relevantGlobals[key] = typeof globalProperties[key];
        }
    });
    
    results.relevantGlobals = relevantGlobals;
    
    // Check provides
    var provides = app._context?.provides || {};
    results.provideKeys = Object.keys(provides).slice(0, 20);
    
    return JSON.stringify(results, null, 2);
})();
    `);
    console.log('Vue Globals:', vueGlobals);
    
    // Try to trigger a member click and intercept the decrypted name
    console.log('\n=== 5. Analyze how member list gets its names ===\n');
    
    const memberListAnalysis = await evaluate(`
(function() {
    var results = {
        approach: 'Analyzing member list rendering'
    };
    
    // Find member list component
    var memberElements = document.querySelectorAll('p.inline-block.truncate.color-\\\\#171717');
    
    memberElements.forEach(function(el, i) {
        if (i > 3) return;
        
        var name = el.textContent.trim();
        
        // Walk up to find the Vue component
        var parent = el;
        for (var d = 0; d < 10 && parent; d++) {
            var vueComp = parent.__vueParentComponent;
            if (vueComp) {
                var props = vueComp.props || {};
                var state = vueComp.setupState || {};
                var ctx = vueComp.ctx || {};
                
                // Log what data the component has
                results['element_' + i] = {
                    displayedName: name,
                    propsKeys: Object.keys(props),
                    setupStateKeys: Object.keys(state),
                    ctxKeys: Object.keys(ctx).slice(0, 20)
                };
                
                // Check for row/data property
                if (state.row || props.row || ctx.row) {
                    var row = state.row || props.row || ctx.row;
                    results['element_' + i].rowData = {
                        account: row?.account,
                        nick: row?.nick,
                        nickInTeam: row?.nickInTeam,
                        displayName: row?.displayName
                    };
                }
                
                if (state.rowData || props.rowData) {
                    var rowData = state.rowData || props.rowData;
                    results['element_' + i].rowDataObj = rowData;
                }
                
                break;
            }
            parent = parent.parentElement;
        }
    });
    
    return JSON.stringify(results, null, 2);
})();
    `);
    console.log('Member List Analysis:', memberListAnalysis);
    
    // Final: Check if there's a computed property that decrypts nicknames
    console.log('\n=== 6. Try to find the decrypt mechanism ===\n');
    
    const decryptMechanism = await evaluate(`
(async function() {
    var results = {};
    
    // Find member grid element
    var gridContainer = document.querySelector('[class*="el-table-v2"]');
    if (!gridContainer) {
        results.error = 'Grid container not found';
        return JSON.stringify(results);
    }
    
    // Find the parent Vue component
    var parent = gridContainer;
    for (var d = 0; d < 20 && parent; d++) {
        var vueComp = parent.__vueParentComponent;
        if (vueComp && vueComp.setupState) {
            var state = vueComp.setupState;
            
            // Look for members or list data
            var stateKeys = Object.keys(state);
            results.foundComponent = {
                depth: d,
                componentName: vueComp.type?.name,
                stateKeys: stateKeys
            };
            
            // Check for data that contains members
            stateKeys.forEach(function(key) {
                var val = state[key];
                if (Array.isArray(val) && val.length > 0) {
                    var sample = val[0];
                    if (sample && (sample.account || sample.nick || sample.nickInTeam)) {
                        results['data_' + key] = {
                            length: val.length,
                            sample: {
                                account: sample.account,
                                nick: sample.nick,
                                nickInTeam: sample.nickInTeam,
                                displayName: sample.displayName,
                                name: sample.name
                            },
                            allKeys: Object.keys(sample)
                        };
                    }
                }
            });
            
            // Check for computed properties
            if (vueComp.type?.computed) {
                results.computedProps = Object.keys(vueComp.type.computed);
            }
            
            // Check for methods
            if (vueComp.type?.methods) {
                var methodNames = Object.keys(vueComp.type.methods);
                var relevantMethods = methodNames.filter(function(m) {
                    return m.includes('nick') || m.includes('name') || m.includes('decrypt');
                });
                if (relevantMethods.length > 0) {
                    results.relevantMethods = relevantMethods;
                }
            }
        }
        parent = parent.parentElement;
    }
    
    return JSON.stringify(results, null, 2);
})();
    `);
    console.log('Decrypt Mechanism:', decryptMechanism);
    
    ws.close();
    console.log('\n=== Done ===');
}

run().catch(console.error);

