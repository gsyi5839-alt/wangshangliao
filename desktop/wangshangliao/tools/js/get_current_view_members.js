// Get current view members from Vue/Pinia store
const WebSocket = require('ws');
const crypto = require('crypto');

const CDP_URL = 'ws://127.0.0.1:9222/devtools/page/8322EEB7A02952E8C4C59B59B616C299';

const KEY = Buffer.from('d6ba6647b7c43b79d0e42ceb2790e342', 'utf8');
const IV = Buffer.from('kgWRyiiODMjSCh0m', 'utf8');

function decrypt(ciphertext) {
    try {
        const decipher = crypto.createDecipheriv('aes-256-cbc', KEY, IV);
        let decrypted = decipher.update(Buffer.from(ciphertext, 'base64'));
        decrypted = Buffer.concat([decrypted, decipher.final()]);
        return decrypted.toString('utf8');
    } catch (e) {
        return null;
    }
}

async function getCurrentViewMembers() {
    console.log('=== 从当前界面获取群成员 ===\n');
    
    const ws = new WebSocket(CDP_URL);
    
    await new Promise((resolve, reject) => {
        ws.on('open', resolve);
        ws.on('error', reject);
    });
    
    console.log('已连接CDP!');
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
    
    // Get current session info from Pinia/Vue
    console.log('\n1. 获取当前会话信息...');
    const sessionScript = `
(function() {
    var result = { 
        currentSession: null,
        stores: [],
        vueData: null
    };
    
    // Try to get from Pinia
    if (window.__pinia) {
        var stores = window.__pinia._s;
        if (stores) {
            stores.forEach((store, key) => {
                result.stores.push({
                    name: key,
                    keys: Object.keys(store.$state || store).slice(0, 20)
                });
            });
        }
        
        // Get app store
        var appStore = stores.get('app');
        if (appStore) {
            var state = appStore.$state || appStore;
            result.currentSession = state.currentSession;
            result.appStoreKeys = Object.keys(state);
        }
        
        // Get sdk store for group members
        var sdkStore = stores.get('sdk');
        if (sdkStore) {
            var sdkState = sdkStore.$state || sdkStore;
            result.sdkStoreKeys = Object.keys(sdkState);
            
            // Try to get groupMembersMap
            if (sdkState.groupMembersMap) {
                result.groupMembersMapKeys = Object.keys(sdkState.groupMembersMap);
            }
        }
        
        // Get cache store
        var cacheStore = stores.get('cache');
        if (cacheStore) {
            var cacheState = cacheStore.$state || cacheStore;
            result.cacheStoreKeys = Object.keys(cacheState);
        }
    }
    
    // Try to get from URL
    var url = window.location.href;
    var match = url.match(/team-([0-9]+)/);
    if (match) {
        result.teamIdFromUrl = match[1];
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const sessionResult = await evaluate(sessionScript);
    console.log('会话信息:', sessionResult);
    
    // Get group members from SDK store
    console.log('\n2. 从SDK Store获取群成员...');
    const membersFromStoreScript = `
(function() {
    var result = { members: [], error: null };
    
    try {
        if (!window.__pinia) {
            result.error = 'Pinia not found';
            return JSON.stringify(result);
        }
        
        var stores = window.__pinia._s;
        var sdkStore = stores.get('sdk');
        
        if (!sdkStore) {
            result.error = 'SDK store not found';
            return JSON.stringify(result);
        }
        
        var state = sdkStore.$state || sdkStore;
        
        // Get current team ID from URL
        var url = window.location.href;
        var match = url.match(/team-([0-9]+)/);
        var currentTeamId = match ? match[1] : null;
        result.currentTeamId = currentTeamId;
        
        // Get all team IDs in groupMembersMap
        if (state.groupMembersMap) {
            result.availableTeams = Object.keys(state.groupMembersMap);
            
            // Get members for all teams we have
            for (var teamId in state.groupMembersMap) {
                var teamMembers = state.groupMembersMap[teamId];
                if (teamMembers && teamMembers.length > 0) {
                    result.members.push({
                        teamId: teamId,
                        count: teamMembers.length,
                        sample: teamMembers.slice(0, 5).map(m => ({
                            account: m.account,
                            nick: m.nick,
                            nickInTeam: m.nickInTeam,
                            custom: m.custom ? m.custom.substring(0, 200) : null
                        }))
                    });
                }
            }
        }
        
        // Also check groupInfoMap
        if (state.groupInfoMap) {
            result.groupInfoMapKeys = Object.keys(state.groupInfoMap);
        }
        
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const membersFromStore = await evaluate(membersFromStoreScript);
    console.log('SDK Store成员:', membersFromStore);
    
    // Get from Vue components
    console.log('\n3. 从Vue组件获取当前显示的成员...');
    const vueComponentsScript = `
(function() {
    var result = { components: [], memberData: null };
    
    function findVueData(el, depth) {
        if (depth > 15 || !el) return;
        
        if (el.__vue__) {
            var v = el.__vue__;
            var name = v.$options.name || v.$options._componentTag || '';
            
            // Look for components that might have member data
            var data = v.$data || {};
            var props = v.$props || {};
            
            // Check for member-related data
            for (var key in data) {
                if (key.toLowerCase().includes('member') || 
                    key.toLowerCase().includes('team') ||
                    key.toLowerCase().includes('group')) {
                    result.components.push({
                        name: name,
                        dataKey: key,
                        dataType: typeof data[key],
                        isArray: Array.isArray(data[key]),
                        length: Array.isArray(data[key]) ? data[key].length : null
                    });
                    
                    // If it's an array with members, get sample
                    if (Array.isArray(data[key]) && data[key].length > 0) {
                        var sample = data[key].slice(0, 10).map(function(m) {
                            if (typeof m === 'object') {
                                return {
                                    account: m.account,
                                    nick: m.nick,
                                    nickInTeam: m.nickInTeam,
                                    custom: m.custom ? m.custom.substring(0, 200) : null
                                };
                            }
                            return m;
                        });
                        result.memberData = result.memberData || [];
                        result.memberData.push({
                            component: name,
                            key: key,
                            sample: sample
                        });
                    }
                }
            }
        }
        
        var children = el.children || [];
        for (var i = 0; i < children.length; i++) {
            findVueData(children[i], depth + 1);
        }
    }
    
    findVueData(document.body, 0);
    return JSON.stringify(result, null, 2);
})()`;

    const vueComponents = await evaluate(vueComponentsScript);
    console.log('Vue组件数据:', vueComponents);
    
    // Direct DOM scraping for displayed nicknames
    console.log('\n4. 从DOM获取显示的成员昵称...');
    const domMembersScript = `
(function() {
    var result = { members: [] };
    
    // Find the member list sidebar
    var allElements = document.querySelectorAll('*');
    var memberListContainer = null;
    
    // Look for element containing "群成员"
    for (var i = 0; i < allElements.length; i++) {
        var el = allElements[i];
        if (el.textContent && el.textContent.includes('群成员') && 
            el.textContent.length < 50) {
            // Found the header, look for parent container
            var parent = el.parentElement;
            for (var j = 0; j < 5 && parent; j++) {
                if (parent.querySelectorAll('*').length > 30) {
                    memberListContainer = parent;
                    break;
                }
                parent = parent.parentElement;
            }
            break;
        }
    }
    
    if (memberListContainer) {
        result.containerFound = true;
        
        // Get all text nodes that look like nicknames
        var textElements = memberListContainer.querySelectorAll('span, div, p');
        var seen = new Set();
        
        textElements.forEach(function(te) {
            var text = te.textContent.trim();
            // Filter for likely nicknames
            if (text.length > 0 && text.length < 20 &&
                !text.match(/^[0-9\\/\\(\\)\\s]+$/) &&
                !text.includes('群成员') &&
                !text.includes('搜索') &&
                !text.includes('群公告') &&
                !seen.has(text)) {
                seen.add(text);
                result.members.push(text);
            }
        });
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const domMembers = await evaluate(domMembersScript);
    console.log('DOM显示的成员:', domMembers);
    
    ws.close();
    console.log('\n完成!');
}

getCurrentViewMembers().catch(console.error);
