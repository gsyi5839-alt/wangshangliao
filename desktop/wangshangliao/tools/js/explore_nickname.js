// Explore how WangShangLiao decrypts nickname
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
    
    // Explore Vue store data for team members with decrypted nicknames
    console.log('\n=== Exploring Vue Store Team Members ===');
    const vueMembersScript = `
(function() {
    var result = {
        found: false,
        members: [],
        stores: []
    };
    
    // Try to find Vue app
    var app = document.querySelector('#app');
    if (app && app.__vue__) {
        var vue = app.__vue__;
        result.stores.push('Found Vue instance');
        
        // Look for team member data in Vue
        function searchForMembers(obj, path, depth) {
            if (depth > 5 || !obj) return;
            
            for (var key in obj) {
                try {
                    var val = obj[key];
                    if (val && typeof val === 'object') {
                        // Look for members array
                        if (Array.isArray(val) && val.length > 0 && val[0] && val[0].account) {
                            result.found = true;
                            result.members = val.slice(0, 5).map(function(m) {
                                return {
                                    account: m.account,
                                    nick: m.nick,
                                    nickInTeam: m.nickInTeam,
                                    alias: m.alias,
                                    name: m.name,
                                    displayName: m.displayName,
                                    customName: m.customName,
                                    allKeys: Object.keys(m).join(',')
                                };
                            });
                        }
                        if (depth < 4) {
                            searchForMembers(val, path + '.' + key, depth + 1);
                        }
                    }
                } catch(e) {}
            }
        }
        
        searchForMembers(vue.$store?.state || vue.$data, 'root', 0);
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const vueMembers = await evaluate(vueMembersScript);
    console.log('Vue Members:', vueMembers);
    
    // Explore NIM team members data directly
    console.log('\n=== Exploring NIM Team Members Data ===');
    const nimMembersScript = `
