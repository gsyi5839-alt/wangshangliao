// Extract account-to-nickname mapping from WangShangLiao DOM
const WebSocket = require('ws');

async function run() {
    console.log('=== Get Account-to-Nickname Mapping ===\n');
    
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
    
    console.log(`Connected to: ${pageTarget.url}\n`);
    
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
    
    // Get member list with mapping to account
    console.log('Method 1: Get member panel mapping...\n');
    
    const memberPanelMapping = await evaluate(`
(function() {
    var results = [];
    
    // Find member list in the right panel
    // Based on DOM structure: members are shown with class "inline-block truncate color-#171717"
    
    // First, find the member panel container
    var memberContainer = document.querySelector('[class*="member"], .team-member-panel');
    if (!memberContainer) {
        // Try to find by text "群成员"
        var headers = document.querySelectorAll('span');
        for (var header of headers) {
            if (header.textContent.includes('群成员')) {
                memberContainer = header.closest('.panel, .member-panel, [class*="member"]')?.parentElement;
                break;
            }
        }
    }
    
    // Get all member items
    var memberItems = document.querySelectorAll('.member-item, [class*="member-item"]');
    
    memberItems.forEach(function(item) {
        var nameEl = item.querySelector('p.truncate, .name, [class*="nick"]');
        var name = nameEl ? nameEl.textContent.trim() : '';
        
        // Try to get account from various attributes
        var account = item.getAttribute('data-account') || 
                     item.getAttribute('data-uid') ||
                     item.getAttribute('data-id');
        
        // Also check onclick or data attributes
        var onclick = item.getAttribute('@click') || item.getAttribute('onclick') || '';
        var accountMatch = onclick.match(/(\\d{8,})/);
        if (!account && accountMatch) {
            account = accountMatch[1];
        }
        
        if (name) {
            results.push({ name, account: account || 'unknown' });
        }
    });
    
    return JSON.stringify(results, null, 2);
})();
    `);
    console.log('Member panel mapping:', memberPanelMapping);
    
    // Method 2: Click on members to get their account info
    console.log('\nMethod 2: Analyze Vue component bindings...\n');
    
    const vueBindings = await evaluate(`
(function() {
    var results = [];
    
    // Find all P elements with names
    var nameElements = document.querySelectorAll('p.inline-block.truncate');
    
    nameElements.forEach(function(el, i) {
        if (i > 20) return;
        
        var name = el.textContent.trim();
        if (!name || name.length > 15) return;
        
        // Check Vue bindings
        var vnode = el.__vue__ || el._vnode || el.__vnode;
        var parent = el.parentElement;
        
        // Look for account in parent's data
        var parentData = null;
        var p = el;
        for (var depth = 0; depth < 5; depth++) {
            p = p.parentElement;
            if (!p) break;
            
            // Check for Vue instance with member data
            if (p.__vue__) {
                var vm = p.__vue__;
                if (vm.member) {
                    parentData = { account: vm.member.account, nick: vm.member.nick || vm.member.nickInTeam };
                    break;
                }
                if (vm.item) {
                    parentData = { account: vm.item.account, nick: vm.item.nick };
                    break;
                }
            }
            
            // Check data attributes
            var dataId = p.getAttribute('data-id') || p.getAttribute('data-account');
            if (dataId && /^\\d{8,}$/.test(dataId)) {
                parentData = { account: dataId };
                break;
            }
        }
        
        results.push({
            name: name,
            parentData: parentData,
            elementClass: el.className
        });
    });
    
    return JSON.stringify(results, null, 2);
})();
    `);
    console.log('Vue bindings:', vueBindings);
    
    // Method 3: Get from NIM SDK with message correlation
    console.log('\nMethod 3: Correlate with message senders...\n');
    
    const messageCorrelation = await evaluate(`
(async function() {
    if (!window.nim) return 'nim not found';
    
    var results = [];
    var teamId = '40821608989'; // Current team
    var sessionId = 'team-' + teamId;
    
    try {
        // Get recent messages
        var msgs = await new Promise((resolve, reject) => {
            window.nim.getLocalMsgs({
                sessionId: sessionId,
                limit: 200,
                done: function(err, obj) {
                    if (err) reject(err);
                    else resolve(obj);
                }
            });
            setTimeout(() => reject('timeout'), 10000);
        });
        
        var msgList = msgs.msgs || msgs || [];
        var accountToNick = {};
        
        msgList.forEach(function(m) {
            if (m.from && m.fromNick) {
                // Check if fromNick is not MD5 hash
                var isMd5 = /^[a-f0-9]{32}$/i.test(m.fromNick);
                if (!isMd5 && m.fromNick.length < 20) {
                    accountToNick[m.from] = m.fromNick;
                }
            }
        });
        
        results = Object.entries(accountToNick).map(function([account, nick]) {
            return { account, nick };
        });
        
    } catch (e) {
        return 'Error: ' + e.toString();
    }
    
    return JSON.stringify(results, null, 2);
})();
    `, true);
    console.log('Message correlation:', messageCorrelation);
    
    // Method 4: Get all team members with their displayed names via DOM walk
    console.log('\nMethod 4: Deep DOM analysis with parent-child correlation...\n');
    
    const deepDomAnalysis = await evaluate(`
(function() {
    var results = [];
    
    // Find the member grid/list container
    // Looking at the structure, members are in a grid with avatars and names
    
    // Method: Find avatar images and their adjacent name elements
    var avatars = document.querySelectorAll('img[class*="avatar"], .avatar img');
    
    avatars.forEach(function(avatar, i) {
        if (i > 30) return;
        
        var src = avatar.src || '';
        // Extract account from avatar URL if present
        var accountMatch = src.match(/(\\d{8,})/);
        var account = accountMatch ? accountMatch[1] : null;
        
        // Find the name element near this avatar
        var container = avatar.closest('.member-item, [class*="member"], .user-item, .grid-item, .flex');
        if (container) {
            var nameEl = container.querySelector('p.truncate, .name, [class*="nick"]');
            var name = nameEl ? nameEl.textContent.trim() : '';
            
            // Also check container for data attributes
            if (!account) {
                account = container.getAttribute('data-account') || 
                         container.getAttribute('data-id') || 
                         container.getAttribute('data-uid');
            }
            
            if (name && name.length < 15) {
                results.push({
                    name: name,
                    account: account || 'unknown',
                    avatarSrc: src.slice(0, 100)
                });
            }
        }
    });
    
    return JSON.stringify(results, null, 2);
})();
    `);
    console.log('Deep DOM analysis:', deepDomAnalysis);
    
    // Method 5: Check global data store (Pinia/reactive data)
    console.log('\nMethod 5: Access reactive state...\n');
    
    const reactiveState = await evaluate(`
(function() {
    var results = [];
    
    // Try to find the Vue 3 app instance
    var app = document.querySelector('#app')?.__vue_app__;
    if (!app) return 'Vue app not found';
    
    // Try to access Pinia store
    var pinia = app._context?.provides?.pinia;
    if (pinia) {
        // Get all stores
        var stores = pinia._s;
        if (stores) {
            stores.forEach(function(store, key) {
                if (key.includes('team') || key.includes('member') || key.includes('chat')) {
                    var state = store.$state || store;
                    results.push({
                        storeName: key,
                        stateKeys: Object.keys(state).slice(0, 10)
                    });
                    
                    // Check for members array
                    if (state.members && Array.isArray(state.members)) {
                        state.members.slice(0, 10).forEach(function(m) {
                            results.push({
                                from: key,
                                account: m.account,
                                nick: m.nick || m.nickInTeam || m.displayName
                            });
                        });
                    }
                }
            });
        }
    }
    
    return JSON.stringify(results, null, 2);
})();
    `);
    console.log('Reactive state:', reactiveState);
    
    // Method 6: Use NIM's getUsers with all accounts from team
    console.log('\nMethod 6: Use NIM getUsers API...\n');
    
    const nimGetUsers = await evaluate(`
(async function() {
    if (!window.nim) return 'nim not found';
    
    var teamId = '40821608989';
    var results = [];
    
    try {
        // First get all team members accounts
        var teamData = await new Promise((resolve, reject) => {
            window.nim.getTeamMembers({
                teamId: teamId,
                done: function(err, obj) {
                    if (err) reject(err);
                    else resolve(obj);
                }
            });
            setTimeout(() => reject('timeout'), 5000);
        });
        
        var accounts = (teamData.members || []).map(m => m.account);
        results.push({ totalAccounts: accounts.length });
        
        // Get user info for all accounts (batch of 50)
        for (var i = 0; i < accounts.length; i += 50) {
            var batch = accounts.slice(i, i + 50);
            
            try {
                var users = await new Promise((resolve, reject) => {
                    window.nim.getUsers({
                        accounts: batch,
                        done: function(err, data) {
                            if (err) reject(err);
                            else resolve(data);
                        }
                    });
                    setTimeout(() => reject('timeout'), 5000);
                });
                
                (users || []).forEach(function(u) {
                    // Check if nick is plaintext (not MD5)
                    var nick = u.nick || '';
                    var isMd5 = /^[a-f0-9]{32}$/i.test(nick);
                    
                    results.push({
                        account: u.account,
                        nick: nick,
                        isMd5: isMd5,
                        avatar: u.avatar ? u.avatar.slice(0, 50) : null
                    });
                });
            } catch (e) {
                results.push({ batchError: e.toString() });
            }
        }
        
    } catch (e) {
        return 'Error: ' + e.toString();
    }
    
    return JSON.stringify(results, null, 2);
})();
    `, true);
    console.log('NIM getUsers:', nimGetUsers);
    
    // Method 7: Try to find where decryption happens
    console.log('\nMethod 7: Find decryption in Vue computed/methods...\n');
    
    const vueComputed = await evaluate(`
(function() {
    var results = [];
    
    // Walk through all Vue components looking for decrypt methods
    function walkComponents(el, depth) {
        if (depth > 5) return;
        
        var vm = el.__vue__;
        if (vm) {
            var options = vm.$options || {};
            var methods = Object.keys(options.methods || {});
            var computed = Object.keys(options.computed || {});
            
            var decryptMethods = [...methods, ...computed].filter(function(name) {
                return name.toLowerCase().includes('decrypt') ||
                       name.toLowerCase().includes('nick') ||
                       name.toLowerCase().includes('name');
            });
            
            if (decryptMethods.length > 0) {
                results.push({
                    component: options.name || el.tagName,
                    decryptMethods: decryptMethods
                });
            }
        }
        
        Array.from(el.children || []).forEach(function(child) {
            walkComponents(child, depth + 1);
        });
    }
    
    walkComponents(document.body, 0);
    
    return JSON.stringify(results, null, 2);
})();
    `);
    console.log('Vue computed/methods:', vueComputed);
    
    ws.close();
    console.log('\n=== Done ===');
}

run().catch(console.error);

