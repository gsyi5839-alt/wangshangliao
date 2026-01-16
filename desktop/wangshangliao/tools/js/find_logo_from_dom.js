// Find "logo" account by checking DOM and correlating with members
const WebSocket = require('ws');
const crypto = require('crypto');

const CDP_URL = 'ws://127.0.0.1:9222/devtools/page/8322EEB7A02952E8C4C59B59B616C299';
const TEAM_ID = '40821608989';

async function findLogoAccount() {
    console.log('=== 通过前端缓存找到 "logo" 的账号 ===\n');
    
    const ws = new WebSocket(CDP_URL);
    
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
    
    // Search in global cache and stores for member-nick mapping
    console.log('1. 搜索前端缓存...');
    const cacheSearchScript = `
(function() {
    var result = { found: false, searches: [] };
    
    // 1. Search in window.nim cache
    if (window.nim && window.nim.cache) {
        var cache = window.nim.cache;
        result.searches.push({ name: 'nim.cache', keys: Object.keys(cache).slice(0, 20) });
        
        // Check teams cache
        if (cache.teams) {
            for (var teamId in cache.teams) {
                var team = cache.teams[teamId];
                if (team.teamId === '${TEAM_ID}') {
                    result.searches.push({ name: 'nim.cache.teams', teamData: team });
                }
            }
        }
        
        // Check team members cache
        if (cache.teamMembers) {
            var teamMembers = cache.teamMembers['${TEAM_ID}'];
            if (teamMembers) {
                // Search for logo
                for (var account in teamMembers) {
                    var member = teamMembers[account];
                    var nick = member.nick || member.nickInTeam || '';
                    if (nick.toLowerCase().includes('logo')) {
                        result.found = true;
                        result.logoAccount = account;
                        result.logoData = member;
                    }
                }
            }
        }
    }
    
    // 2. Search window globals
    for (var key in window) {
        if (key.toLowerCase().includes('cache') || 
            key.toLowerCase().includes('store') ||
            key.toLowerCase().includes('member')) {
            try {
                var val = window[key];
                if (val && typeof val === 'object') {
                    result.searches.push({ name: key, type: typeof val });
                }
            } catch(e) {}
        }
    }
    
    // 3. Search Vue reactive data
    var allElements = document.querySelectorAll('*');
    for (var i = 0; i < allElements.length && !result.found; i++) {
        var el = allElements[i];
        if (el.__vue__) {
            var v = el.__vue__;
            var data = v.$data || {};
            
            // Check for members array
            for (var dataKey in data) {
                var dataVal = data[dataKey];
                if (Array.isArray(dataVal)) {
                    for (var j = 0; j < dataVal.length; j++) {
                        var item = dataVal[j];
                        if (item && typeof item === 'object') {
                            var itemNick = item.nick || item.nickInTeam || item.displayName || '';
                            if (itemNick.toLowerCase() === 'logo') {
                                result.found = true;
                                result.logoAccount = item.account || item.accid;
                                result.logoData = {
                                    account: item.account || item.accid,
                                    nick: itemNick,
                                    component: v.$options.name
                                };
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const cacheResult = await evaluate(cacheSearchScript);
    console.log('缓存搜索结果:', cacheResult);
    
    // Also try to get user info directly by nick
    console.log('\n2. 尝试使用 getUsers API...');
    const getUsersScript = `
(async function() {
    // Try getLocalUser or searchUser
    if (window.nim) {
        try {
            // Get all team members and look for logo in resolved nicks
            var result = await new Promise((resolve, reject) => {
                window.nim.getTeamMembers({
                    teamId: '${TEAM_ID}',
                    done: function(err, obj) {
                        if (err) reject(err);
                        else resolve(obj);
                    }
                });
                setTimeout(() => reject('timeout'), 10000);
            });
            
            var members = result.members || [];
            
            // Get user info for each member
            var accounts = members.map(m => m.account);
            
            var usersResult = await new Promise((resolve, reject) => {
                window.nim.getUsers({
                    accounts: accounts,
                    done: function(err, users) {
                        if (err) reject(err);
                        else resolve(users);
                    }
                });
                setTimeout(() => reject('timeout'), 15000);
            });
            
            // Find logo in users
            var logoUser = null;
            for (var i = 0; i < usersResult.length; i++) {
                var user = usersResult[i];
                var nick = user.nick || user.displayName || '';
                if (nick.toLowerCase() === 'logo') {
                    logoUser = user;
                    break;
                }
            }
            
            return JSON.stringify({
                totalUsers: usersResult.length,
                logoUser: logoUser,
                sample: usersResult.slice(0, 5).map(u => ({
                    account: u.account,
                    nick: u.nick
                }))
            });
        } catch(e) {
            return JSON.stringify({ error: e.message || String(e) });
        }
    }
    return JSON.stringify({ error: 'nim not found' });
})()`;

    const usersResult = await evaluate(getUsersScript);
    console.log('用户搜索结果:', usersResult);
    
    // Try to get cached/displayed member info from DOM
    console.log('\n3. 从DOM组件获取成员映射...');
    const domMappingScript = `
(function() {
    var result = { mappings: [] };
    
    // Find member list items in DOM
    var memberItems = document.querySelectorAll('[class*="member"], [class*="user"]');
    
    memberItems.forEach(function(item) {
        // Try to get Vue data
        if (item.__vue__) {
            var v = item.__vue__;
            var member = v.member || v.user || v.item || v.$props?.member || v.$props?.user;
            
            if (member) {
                result.mappings.push({
                    account: member.account || member.accid,
                    nick: member.nick || member.displayName,
                    nickInTeam: member.nickInTeam
                });
            }
        }
        
        // Also check data attributes
        var account = item.getAttribute('data-account') || item.getAttribute('data-accid');
        var textContent = item.textContent.trim().substring(0, 30);
        
        if (account || textContent.toLowerCase().includes('logo')) {
            result.mappings.push({
                element: item.className.substring(0, 50),
                account: account,
                text: textContent
            });
        }
    });
    
    return JSON.stringify(result, null, 2);
})()`;

    const domMapping = await evaluate(domMappingScript);
    console.log('DOM映射:', domMapping);
    
    ws.close();
}

findLogoAccount().catch(console.error);
