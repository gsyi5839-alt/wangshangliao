// Try to match DOM member order with SDK member order
const WebSocket = require('ws');

async function run() {
    console.log('=== Match DOM Order with SDK Data ===\n');
    
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
    
    // Get member list from SDK with their order
    console.log('Getting SDK member list...\n');
    
    const matchResult = await evaluate(`
(async function() {
    if (!window.nim) return 'nim not found';
    
    var teamId = '40821608989';
    var results = {
        matched: [],
        unmatched: [],
        sdkMembers: [],
        domNames: []
    };
    
    // Get SDK members
    var teamData = await new Promise((resolve, reject) => {
        window.nim.getTeamMembers({
            teamId: teamId,
            done: function(err, obj) {
                if (err) reject(err);
                else resolve(obj);
            }
        });
        setTimeout(() => reject('timeout'), 10000);
    });
    
    var members = teamData.members || [];
    
    // Store SDK members with their index
    results.sdkMembers = members.map(function(m, i) {
        return {
            index: i,
            account: m.account,
            nickHash: m.nick,
            nickInTeam: m.nickInTeam
        };
    });
    
    // Get DOM names in order from the member panel
    // The panel uses el-table-v2 with virtual scrolling
    var memberPanel = document.querySelector('.el-table-v2__body');
    if (memberPanel) {
        // Get all visible rows
        var rows = memberPanel.querySelectorAll('.el-table-v2__row');
        rows.forEach(function(row, i) {
            var nameEl = row.querySelector('p.inline-block.truncate');
            if (nameEl) {
                results.domNames.push({
                    domIndex: i,
                    name: nameEl.textContent.trim()
                });
            }
        });
    }
    
    // Also get names from the right panel (member grid)
    var rightPanel = document.querySelector('[class*="member-grid"], [class*="pl-12px"]');
    if (rightPanel) {
        var gridNames = rightPanel.querySelectorAll('p.inline-block.truncate.color-\\\\#171717');
        var rightPanelNames = [];
        gridNames.forEach(function(el, i) {
            var name = el.textContent.trim();
            if (name && !/^\\d+$/.test(name)) {
                rightPanelNames.push({ gridIndex: i, name: name });
            }
        });
        results.rightPanelNames = rightPanelNames;
    }
    
    // Try to match based on the fact that the list might be in the same order
    // First, scroll to top to ensure we're looking at the first members
    results.note = 'DOM shows only visible rows due to virtual scrolling';
    
    return JSON.stringify(results, null, 2);
})();
    `, true);
    console.log('Match Result:', matchResult);
    
    // Try another approach - intercept the rendering
    console.log('\n=== Try to intercept rendering ===\n');
    
    const interceptResult = await evaluate(`
(function() {
    var results = [];
    
    // Find the Vue component that renders member names
    // It should have access to both the account and the decrypted name
    
    // Walk through Vue components looking for member data
    function findMemberComponent(el, depth) {
        if (depth > 10) return;
        
        // Check for Vue 3 instance
        var vueInstance = el.__vueParentComponent;
        if (vueInstance) {
            var props = vueInstance.props || {};
            var setupState = vueInstance.setupState || {};
            var ctx = vueInstance.ctx || {};
            
            // Check for member data in various places
            var member = props.member || setupState.member || ctx.member;
            var item = props.item || setupState.item || ctx.item;
            var data = props.data || setupState.data;
            
            var memberObj = member || item || (Array.isArray(data) ? data[0] : null);
            
            if (memberObj && memberObj.account) {
                // Found a member object!
                results.push({
                    account: memberObj.account,
                    nick: memberObj.nick,
                    nickInTeam: memberObj.nickInTeam,
                    displayName: memberObj.displayName || memberObj.name,
                    elementText: el.textContent?.slice(0, 50),
                    depth: depth
                });
            }
            
            // Also check for computed decrypted nickname
            if (ctx.$options?.computed) {
                var computed = ctx.$options.computed;
                for (var key in computed) {
                    if (key.includes('nick') || key.includes('name')) {
                        try {
                            var val = ctx[key];
                            if (val && typeof val === 'string') {
                                results.push({
                                    computedKey: key,
                                    value: val,
                                    depth: depth
                                });
                            }
                        } catch (e) {}
                    }
                }
            }
        }
        
        Array.from(el.children || []).forEach(function(child) {
            findMemberComponent(child, depth + 1);
        });
    }
    
    // Focus on the member panel area
    var memberArea = document.querySelector('[class*="el-table-v2"], [class*="member"]');
    if (memberArea) {
        findMemberComponent(memberArea, 0);
    }
    
    // Deduplicate
    var seen = new Set();
    results = results.filter(function(r) {
        var key = r.account || r.computedKey;
        if (seen.has(key)) return false;
        seen.add(key);
        return true;
    });
    
    return JSON.stringify(results.slice(0, 30), null, 2);
})();
    `);
    console.log('Intercept Result:', interceptResult);
    
    // Try to find the decrypt function by looking at imported modules
    console.log('\n=== Look for decrypt function in modules ===\n');
    
    const moduleSearch = await evaluate(`
(function() {
    var results = {
        foundDecrypt: false,
        modules: []
    };
    
    // Check for webpack modules
    if (window.__webpack_require__) {
        results.hasWebpack = true;
        
        // Try to find crypto/decrypt modules
        try {
            var cache = window.__webpack_require__.c || {};
            var moduleIds = Object.keys(cache).slice(0, 100);
            
            moduleIds.forEach(function(id) {
                var mod = cache[id];
                if (mod && mod.exports) {
                    var exports = mod.exports;
                    var exportKeys = Object.keys(exports || {});
                    
                    var cryptoKeys = exportKeys.filter(function(k) {
                        return k.toLowerCase().includes('decrypt') ||
                               k.toLowerCase().includes('cipher') ||
                               k.toLowerCase().includes('aes');
                    });
                    
                    if (cryptoKeys.length > 0) {
                        results.modules.push({
                            id: id,
                            cryptoKeys: cryptoKeys
                        });
                        results.foundDecrypt = true;
                    }
                }
            });
        } catch (e) {
            results.webpackError = e.message;
        }
    }
    
    // Also check global scope
    var globalCrypto = [];
    for (var key in window) {
        try {
            var val = window[key];
            if (typeof val === 'function' || typeof val === 'object') {
                var str = String(val).toLowerCase();
                if (str.includes('decrypt') || str.includes('aes') || str.includes('cipher')) {
                    globalCrypto.push(key);
                }
            }
        } catch (e) {}
    }
    results.globalCrypto = globalCrypto.slice(0, 10);
    
    return JSON.stringify(results, null, 2);
})();
    `);
    console.log('Module Search:', moduleSearch);
    
    // Finally, try to hook the decryption
    console.log('\n=== Try to hook network request for decryption ===\n');
    
    const hookDecrypt = await evaluate(`
(function() {
    var results = {};
    
    // Check if there's a custom property getter that decrypts nicknames
    // Vue 3 uses reactive getters
    
    // Find a member element and trace its reactive dependencies
    var memberEl = document.querySelector('p.inline-block.truncate.color-\\\\#171717');
    if (memberEl) {
        var text = memberEl.textContent.trim();
        results.sampleText = text;
        
        // Walk up to find Vue instance
        var el = memberEl;
        for (var i = 0; i < 10 && el; i++) {
            if (el.__vueParentComponent) {
                var comp = el.__vueParentComponent;
                results.componentType = comp.type?.name || 'anonymous';
                
                // Check for render function
                if (comp.render) {
                    results.hasRender = true;
                }
                
                // Check subTree (rendered vnode)
                if (comp.subTree) {
                    results.subTreeType = comp.subTree.type;
                }
                
                // Check setup state for reactive references
                if (comp.setupState) {
                    var stateKeys = Object.keys(comp.setupState);
                    results.setupStateKeys = stateKeys;
                    
                    // Look for member or name related state
                    stateKeys.forEach(function(key) {
                        if (key.includes('member') || key.includes('name') || key.includes('nick')) {
                            var val = comp.setupState[key];
                            results['state_' + key] = typeof val === 'object' ? 
                                JSON.stringify(val, null, 2).slice(0, 500) : String(val);
                        }
                    });
                }
                
                break;
            }
            el = el.parentElement;
        }
    }
    
    return JSON.stringify(results, null, 2);
})();
    `);
    console.log('Hook Decrypt:', hookDecrypt);
    
    ws.close();
    console.log('\n=== Done ===');
}

run().catch(console.error);

