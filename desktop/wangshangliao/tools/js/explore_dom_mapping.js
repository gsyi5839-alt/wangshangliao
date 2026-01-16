// Explore DOM to find account-nickname mapping
const WebSocket = require('ws');
const http = require('http');

async function explore() {
    // Get page ID first
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
    
    // Try to find Vue component data for member list
    console.log('\n=== Searching for Member List Vue Component ===');
    const vueSearchScript = `
(function() {
    var result = { found: false, members: [], componentPath: '' };
    
    // Find element containing member list
    var memberContainer = null;
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
                // Go up to find container with Vue data
                var parent = el;
                for (var k = 0; k < 10 && parent; k++) {
                    if (parent.__vue__) {
                        var v = parent.__vue__;
                        result.componentPath = 'Found Vue at level ' + k;
                        
                        // Search for member data
                        function searchForMembers(obj, path, depth) {
                            if (depth > 5 || !obj) return;
                            
                            if (Array.isArray(obj) && obj.length > 0) {
                                var first = obj[0];
                                if (first && (first.account || first.id || first.accid)) {
                                    result.found = true;
                                    result.arrayPath = path;
                                    result.members = obj.slice(0, 20).map(function(m) {
                                        return {
                                            account: m.account || m.id || m.accid,
                                            nick: m.nick,
                                            nickInTeam: m.nickInTeam,
                                            alias: m.alias,
                                            displayName: m.displayName,
                                            name: m.name,
                                            teamNick: m.teamNick
                                        };
                                    });
                                }
                            }
                            
                            if (typeof obj === 'object' && !Array.isArray(obj)) {
                                for (var key in obj) {
                                    if (key.startsWith('_') || key.startsWith('$')) continue;
                                    try {
                                        searchForMembers(obj[key], path + '.' + key, depth + 1);
                                    } catch(e) {}
                                }
                            }
                        }
                        
                        searchForMembers(v.$data, 'data', 0);
                        searchForMembers(v.$props, 'props', 0);
                        searchForMembers(v, 'component', 0);
                        
                        if (result.found) break;
                    }
                    parent = parent.parentElement;
                }
                if (result.found) break;
            }
        } catch(e) {}
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const vueResult = await evaluate(vueSearchScript);
    console.log('Vue Search Result:', vueResult);
    
    // Try direct DOM scraping with account IDs
    console.log('\n=== Try to Find Account IDs in DOM ===');
    const domScrapeScript = `
(function() {
    var result = { items: [] };
    
    // Find all elements that might contain member info
    var elements = document.querySelectorAll('[data-account], [data-id], [data-user], [data-member]');
    
    elements.forEach(function(el) {
        result.items.push({
            account: el.getAttribute('data-account') || el.getAttribute('data-id') || el.getAttribute('data-user'),
            text: el.textContent.substring(0, 50),
            className: el.className.substring(0, 50)
        });
    });
    
    // Also check for click handlers that might reveal account
    var memberItems = document.querySelectorAll('[class*="member-item"], [class*="memberItem"], [class*="user-item"]');
    result.memberItemCount = memberItems.length;
    
    if (memberItems.length > 0) {
        result.firstMemberItem = {
            className: memberItems[0].className,
            innerHTML: memberItems[0].innerHTML.substring(0, 200),
            attributes: Array.from(memberItems[0].attributes).map(function(a) {
                return a.name + '=' + a.value.substring(0, 50);
            })
        };
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const domScrape = await evaluate(domScrapeScript);
    console.log('DOM Scrape Result:', domScrape);
    
    // Check NIM for stored user data
    console.log('\n=== Check NIM for User Data Cache ===');
    const nimCacheScript = `
(function() {
    var result = { users: [], methods: [] };
    
    if (!window.nim) return JSON.stringify({error: 'No nim'});
    
    // List methods that might help
    for (var key in window.nim) {
        if (typeof window.nim[key] === 'function') {
            var name = key.toLowerCase();
            if (name.indexOf('user') !== -1 || name.indexOf('friend') !== -1 || 
                name.indexOf('contact') !== -1 || name.indexOf('member') !== -1) {
                result.methods.push(key);
            }
        }
    }
    
    // Try to get local users
    if (typeof window.nim.getLocalUsers === 'function') {
        var users = window.nim.getLocalUsers() || [];
        result.localUsers = users.slice(0, 10).map(function(u) {
            return {
                account: u.account,
                nick: u.nick,
                allKeys: Object.keys(u).join(',')
            };
        });
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const nimCache = await evaluate(nimCacheScript);
    console.log('NIM Cache Result:', nimCache);
    
    // Try getTeamMemberList which might have different data
    console.log('\n=== Try Different NIM APIs ===');
    const nimApisScript = `
(async function() {
    var result = { apis: [], data: null };
    
    if (!window.nim) return JSON.stringify({error: 'No nim'});
    
    var teamId = '40821608989';
    
    // Try queryTeamMemberNickName
    if (typeof window.nim.queryTeamMemberNickName === 'function') {
        result.apis.push('queryTeamMemberNickName');
    }
    
    // Try getTeamMemberList
    if (typeof window.nim.getTeamMemberList === 'function') {
        result.apis.push('getTeamMemberList');
    }
    
    // Try getTeamMembersInfo
    if (typeof window.nim.getTeamMembersInfo === 'function') {
        result.apis.push('getTeamMembersInfo');
    }
    
    // Try getUser for specific accounts
    if (typeof window.nim.getUser === 'function') {
        result.apis.push('getUser');
        try {
            var user = await new Promise(function(resolve, reject) {
                window.nim.getUser({
                    account: '1391351554',
                    done: function(err, data) {
                        if (err) reject(err);
                        else resolve(data);
                    }
                });
                setTimeout(reject, 3000);
            });
            result.sampleUser = {
                account: user.account,
                nick: user.nick,
                allKeys: Object.keys(user).join(',')
            };
        } catch(e) {
            result.getUserError = e.message || String(e);
        }
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const nimApis = await evaluate(nimApisScript);
    console.log('NIM APIs Result:', nimApis);
    
    // Check what data Vue store has for members
    console.log('\n=== Deep Dive into Vue Store ===');
    const vueStoreScript = `
(function() {
    var result = { found: false, members: [] };
    
    var app = document.querySelector('#app');
    if (!app || !app.__vue__) return JSON.stringify({error: 'No Vue app'});
    
    var vue = app.__vue__;
    var store = vue.$store;
    
    if (!store || !store.state) return JSON.stringify({error: 'No Vuex store'});
    
    // Log all state keys
    result.stateKeys = Object.keys(store.state);
    
    // Search for team or member related state
    var stateStr = JSON.stringify(store.state).substring(0, 5000);
    
    // Look for patterns that might contain member data
    var patterns = ['members', 'teamMembers', 'groupMembers', 'userList', 'contacts'];
    patterns.forEach(function(p) {
        if (stateStr.indexOf(p) !== -1) {
            result.foundPattern = p;
        }
    });
    
    // Try to access common state paths
    var paths = ['team', 'teams', 'user', 'users', 'member', 'members', 'contact', 'contacts'];
    paths.forEach(function(p) {
        if (store.state[p]) {
            result[p + '_type'] = typeof store.state[p];
            result[p + '_keys'] = Object.keys(store.state[p]).slice(0, 10);
        }
    });
    
    return JSON.stringify(result, null, 2);
})()`;

    const vueStore = await evaluate(vueStoreScript);
    console.log('Vue Store Result:', vueStore);
    
    ws.close();
    console.log('\nExploration complete!');
}

explore().catch(console.error);

