// Deep DOM analysis to extract account-nickname mapping
const WebSocket = require('ws');

async function run() {
    console.log('=== Deep DOM Analysis for Account-Nickname Mapping ===\n');
    
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
    
    // First, let's understand the DOM structure of the member panel
    console.log('=== 1. Analyze Member Panel DOM Structure ===\n');
    
    const domStructure = await evaluate(`
(function() {
    var results = [];
    
    // Find member items with names like "法拉利客服"
    var nameEls = document.querySelectorAll('p.inline-block.truncate');
    
    nameEls.forEach(function(nameEl, i) {
        if (i > 5) return; // Just first few for structure analysis
        
        var name = nameEl.textContent.trim();
        if (!name || name.length > 20) return;
        
        // Walk up the DOM tree and record structure
        var path = [];
        var el = nameEl;
        for (var d = 0; d < 8 && el && el !== document.body; d++) {
            var info = {
                tag: el.tagName,
                class: el.className?.slice?.(0, 100),
                id: el.id,
                dataAttrs: {}
            };
            
            // Get all data-* attributes
            Array.from(el.attributes || []).forEach(function(attr) {
                if (attr.name.startsWith('data-')) {
                    info.dataAttrs[attr.name] = attr.value;
                }
            });
            
            path.push(info);
            el = el.parentElement;
        }
        
        results.push({
            name: name,
            path: path
        });
    });
    
    return JSON.stringify(results, null, 2);
})();
    `);
    console.log('DOM Structure:', domStructure);
    
    // Now let's analyze the member grid specifically
    console.log('\n=== 2. Analyze Member Grid Container ===\n');
    
    const memberGridAnalysis = await evaluate(`
(function() {
    var results = {};
    
    // Find the member section (contains "群成员")
    var memberSection = null;
    var spans = document.querySelectorAll('span');
    for (var span of spans) {
        if (span.textContent.includes('群成员')) {
            memberSection = span.closest('div[class*="flex"]')?.parentElement;
            break;
        }
    }
    
    if (!memberSection) {
        results.error = 'Member section not found';
        return JSON.stringify(results);
    }
    
    results.sectionClass = memberSection.className;
    results.sectionHTML = memberSection.innerHTML.slice(0, 2000);
    
    // Get all children with names
    var allDivs = memberSection.querySelectorAll('div');
    results.childCount = allDivs.length;
    
    // Look for grid layout
    var grid = memberSection.querySelector('[class*="grid"]');
    if (grid) {
        results.gridClass = grid.className;
        results.gridChildren = grid.children.length;
    }
    
    return JSON.stringify(results, null, 2);
})();
    `);
    console.log('Member Grid Analysis:', memberGridAnalysis);
    
    // Try to find Vue reactive data
    console.log('\n=== 3. Find Vue Reactive Data ===\n');
    
    const vueReactiveData = await evaluate(`
(function() {
    var results = [];
    
    // Find elements with __vueParentComponent
    function findVueData(el, depth) {
        if (depth > 5) return;
        
        // Vue 3 composition API
        if (el.__vueParentComponent) {
            var comp = el.__vueParentComponent;
            var data = comp.data || comp.setupState || {};
            
            // Check for member-related data
            var keys = Object.keys(data);
            var memberKeys = keys.filter(function(k) {
                return k.toLowerCase().includes('member') ||
                       k.toLowerCase().includes('user') ||
                       k.toLowerCase().includes('team') ||
                       k.toLowerCase().includes('list');
            });
            
            if (memberKeys.length > 0) {
                results.push({
                    componentName: comp.type?.name || 'unknown',
                    memberKeys: memberKeys,
                    sampleData: memberKeys.map(function(k) {
                        var val = data[k];
                        if (Array.isArray(val)) {
                            return { key: k, type: 'array', length: val.length, sample: val.slice(0, 2) };
                        }
                        return { key: k, type: typeof val, value: String(val).slice(0, 100) };
                    })
                });
            }
        }
        
        Array.from(el.children || []).forEach(function(child) {
            findVueData(child, depth + 1);
        });
    }
    
    findVueData(document.body, 0);
    
    return JSON.stringify(results, null, 2);
})();
    `);
    console.log('Vue Reactive Data:', vueReactiveData);
    
    // Look for click handlers on member items to find account
    console.log('\n=== 4. Analyze Click Handlers ===\n');
    
    const clickHandlers = await evaluate(`
(function() {
    var results = [];
    
    // Find name elements
    var nameEls = document.querySelectorAll('p.inline-block.truncate.color-\\\\#171717');
    
    nameEls.forEach(function(nameEl, i) {
        if (i > 10) return;
        
        var name = nameEl.textContent.trim();
        if (!name || name.length > 20 || /^\\d+$/.test(name)) return;
        
        // Find the clickable parent
        var clickable = nameEl;
        for (var d = 0; d < 5; d++) {
            clickable = clickable.parentElement;
            if (!clickable) break;
            
            // Check for click event listeners using __vue__
            if (clickable.__vueParentComponent) {
                var comp = clickable.__vueParentComponent;
                var props = comp.props || {};
                var attrs = comp.attrs || {};
                
                // Check for member/item prop
                if (props.member || props.item) {
                    var memberData = props.member || props.item;
                    results.push({
                        name: name,
                        account: memberData.account,
                        allProps: Object.keys(memberData).slice(0, 15),
                        nick: memberData.nick || memberData.nickInTeam || memberData.displayName
                    });
                    break;
                }
                
                // Check setupState
                if (comp.setupState) {
                    var state = comp.setupState;
                    if (state.member || state.item) {
                        var memberData = state.member || state.item;
                        if (memberData.account) {
                            results.push({
                                name: name,
                                account: memberData.account,
                                fromSetupState: true
                            });
                            break;
                        }
                    }
                }
            }
            
            // Check for data attributes
            var dataAccount = clickable.getAttribute('data-account');
            if (dataAccount) {
                results.push({
                    name: name,
                    account: dataAccount,
                    fromDataAttr: true
                });
                break;
            }
        }
    });
    
    return JSON.stringify(results, null, 2);
})();
    `);
    console.log('Click Handlers:', clickHandlers);
    
    // Inject a mutation observer to catch when member data is rendered
    console.log('\n=== 5. Get All Messages with fromNick ===\n');
    
    const allMsgNicks = await evaluate(`
(async function() {
    if (!window.nim) return 'nim not found';
    
    var results = {};
    var teamId = '40821608989';
    var sessionId = 'team-' + teamId;
    
    try {
        // Get more messages for better coverage
        var msgs = await new Promise((resolve, reject) => {
            window.nim.getLocalMsgs({
                sessionId: sessionId,
                limit: 500,
                done: function(err, obj) {
                    if (err) reject(err);
                    else resolve(obj);
                }
            });
            setTimeout(() => reject('timeout'), 15000);
        });
        
        var msgList = msgs.msgs || msgs || [];
        var accountToNick = {};
        var md5Pattern = /^[a-f0-9]{32}$/i;
        
        msgList.forEach(function(m) {
            if (m.from && m.fromNick) {
                // Check if fromNick is NOT MD5 hash and is meaningful
                var nick = m.fromNick.trim();
                if (!md5Pattern.test(nick) && nick.length >= 1 && nick.length < 30) {
                    // Only store if we don't have a better one already
                    if (!accountToNick[m.from] || accountToNick[m.from].length < nick.length) {
                        accountToNick[m.from] = nick;
                    }
                }
            }
        });
        
        results.totalMsgs = msgList.length;
        results.mappings = Object.entries(accountToNick).map(function([account, nick]) {
            return { account, nick };
        });
        results.mappingCount = results.mappings.length;
        
    } catch (e) {
        results.error = e.toString();
    }
    
    return JSON.stringify(results, null, 2);
})();
    `, true);
    console.log('All Message Nicks:', allMsgNicks);
    
    // Try to get nicknames from the decrypted custom field
    console.log('\n=== 6. Check nicknameCiphertext decryption ===\n');
    
    const ciphertextAnalysis = await evaluate(`
(async function() {
    if (!window.nim) return 'nim not found';
    
    var results = [];
    var teamId = '40821608989';
    
    try {
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
        
        var members = teamData.members || [];
        
        // Check if app has a decrypt method
        var hasDecrypt = false;
        var vueApp = document.querySelector('#app')?.__vue_app__;
        if (vueApp) {
            var globalProps = vueApp.config?.globalProperties || {};
            for (var key in globalProps) {
                if (typeof globalProps[key] === 'function' && 
                    (key.includes('decrypt') || key.includes('cipher'))) {
                    hasDecrypt = true;
                    results.push({ decryptMethod: key });
                }
            }
        }
        
        // Check for AES library
        if (window.CryptoJS || window.aesjs || window.forge) {
            results.push({ cryptoLib: 'found' });
        }
        
        // Analyze first few members with ciphertext
        members.slice(0, 5).forEach(function(m) {
            if (m.custom) {
                try {
                    var customData = JSON.parse(m.custom);
                    results.push({
                        account: m.account,
                        nick: m.nick,
                        nickInTeam: m.nickInTeam,
                        nicknameCiphertext: customData.nicknameCiphertext,
                        customKeys: Object.keys(customData)
                    });
                } catch (e) {}
            }
        });
        
    } catch (e) {
        results.push({ error: e.toString() });
    }
    
    return JSON.stringify(results, null, 2);
})();
    `, true);
    console.log('Ciphertext Analysis:', ciphertextAnalysis);
    
    // Final: Get DOM names and try to match with SDK data
    console.log('\n=== 7. Build Final Mapping ===\n');
    
    const finalMapping = await evaluate(`
(async function() {
    var mapping = {};
    var unmapped = [];
    
    // Step 1: Get all account->nick from messages
    var msgs = await new Promise((resolve, reject) => {
        window.nim.getLocalMsgs({
            sessionId: 'team-40821608989',
            limit: 500,
            done: function(err, obj) {
                if (err) reject(err);
                else resolve(obj);
            }
        });
        setTimeout(() => reject('timeout'), 15000);
    });
    
    var msgList = msgs.msgs || msgs || [];
    var md5Pattern = /^[a-f0-9]{32}$/i;
    
    msgList.forEach(function(m) {
        if (m.from && m.fromNick && !md5Pattern.test(m.fromNick) && m.fromNick.length < 30) {
            mapping[m.from] = m.fromNick.trim();
        }
    });
    
    // Step 2: Get all DOM displayed names
    var domNames = [];
    var nameEls = document.querySelectorAll('p.inline-block.truncate');
    nameEls.forEach(function(el) {
        var name = el.textContent.trim();
        if (name && name.length < 20 && !/^\\d+$/.test(name)) {
            domNames.push(name);
        }
    });
    
    // Step 3: Check how many DOM names are mapped
    var mappedNames = new Set(Object.values(mapping));
    var unmappedDomNames = domNames.filter(function(name) {
        return !mappedNames.has(name);
    });
    
    // Step 4: Get team members for complete account list
    var teamData = await new Promise((resolve, reject) => {
        window.nim.getTeamMembers({
            teamId: '40821608989',
            done: function(err, obj) {
                if (err) reject(err);
                else resolve(obj);
            }
        });
        setTimeout(() => reject('timeout'), 5000);
    });
    
    var members = teamData.members || [];
    var allAccounts = members.map(m => m.account);
    var unmappedAccounts = allAccounts.filter(a => !mapping[a]);
    
    return JSON.stringify({
        fromMessages: Object.keys(mapping).length,
        domNamesCount: domNames.length,
        domNamesUnique: [...new Set(domNames)].length,
        unmappedDomNamesCount: unmappedDomNames.length,
        unmappedDomNamesSample: unmappedDomNames.slice(0, 20),
        totalAccounts: allAccounts.length,
        unmappedAccountsCount: unmappedAccounts.length,
        mapping: mapping
    }, null, 2);
})();
    `, true);
    console.log('Final Mapping:', finalMapping);
    
    ws.close();
    console.log('\n=== Done ===');
}

run().catch(console.error);

