// Try to get account-nickname mapping from member list interaction
const WebSocket = require('ws');
const http = require('http');

async function explore() {
    const pagesJson = await new Promise((resolve, reject) => {
        http.get('http://127.0.0.1:9222/json', (res) => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', () => resolve(JSON.parse(data)));
        }).on('error', reject);
    });
    
    const page = pagesJson.find(p => p.title === '旺商聊');
    if (!page) {
        console.log('WangShangLiao not found');
        return;
    }
    
    console.log('Connecting to:', page.webSocketDebuggerUrl);
    const ws = new WebSocket(page.webSocketDebuggerUrl);
    
    await new Promise((resolve, reject) => {
        ws.on('open', resolve);
        ws.on('error', reject);
    });
    
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
    
    // Get group member list with more detail
    console.log('\n=== Get Group Member List with Account IDs ===');
    const memberListScript = `
(async function() {
    var result = { 
        members: [],
        error: null
    };
    
    if (!window.nim) {
        result.error = 'nim not found';
        return JSON.stringify(result);
    }
    
    // Get team ID from URL
    var url = window.location.href;
    var match = url.match(/team-([0-9]+)/);
    if (!match) {
        result.error = 'No team ID';
        return JSON.stringify(result);
    }
    
    var teamId = match[1];
    
    try {
        // Get team members
        var teamData = await new Promise(function(resolve, reject) {
            window.nim.getTeamMembers({
                teamId: teamId,
                done: function(err, obj) {
                    if (err) reject(err);
                    else resolve(obj);
                }
            });
            setTimeout(function() { reject(new Error('timeout')); }, 10000);
        });
        
        var members = teamData.members || teamData || [];
        
        // For each member, record their account and nickInTeam
        result.totalMembers = members.length;
        result.members = members.slice(0, 30).map(function(m) {
            return {
                account: m.account,
                nickInTeam: m.nickInTeam || '',
                type: m.type
            };
        });
        
    } catch(e) {
        result.error = e.message || String(e);
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const memberList = await evaluate(memberListScript);
    console.log('Member List:', memberList);
    
    // Now try to get messages with plaintext nicknames
    console.log('\n=== Get All Messages with Plaintext Nicknames ===');
    const messagesScript = `
(async function() {
    var result = { 
        mapping: {},
        count: 0,
        error: null
    };
    
    if (!window.nim) {
        result.error = 'nim not found';
        return JSON.stringify(result);
    }
    
    var url = window.location.href;
    var match = url.match(/team-([0-9]+)/);
    if (!match) {
        result.error = 'No team ID';
        return JSON.stringify(result);
    }
    
    var teamId = match[1];
    var sessionId = 'team-' + teamId;
    
    try {
        // Get many messages to find more plaintext nicknames
        var msgs = await new Promise(function(resolve, reject) {
            window.nim.getLocalMsgs({
                sessionId: sessionId,
                limit: 500,
                done: function(err, obj) {
                    if (err) reject(err);
                    else resolve(obj);
                }
            });
            setTimeout(function() { reject(new Error('timeout')); }, 15000);
        });
        
        var msgList = msgs.msgs || msgs || [];
        result.totalMsgs = msgList.length;
        
        // Filter messages with plaintext (non-MD5) nicknames
        msgList.forEach(function(m) {
            if (m.from && m.fromNick) {
                var nick = m.fromNick;
                // Check if it's NOT an MD5 hash (32 hex chars)
                var isMd5 = /^[a-f0-9]{32}$/i.test(nick);
                if (!isMd5 && nick.length > 0 && nick.length < 30) {
                    result.mapping[m.from] = nick;
                }
            }
        });
        
        result.count = Object.keys(result.mapping).length;
        
    } catch(e) {
        result.error = e.message || String(e);
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const messages = await evaluate(messagesScript);
    console.log('Messages Mapping:', messages);
    
    // Try to find decryption key or function
    console.log('\n=== Search for Decryption Logic ===');
    const decryptSearchScript = `
(function() {
    var result = { found: [], checked: [] };
    
    // Search for crypto or decode functions in window
    var searchInObj = function(obj, path, depth) {
        if (depth > 3 || !obj) return;
        
        var keys = [];
        try { keys = Object.keys(obj); } catch(e) { return; }
        
        keys.forEach(function(key) {
            var keyLower = key.toLowerCase();
            try {
                var val = obj[key];
                if (typeof val === 'function') {
                    if (keyLower.indexOf('decrypt') !== -1 ||
                        keyLower.indexOf('aes') !== -1 ||
                        keyLower.indexOf('cipher') !== -1 ||
                        keyLower.indexOf('nick') !== -1 ||
                        keyLower.indexOf('decode') !== -1) {
                        result.found.push({
                            path: path + '.' + key,
                            type: 'function'
                        });
                    }
                }
                if (typeof val === 'object' && val !== null && depth < 3) {
                    searchInObj(val, path + '.' + key, depth + 1);
                }
            } catch(e) {}
        });
    };
    
    // Search in common locations
    searchInObj(window, 'window', 0);
    
    // Check if there's a utils or crypto module
    if (typeof require === 'function') {
        try {
            var crypto = require('crypto');
            result.checked.push('Node crypto available');
        } catch(e) {}
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const decryptSearch = await evaluate(decryptSearchScript);
    console.log('Decrypt Search:', decryptSearch);
    
    // Try to check current displayed members in DOM
    console.log('\n=== Check DOM Display ===');
    const domCheckScript = `
(function() {
    var result = { 
        displayedNames: [],
        structure: null
    };
    
    // Find member list container
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
        result.error = 'No member header found';
        return JSON.stringify(result);
    }
    
    // Go up to find the member list container
    var container = memberHeader.parentElement;
    for (var k = 0; k < 6 && container; k++) {
        container = container.parentElement;
    }
    
    if (container) {
        result.containerClass = container.className;
        
        // Find individual member items
        var items = container.querySelectorAll('[class*="item"], [class*="member"], div > div > div');
        result.itemCount = items.length;
        
        // Extract displayed names
        var names = [];
        container.querySelectorAll('span, div').forEach(function(el) {
            var text = el.textContent.trim();
            if (text.length > 0 && text.length < 20 && 
                !text.match(/^[0-9\\/\\(\\)\\s]+$/) &&
                text !== '群成员' && text !== '搜索' &&
                text.indexOf('/') === -1 &&
                text.indexOf('（') === -1) {
                names.push(text);
            }
        });
        
        result.displayedNames = [...new Set(names)].slice(0, 50);
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const domCheck = await evaluate(domCheckScript);
    console.log('DOM Check:', domCheck);
    
    ws.close();
    console.log('\nExploration complete!');
}

explore().catch(console.error);