(async function() {
    var result = {
        method: '',
        members: [],
        error: null
    };
    
    if (!window.nim) {
        result.error = 'nim not found';
        return JSON.stringify(result);
    }
    
    // Get first available team
    var teams = [];
    if (typeof window.nim.getLocalTeams === 'function') {
        teams = window.nim.getLocalTeams() || [];
    }
    
    if (teams.length === 0) {
        result.error = 'No teams found';
        return JSON.stringify(result);
    }
    
    var teamId = teams[0].teamId || teams[0];
    result.teamId = teamId;
    
    // Try getTeamMembers
    try {
        var data = await new Promise(function(resolve, reject) {
            window.nim.getTeamMembers({
                teamId: teamId,
                done: function(err, obj) {
                    if (err) reject(err);
                    else resolve(obj);
                }
            });
            setTimeout(function() { reject('timeout'); }, 10000);
        });
        
        result.method = 'getTeamMembers';
        var members = data.members || data || [];
        
        // Get first 5 members with ALL their fields
        result.members = members.slice(0, 5).map(function(m) {
            var obj = {
                account: m.account,
                nickInTeam: m.nickInTeam,
                nick: m.nick,
                alias: m.alias,
                custom: m.custom,
                type: m.type
            };
            // Get all keys
            obj.allKeys = Object.keys(m).join(',');
            return obj;
        });
        
    } catch(e) {
        result.error = e.message || String(e);
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const nimMembers = await evaluate(nimMembersScript);
    console.log('NIM Members:', nimMembers);
    
    // Try to find decryption function
    console.log('\n=== Searching for Decryption Functions ===');
    const decryptSearch = `
(function() {
    var result = {
        functions: [],
        globalDecrypt: []
    };
    
    // Search for decrypt/decode functions in window
    var searchTerms = ['decrypt', 'decode', 'cipher', 'nick', 'name'];
    
    for (var key in window) {
        try {
            var val = window[key];
            if (typeof val === 'function') {
                var name = key.toLowerCase();
                for (var i = 0; i < searchTerms.length; i++) {
                    if (name.indexOf(searchTerms[i]) !== -1) {
                        result.globalDecrypt.push(key);
                        break;
                    }
                }
            }
        } catch(e) {}
    }
    
    // Check NIM for decrypt functions
    if (window.nim) {
        var nimFuncs = [];
        for (var k in window.nim) {
            if (typeof window.nim[k] === 'function') {
                var fname = k.toLowerCase();
                if (fname.indexOf('decrypt') !== -1 || 
                    fname.indexOf('decode') !== -1 || 
                    fname.indexOf('nick') !== -1 ||
                    fname.indexOf('user') !== -1 ||
                    fname.indexOf('profile') !== -1) {
                    nimFuncs.push(k);
                }
            }
        }
        result.nimFunctions = nimFuncs;
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const decryptResult = await evaluate(decryptSearch);
    console.log('Decrypt Functions:', decryptResult);
    
    // Try getUserProfile for real nicknames
    console.log('\n=== Trying getUser/getUserProfile ===');
    const getUserScript = `
(async function() {
    var result = {
        users: [],
        error: null
    };
    
    if (!window.nim) {
        result.error = 'nim not found';
        return JSON.stringify(result);
    }
    
    // Get first team members
    var teams = window.nim.getLocalTeams ? window.nim.getLocalTeams() : [];
    if (teams.length === 0) {
        result.error = 'No teams';
        return JSON.stringify(result);
    }
    
    var teamId = teams[0].teamId || teams[0];
    
    try {
        var teamData = await new Promise(function(resolve, reject) {
            window.nim.getTeamMembers({
                teamId: teamId,
                done: function(err, obj) {
                    if (err) reject(err);
                    else resolve(obj);
                }
            });
            setTimeout(reject, 5000);
        });
        
        var members = teamData.members || teamData || [];
        var accounts = members.slice(0, 3).map(function(m) { return m.account; });
        
        // Try getUsers
        if (typeof window.nim.getUsers === 'function') {
            var usersData = await new Promise(function(resolve, reject) {
                window.nim.getUsers({
                    accounts: accounts,
                    done: function(err, users) {
                        if (err) reject(err);
                        else resolve(users);
                    }
                });
                setTimeout(reject, 5000);
            });
            
            result.method = 'getUsers';
            result.users = usersData.map(function(u) {
                return {
                    account: u.account,
                    nick: u.nick,
                    avatar: u.avatar ? 'yes' : 'no',
                    allKeys: Object.keys(u).join(',')
                };
            });
        }
    } catch(e) {
        result.error = e.message || String(e);
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const getUserResult = await evaluate(getUserScript);
    console.log('GetUsers Result:', getUserResult);
    
    // Check DOM for displayed nicknames
    console.log('\n=== Checking DOM for Displayed Names ===');
    const domNamesScript = `
(function() {
    var result = {
        memberListItems: []
    };
    
    // Look for member list items in the DOM
    var memberItems = document.querySelectorAll('[class*="member"], [class*="user"], [class*="nick"]');
    
    result.totalFound = memberItems.length;
    
    // Check group member list specifically
    var groupList = document.querySelector('.member-list, .team-member-list, [class*="groupMember"]');
    if (groupList) {
        result.groupListClass = groupList.className;
        var items = groupList.querySelectorAll('*');
        result.groupListChildren = items.length;
    }
    
    // Get text content from visible member names
    memberItems = Array.from(memberItems).slice(0, 10);
    result.memberListItems = memberItems.map(function(el) {
        return {
            tag: el.tagName,
            className: el.className.substring(0, 50),
            text: (el.textContent || '').substring(0, 30).trim()
        };
    });
    
    return JSON.stringify(result, null, 2);
})()`;

    const domNames = await evaluate(domNamesScript);
    console.log('DOM Names:', domNames);
    
    // Check Custom field for nickname ciphertext
    console.log('\n=== Checking Custom Field ===');
    const customFieldScript = `
(async function() {
    var result = {
        customData: []
    };
    
    if (!window.nim) return JSON.stringify({error: 'no nim'});
    
    var teams = window.nim.getLocalTeams ? window.nim.getLocalTeams() : [];
    if (teams.length === 0) return JSON.stringify({error: 'no teams'});
    
    var teamId = teams[0].teamId || teams[0];
    
    try {
        var data = await new Promise(function(resolve, reject) {
            window.nim.getTeamMembers({
                teamId: teamId,
                done: function(err, obj) {
                    if (err) reject(err);
                    else resolve(obj);
                }
            });
            setTimeout(reject, 5000);
        });
        
        var members = data.members || data || [];
        
        result.customData = members.slice(0, 5).map(function(m) {
            var customStr = m.custom || '';
            var customObj = null;
            try {
                customObj = JSON.parse(customStr);
            } catch(e) {}
            
            return {
                account: m.account,
                nickInTeam: m.nickInTeam,
                customRaw: customStr.substring(0, 200),
                customParsed: customObj
            };
        });
        
    } catch(e) {
        result.error = e.message || String(e);
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const customField = await evaluate(customFieldScript);
    console.log('Custom Field Data:', customField);
    
    ws.close();
    console.log('\nExploration complete!');
}

explore().catch(console.error);

