// Map DOM nicknames to account IDs
const WebSocket = require('ws');

const CDP_URL = 'ws://127.0.0.1:9222/devtools/page/8322EEB7A02952E8C4C59B59B616C299';

async function explore() {
    console.log('Connecting to CDP...');
    const ws = new WebSocket(CDP_URL);
    
    await new Promise((resolve, reject) => {
        ws.on('open', resolve);
        ws.on('error', reject);
    });
    
    console.log('Connected!');
    let msgId = 1;
    
    function send(method, params = {}) {
        return new Promise((resolve) => {
            const id = msgId++;
            const handler = (data) => {
                const msg = JSON.parse(data);
                if (msg.id === id) {
                    ws.off('message', handler);
                    resolve(msg.result);
                }
            };
            ws.on('message', handler);
            ws.send(JSON.stringify({ id, method, params }));
        });
    }
    
    async function evaluate(expression) {
        const result = await send('Runtime.evaluate', {
            expression,
            returnByValue: true,
            awaitPromise: true
        });
        return result?.result?.value;
    }
    
    // Find member list items with data attributes or Vue bindings
    console.log('\n=== Finding Member List Items with Data ===');
    const memberListScript = `
(function() {
    var result = { items: [] };
    
    // Find the container with member names
    var memberHeader = null;
    var allElements = document.querySelectorAll('*');
    
    for (var i = 0; i < allElements.length; i++) {
        var el = allElements[i];
        try {
            var directText = '';
            for (var j = 0; j < el.childNodes.length; j++) {
                if (el.childNodes[j].nodeType === 3) {
                    directText += el.childNodes[j].textContent;
                }
            }
            if (directText.indexOf('群成员') !== -1) {
                memberHeader = el;
                break;
            }
        } catch(e) {}
    }
    
    if (!memberHeader) {
        return JSON.stringify({error: 'No header'});
    }
    
    // Go up to find container, then find member items
    var container = memberHeader.parentElement;
    for (var k = 0; k < 5 && container; k++) {
        container = container.parentElement;
    }
    
    if (!container) {
        return JSON.stringify({error: 'No container'});
    }
    
    // Find clickable member items (likely have account data)
    var clickableItems = container.querySelectorAll('[class*="item"], [class*="member"], div > div');
    
    clickableItems.forEach(function(item) {
        // Check if this item has Vue data
        var vueData = null;
        var itemEl = item;
        
        // Walk up to find Vue component
        while (itemEl && !vueData) {
            if (itemEl.__vue__) {
                var v = itemEl.__vue__;
                if (v.member || v.item || v.user || v.info) {
                    var data = v.member || v.item || v.user || v.info;
                    vueData = {
                        account: data.account,
                        nick: data.nick,
                        nickInTeam: data.nickInTeam,
                        displayName: data.displayName
                    };
                }
                if (v.$props && (v.$props.member || v.$props.item)) {
                    var propData = v.$props.member || v.$props.item;
                    vueData = {
                        account: propData.account,
                        nick: propData.nick,
                        nickInTeam: propData.nickInTeam
                    };
                }
            }
            itemEl = itemEl.parentElement;
        }
        
        // Get displayed text
        var displayText = '';
        var spans = item.querySelectorAll('span');
        spans.forEach(function(s) {
            var t = s.textContent.trim();
            if (t && t.length < 15 && !t.match(/^[0-9]+$/)) {
                displayText = t;
            }
        });
        
        if (!displayText) {
            displayText = item.textContent.trim().substring(0, 20);
        }
        
        if (displayText || vueData) {
            result.items.push({
                displayText: displayText,
                vueData: vueData,
                className: item.className.substring(0, 50),
                hasVue: !!item.__vue__
            });
        }
    });
    
    // Dedupe by displayText
    var seen = new Set();
    result.items = result.items.filter(function(i) {
        if (seen.has(i.displayText)) return false;
        seen.add(i.displayText);
        return true;
    }).slice(0, 30);
    
    return JSON.stringify(result, null, 2);
})()`;

    const memberList = await evaluate(memberListScript);
    console.log('Member List Items:', memberList);
    
    // Try to find where the decrypted names come from
    console.log('\n=== Check Vue Store State for Decrypted Names ===');
    const vueStoreScript = `
(function() {
    var result = { state: null, memberData: [] };
    
    var app = document.querySelector('#app');
    if (!app || !app.__vue__) return JSON.stringify({error: 'No Vue'});
    
    var store = app.__vue__.$store;
    if (!store || !store.state) return JSON.stringify({error: 'No store'});
    
    result.stateKeys = Object.keys(store.state);
    
    // Look for member-related state
    function findMembers(obj, path, depth) {
        if (depth > 4 || !obj) return;
        
        if (Array.isArray(obj) && obj.length > 0) {
            var first = obj[0];
            if (first && first.account) {
                // Found member array
                result.memberData.push({
                    path: path,
                    count: obj.length,
                    sample: obj.slice(0, 3).map(function(m) {
                        return {
                            account: m.account,
                            nick: m.nick,
                            nickInTeam: m.nickInTeam,
                            displayName: m.displayName,
                            customNick: m.customNick,
                            alias: m.alias
                        };
                    })
                });
            }
        }
        
        if (typeof obj === 'object' && !Array.isArray(obj)) {
            var keys = Object.keys(obj);
            for (var i = 0; i < keys.length; i++) {
                var key = keys[i];
                if (typeof obj[key] === 'object') {
                    findMembers(obj[key], path + '.' + key, depth + 1);
                }
            }
        }
    }
    
    findMembers(store.state, 'state', 0);
    
    return JSON.stringify(result, null, 2);
})()`;

    const vueStore = await evaluate(vueStoreScript);
    console.log('Vue Store:', vueStore);
    
    // Check for getLocalTeamMembers which might have decrypted data
    console.log('\n=== Check NIM Local Team Members ===');
    const localMembersScript = `
(async function() {
    var result = { members: [], error: null };
    
    if (!window.nim) return JSON.stringify({error: 'No nim'});
    
    var teamId = '40821608989';
    
    // Try getLocalTeamMembers
    if (typeof window.nim.getLocalTeamMembers === 'function') {
        try {
            var data = window.nim.getLocalTeamMembers({
                teamId: teamId
            });
            
            result.method = 'getLocalTeamMembers';
            result.members = (data || []).slice(0, 10).map(function(m) {
                return {
                    account: m.account,
                    nick: m.nick,
                    nickInTeam: m.nickInTeam,
                    alias: m.alias,
                    displayName: m.displayName,
                    customNick: m.customNick,
                    allKeys: Object.keys(m).join(',')
                };
            });
        } catch(e) {
            result.error = e.message;
        }
    } else {
        result.error = 'getLocalTeamMembers not available';
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const localMembers = await evaluate(localMembersScript);
    console.log('Local Members:', localMembers);
    
    // Check messages for sender names - they appear decrypted in chat
    console.log('\n=== Check Recent Messages for Sender Names ===');
    const messagesScript = `
(async function() {
    var result = { messages: [], error: null };
    
    if (!window.nim) return JSON.stringify({error: 'No nim'});
    
    var teamId = '40821608989';
    
    // Try getLocalMsgs
    if (typeof window.nim.getLocalMsgs === 'function') {
        try {
            var data = await new Promise(function(resolve, reject) {
                window.nim.getLocalMsgs({
                    sessionId: 'team-' + teamId,
                    limit: 10,
                    done: function(err, obj) {
                        if (err) reject(err);
                        else resolve(obj);
                    }
                });
                setTimeout(reject, 5000);
            });
            
            var msgs = data.msgs || data || [];
            result.messages = msgs.map(function(m) {
                return {
                    from: m.from,
                    fromNick: m.fromNick,
                    text: (m.text || '').substring(0, 30),
                    user: m.user ? {
                        nick: m.user.nick,
                        displayName: m.user.displayName,
                        groupMemberNick: m.user.groupMemberNick
                    } : null
                };
            });
        } catch(e) {
            result.error = e.message;
        }
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const messages = await evaluate(messagesScript);
    console.log('Messages:', messages);
    
    // Check if there's a decrypt function in the Electron app
    console.log('\n=== Search for Decrypt in Electron/Node ===');
    const electronScript = `
(function() {
    var result = { 
        hasRequire: typeof require === 'function',
        modules: [],
        cryptoFuncs: []
    };
    
    if (typeof require === 'function') {
        try {
            // Try to get crypto module
            var crypto = require('crypto');
            result.hasCrypto = true;
            result.cryptoMethods = Object.keys(crypto).filter(function(k) {
                return k.toLowerCase().indexOf('decrypt') !== -1 ||
                       k.toLowerCase().indexOf('cipher') !== -1;
            });
        } catch(e) {
            result.cryptoError = e.message;
        }
    }
    
    // Search for app-specific decrypt function
    for (var key in window) {
        if (typeof window[key] === 'object' && window[key]) {
            try {
                var objKeys = Object.keys(window[key]);
                objKeys.forEach(function(k) {
                    if (k.toLowerCase().indexOf('decrypt') !== -1 ||
                        k.toLowerCase().indexOf('cipher') !== -1) {
                        result.cryptoFuncs.push(key + '.' + k);
                    }
                });
            } catch(e) {}
        }
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const electron = await evaluate(electronScript);
    console.log('Electron/Node:', electron);
    
    ws.close();
    console.log('\nExploration complete!');
}

explore().catch(console.error);

