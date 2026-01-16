// Explore how to get real nicknames from WangShangLiao
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
    
    // Method 1: Try getUsers with known account IDs
    console.log('\n=== Method 1: getUsers with accounts ===');
    const getUsersScript = `
(async function() {
    var result = { users: [], error: null };
    
    if (!window.nim || typeof window.nim.getUsers !== 'function') {
        result.error = 'nim.getUsers not available';
        return JSON.stringify(result);
    }
    
    // Known account IDs from screenshot
    var accounts = ['1391351554', '1569397794', '1675695400', '1872087808'];
    
    try {
        var users = await new Promise(function(resolve, reject) {
            window.nim.getUsers({
                accounts: accounts,
                done: function(err, data) {
                    if (err) reject(err);
                    else resolve(data);
                }
            });
            setTimeout(function() { reject(new Error('timeout')); }, 10000);
        });
        
        result.users = (users || []).map(function(u) {
            return {
                account: u.account,
                nick: u.nick,
                avatar: u.avatar ? 'has avatar' : 'no avatar',
                allKeys: Object.keys(u).join(',')
            };
        });
    } catch(e) {
        result.error = e.message || String(e);
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const users = await evaluate(getUsersScript);
    console.log('getUsers Result:', users);
    
    // Method 2: Check Vue data store for team members
    console.log('\n=== Method 2: Deep Vue Store Search ===');
    const vueDeepScript = `
(function() {
    var result = { found: [], paths: [] };
    var visited = new WeakSet();
    
    function search(obj, path, depth) {
        if (depth > 8 || !obj || typeof obj !== 'object') return;
        if (visited.has(obj)) return;
        visited.add(obj);
        
        try {
            // Check if this looks like member data
            if (obj.account && (obj.nick || obj.nickInTeam || obj.displayName)) {
                result.found.push({
                    path: path,
                    account: obj.account,
                    nick: obj.nick,
                    nickInTeam: obj.nickInTeam,
                    displayName: obj.displayName,
                    alias: obj.alias
                });
                return;
            }
            
            // Check arrays
            if (Array.isArray(obj) && obj.length > 0 && obj.length < 1000) {
                for (var i = 0; i < Math.min(obj.length, 20); i++) {
                    search(obj[i], path + '[' + i + ']', depth + 1);
                }
            } else {
                // Check object properties
                var keys = Object.keys(obj);
                for (var j = 0; j < keys.length; j++) {
                    var key = keys[j];
                    if (key.startsWith('_') || key.startsWith('$')) continue;
                    if (['el', 'options', 'parent', '__vue__'].indexOf(key) !== -1) continue;
                    try {
                        var val = obj[key];
                        if (val && typeof val === 'object') {
                            search(val, path + '.' + key, depth + 1);
                        }
                    } catch(e) {}
                }
            }
        } catch(e) {}
    }
    
    // Search from Vue app
    var app = document.querySelector('#app');
    if (app && app.__vue__) {
        var store = app.__vue__.$store;
        if (store && store.state) {
            result.paths.push('Found $store.state');
            search(store.state, 'state', 0);
        }
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const vueDeep = await evaluate(vueDeepScript);
    console.log('Vue Deep Search:', vueDeep);
    
    // Method 3: Get team ID from current session and get members
    console.log('\n=== Method 3: Get Team Members from Current Session ===');
    const sessionMembersScript = `
(async function() {
    var result = { 
        teamId: null, 
        members: [],
        error: null 
    };
    
    // Get team ID from URL
    var url = window.location.href;
    var match = url.match(/team-([0-9]+)/);
    if (match) {
        result.teamId = match[1];
    }
    
    if (!result.teamId) {
        result.error = 'No team ID in URL';
        return JSON.stringify(result);
    }
    
    if (!window.nim) {
        result.error = 'nim not found';
        return JSON.stringify(result);
    }
    
    try {
        // Get team members
        var data = await new Promise(function(resolve, reject) {
            window.nim.getTeamMembers({
                teamId: result.teamId,
                done: function(err, obj) {
                    if (err) reject(err);
                    else resolve(obj);
                }
            });
            setTimeout(function() { reject(new Error('timeout')); }, 15000);
        });
        
        var members = data.members || data || [];
        var accounts = members.slice(0, 10).map(function(m) { return m.account; });
        
        result.memberCount = members.length;
        
        // Now get user profiles for these accounts
        if (typeof window.nim.getUsers === 'function') {
            var users = await new Promise(function(resolve, reject) {
                window.nim.getUsers({
                    accounts: accounts,
                    done: function(err, data) {
                        if (err) reject(err);
                        else resolve(data);
                    }
                });
                setTimeout(function() { reject(new Error('timeout')); }, 10000);
            });
            
            // Create a map of account -> user info
            var userMap = {};
            (users || []).forEach(function(u) {
                userMap[u.account] = u;
            });
            
            result.members = members.slice(0, 10).map(function(m) {
                var user = userMap[m.account] || {};
                return {
                    account: m.account,
                    nickInTeam: m.nickInTeam,
                    userNick: user.nick,
                    type: m.type,
                    custom: m.custom ? m.custom.substring(0, 100) : ''
                };
            });
        }
    } catch(e) {
        result.error = e.message || String(e);
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const sessionMembers = await evaluate(sessionMembersScript);
    console.log('Session Members:', sessionMembers);
    
    // Method 4: Check if there's a local user cache/store
    console.log('\n=== Method 4: Check Local Storage & IndexedDB ===');
    const localDataScript = `
(function() {
    var result = { 
        localStorage: [],
        sessionKeys: [],
        userRelated: []
    };
    
    // Check localStorage keys
    for (var i = 0; i < localStorage.length; i++) {
        var key = localStorage.key(i);
        if (key.toLowerCase().indexOf('user') !== -1 || 
            key.toLowerCase().indexOf('nick') !== -1 ||
            key.toLowerCase().indexOf('member') !== -1) {
            try {
                var val = localStorage.getItem(key);
                result.userRelated.push({
                    key: key,
                    valuePreview: val ? val.substring(0, 200) : ''
                });
            } catch(e) {}
        }
        result.localStorage.push(key);
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const localData = await evaluate(localDataScript);
    console.log('Local Data:', localData);
    
    // Method 5: Parse Custom field for nickname
    console.log('\n=== Method 5: Parse Custom Field for Encrypted Nickname ===');
    const customParseScript = `
(async function() {
    var result = { members: [], error: null };
    
    var url = window.location.href;
    var match = url.match(/team-([0-9]+)/);
    if (!match || !window.nim) {
        result.error = 'No team or nim';
        return JSON.stringify(result);
    }
    
    var teamId = match[1];
    
    try {
        var data = await new Promise(function(resolve, reject) {
            window.nim.getTeamMembers({
                teamId: teamId,
                done: function(err, obj) {
                    if (err) reject(err);
                    else resolve(obj);
                }
            });
            setTimeout(reject, 10000);
        });
        
        var members = data.members || data || [];
        
        result.members = members.slice(0, 5).map(function(m) {
            var customObj = null;
            try {
                if (m.custom) {
                    customObj = JSON.parse(m.custom);
                }
            } catch(e) {}
            
            return {
                account: m.account,
                nickInTeam: m.nickInTeam,
                customParsed: customObj,
                customKeys: customObj ? Object.keys(customObj) : []
            };
        });
    } catch(e) {
        result.error = e.message || String(e);
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const customParse = await evaluate(customParseScript);
    console.log('Custom Parse:', customParse);
    
    ws.close();
    console.log('\nExploration complete!');
}

explore().catch(console.error);

