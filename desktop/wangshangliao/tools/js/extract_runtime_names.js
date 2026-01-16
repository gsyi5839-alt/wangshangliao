// Extract decrypted names directly from WangShangLiao runtime
const WebSocket = require('ws');

const CDP_URL = 'ws://127.0.0.1:9222';

async function run() {
    console.log('=== Extract Runtime Decrypted Names ===\n');
    
    // Get WebSocket debugger URL
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
        console.log('No WangShangLiao page found. Available:');
        targets.forEach(t => console.log(`  ${t.type}: ${t.url}`));
        return;
    }
    
    console.log(`Connecting to: ${pageTarget.url}\n`);
    
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
    
    // Strategy 1: Check if there's a decryption function being used
    console.log('Strategy 1: Look for decrypt function in window/Vue...\n');
    
    const decryptFunctions = await evaluate(`
(function() {
    var found = [];
    
    // Check window for decrypt-related functions
    for (var key in window) {
        if (key.toLowerCase().includes('decrypt') || 
            key.toLowerCase().includes('cipher') ||
            key.toLowerCase().includes('aes')) {
            found.push('window.' + key + ' = ' + typeof window[key]);
        }
    }
    
    // Check Vue instances
    var vueApps = document.querySelectorAll('[data-v-app], #app, .app');
    vueApps.forEach(function(el, i) {
        if (el.__vue_app__) {
            var config = el.__vue_app__.config;
            var global = config?.globalProperties || {};
            for (var key in global) {
                if (key.toLowerCase().includes('decrypt') ||
                    key.toLowerCase().includes('util')) {
                    found.push('Vue.global.' + key);
                }
            }
        }
    });
    
    // Check for common utility libraries
    if (window.CryptoJS) found.push('CryptoJS found');
    if (window.crypto) found.push('Web Crypto API available');
    if (window.JSEncrypt) found.push('JSEncrypt found');
    
    return found.join('\\n');
})();
    `);
    console.log('Decrypt functions:', decryptFunctions || 'None found');
    
    // Strategy 2: Hook into the team member display to get decrypted names
    console.log('\nStrategy 2: Extract names from Vue component state...\n');
    
    const vueComponentNames = await evaluate(`
(function() {
    var results = [];
    
    // Find all Vue components with member lists
    function extractFromVueEl(el, depth) {
        if (depth > 10) return;
        
        var vm = el.__vue__ || el.__vue_app__?.component;
        if (!vm) return;
        
        // Check data properties
        if (vm.$data) {
            for (var key in vm.$data) {
                var val = vm.$data[key];
                if (Array.isArray(val) && val.length > 0) {
                    val.slice(0, 3).forEach(function(item, i) {
                        if (item && (item.nick || item.nickname || item.name || item.displayName)) {
                            results.push({
                                component: el.tagName + '.' + (el.className || '').split(' ')[0],
                                key: key,
                                sample: {
                                    account: item.account || item.id,
                                    nick: item.nick || item.nickname || item.name || item.displayName
                                }
                            });
                        }
                    });
                }
            }
        }
        
        // Check children
        Array.from(el.children).forEach(function(child) {
            extractFromVueEl(child, depth + 1);
        });
    }
    
    extractFromVueEl(document.body, 0);
    return JSON.stringify(results, null, 2);
})();
    `);
    console.log('Vue component names:', vueComponentNames);
    
    // Strategy 3: Get names from displayed DOM elements with data attributes
    console.log('\nStrategy 3: Extract from DOM with data attributes...\n');
    
    const domNamesWithIds = await evaluate(`
(function() {
    var results = [];
    
    // Find member list items
    var memberItems = document.querySelectorAll('[class*="member"], [class*="user"], [class*="nick"], [data-account], [data-uid]');
    
    memberItems.forEach(function(el) {
        var account = el.getAttribute('data-account') || el.getAttribute('data-uid') || el.getAttribute('data-id');
        var textContent = el.textContent.trim();
        
        if (account && textContent && textContent.length < 50) {
            results.push({
                account: account,
                text: textContent.slice(0, 30),
                className: el.className
            });
        }
    });
    
    // Also check member list containers
    var containers = document.querySelectorAll('.team-members, .member-list, [class*="member-panel"]');
    containers.forEach(function(container) {
        var items = container.querySelectorAll('li, .item, [class*="member-item"]');
        items.forEach(function(item) {
            var nameEl = item.querySelector('[class*="name"], [class*="nick"], .nickname, .name');
            var idEl = item.querySelector('[class*="account"], [class*="id"]');
            
            if (nameEl) {
                results.push({
                    account: idEl ? idEl.textContent.trim() : 'unknown',
                    name: nameEl.textContent.trim(),
                    parentClass: container.className
                });
            }
        });
    });
    
    return JSON.stringify(results.slice(0, 20), null, 2);
})();
    `);
    console.log('DOM names with IDs:', domNamesWithIds);
    
    // Strategy 4: Intercept network responses containing decrypted names
    console.log('\nStrategy 4: Check Pinia/Vuex store state...\n');
    
    const storeState = await evaluate(`
(function() {
    var results = {};
    
    // Check Pinia stores
    if (window.__PINIA_STORE_ID_MAP__) {
        results.pinia = Object.keys(window.__PINIA_STORE_ID_MAP__);
    }
    
    // Check Vuex
    if (window.__VUEX__) {
        results.vuex = 'found';
    }
    
    // Check for team members in store
    var app = document.querySelector('#app')?.__vue_app__;
    if (app) {
        var provides = app._context?.provides;
        if (provides) {
            for (var key in provides) {
                if (key.includes('store') || key.includes('pinia')) {
                    results.provide = results.provide || [];
                    results.provide.push(key);
                }
            }
        }
    }
    
    // Check localStorage/sessionStorage for cached names
    var storageKeys = [];
    for (var i = 0; i < localStorage.length; i++) {
        var k = localStorage.key(i);
        if (k.includes('member') || k.includes('user') || k.includes('team') || k.includes('nick')) {
            storageKeys.push(k);
        }
    }
    results.localStorage = storageKeys;
    
    return JSON.stringify(results, null, 2);
})();
    `);
    console.log('Store state:', storeState);
    
    // Strategy 5: Access NIM SDK's internal data structure
    console.log('\nStrategy 5: Access NIM SDK internal cache...\n');
    
    const nimCache = await evaluate(`
(async function() {
    if (!window.nim) return 'nim not found';
    
    var results = {
        teams: [],
        users: []
    };
    
    // Check nim's internal data
    var nimProps = Object.keys(window.nim);
    
    // Look for internal cache/store
    for (var prop of nimProps) {
        var val = window.nim[prop];
        if (val && typeof val === 'object' && !Array.isArray(val)) {
            var subKeys = Object.keys(val).slice(0, 5);
            if (subKeys.some(k => k.includes('cache') || k.includes('store') || k.includes('map'))) {
                results.nimProp = { name: prop, keys: subKeys };
            }
        }
    }
    
    // Try to get team members with full info
    try {
        var teams = await new Promise((resolve, reject) => {
            window.nim.getTeams({
                done: function(err, obj) {
                    if (err) reject(err);
                    else resolve(obj);
                }
            });
            setTimeout(() => reject('timeout'), 5000);
        });
        
        if (teams && teams.teams) {
            for (var team of teams.teams.slice(0, 1)) {
                // Get members with decrypted info
                var members = await new Promise((resolve, reject) => {
                    window.nim.getTeamMembers({
                        teamId: team.teamId,
                        done: function(err, obj) {
                            if (err) reject(err);
                            else resolve(obj);
                        }
                    });
                    setTimeout(() => reject('timeout'), 5000);
                });
                
                if (members && members.members) {
                    results.teams.push({
                        teamId: team.teamId,
                        teamName: team.name,
                        members: members.members.slice(0, 10).map(function(m) {
                            return {
                                account: m.account,
                                nick: m.nick,
                                nickInTeam: m.nickInTeam,
                                displayName: m.displayName,
                                alias: m.alias,
                                custom: m.custom ? m.custom.slice(0, 100) : null
                            };
                        })
                    });
                }
            }
        }
    } catch (e) {
        results.error = e.toString();
    }
    
    return JSON.stringify(results, null, 2);
})();
    `, true);
    console.log('NIM Cache:', nimCache);
    
    // Strategy 6: Look for the actual decryption happening in network/memory
    console.log('\nStrategy 6: Check for decrypted data in DOM text...\n');
    
    const allVisibleNames = await evaluate(`
(function() {
    var results = [];
    
    // Get all visible text that looks like Chinese names
    var walker = document.createTreeWalker(
        document.body,
        NodeFilter.SHOW_TEXT,
        null,
        false
    );
    
    var chineseNamePattern = /^[\\u4e00-\\u9fa5]{2,8}$/;
    var seen = new Set();
    
    while (walker.nextNode()) {
        var text = walker.currentNode.textContent.trim();
        if (text.length >= 2 && text.length <= 10 && chineseNamePattern.test(text)) {
            if (!seen.has(text)) {
                seen.add(text);
                var parent = walker.currentNode.parentElement;
                if (parent) {
                    results.push({
                        name: text,
                        element: parent.tagName,
                        class: parent.className?.slice(0, 50)
                    });
                }
            }
        }
    }
    
    return JSON.stringify(results.slice(0, 30), null, 2);
})();
    `);
    console.log('All visible Chinese names:', allVisibleNames);
    
    // Strategy 7: Find account-to-name mapping in the chat panel
    console.log('\nStrategy 7: Extract from chat messages panel...\n');
    
    const chatPanelNames = await evaluate(`
(function() {
    var results = [];
    
    // Find message items
    var messages = document.querySelectorAll('[class*="msg-item"], [class*="message"], [class*="chat-item"]');
    
    messages.forEach(function(msg, i) {
        if (i > 20) return;
        
        // Find sender info
        var avatar = msg.querySelector('[class*="avatar"]');
        var nameEl = msg.querySelector('[class*="nick"], [class*="name"], [class*="sender"]');
        
        var dataAccount = msg.getAttribute('data-account') || 
                          msg.getAttribute('data-from') ||
                          avatar?.getAttribute('data-uid');
        
        if (nameEl || dataAccount) {
            results.push({
                account: dataAccount,
                name: nameEl?.textContent.trim(),
                msgPreview: msg.textContent.slice(0, 30)
            });
        }
    });
    
    return JSON.stringify(results, null, 2);
})();
    `);
    console.log('Chat panel names:', chatPanelNames);
    
    // Strategy 8: Try to find where decryption happens by checking for AES operations
    console.log('\nStrategy 8: Look for encryption library usage...\n');
    
    const cryptoAnalysis = await evaluate(`
(function() {
    var results = {};
    
    // Check if there's any crypto module in webpack modules
    if (typeof webpackJsonp !== 'undefined' || typeof __webpack_require__ !== 'undefined') {
        results.webpack = 'webpack detected';
        
        // Try to find crypto modules
        try {
            var modules = window.webpackJsonp || window.__webpack_modules__;
            if (modules) {
                var modKeys = typeof modules === 'object' ? Object.keys(modules).slice(0, 10) : [];
                results.modulesSample = modKeys;
            }
        } catch (e) {}
    }
    
    // Check for common crypto libraries
    if (window.CryptoJS) {
        results.cryptoJS = {
            available: true,
            methods: Object.keys(window.CryptoJS).slice(0, 20)
        };
    }
    
    // Check for node crypto (in Electron)
    try {
        if (typeof require === 'function') {
            var crypto = require('crypto');
            if (crypto) {
                results.nodeCrypto = 'available';
            }
        }
    } catch (e) {
        results.nodeCryptoError = e.message;
    }
    
    // Check for aes-js or similar
    if (window.aesjs) results.aesjs = 'found';
    if (window.forge) results.forge = 'found';
    
    return JSON.stringify(results, null, 2);
})();
    `);
    console.log('Crypto analysis:', cryptoAnalysis);
    
    ws.close();
    console.log('\nDone!');
}

run().catch(console.error);

