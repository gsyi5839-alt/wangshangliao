// Deep exploration to find nickname decryption
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
    
    console.log('Connecting...');
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
    
    // 1. Find Vue components that render member names
    console.log('\n=== Finding Vue Components with Decrypted Names ===');
    const vueComponentScript = `
(function() {
    var result = { components: [], memberData: [] };
    
    // Walk all DOM elements looking for Vue components with member data
    function walkDom(el, depth) {
        if (depth > 15 || !el) return;
        
        if (el.__vue__) {
            var v = el.__vue__;
            var name = v.$options.name || v.$options._componentTag || '';
            
            // Check component data for member info
            var data = v.$data || {};
            var props = v.$props || {};
            
            // Look for member/user arrays
            function findMembers(obj, path) {
                if (!obj || typeof obj !== 'object') return;
                
                if (Array.isArray(obj) && obj.length > 0) {
                    var first = obj[0];
                    if (first && (first.account || first.accid)) {
                        // Found member array - check for decrypted names
                        result.memberData.push({
                            path: path,
                            count: obj.length,
                            sample: obj.slice(0, 5).map(function(m) {
                                return {
                                    account: m.account || m.accid,
                                    nick: m.nick,
                                    nickInTeam: m.nickInTeam,
                                    displayName: m.displayName,
                                    alias: m.alias,
                                    teamNick: m.teamNick,
                                    showName: m.showName,
                                    renderName: m.renderName
                                };
                            })
                        });
                    }
                }
                
                // Search object properties
                if (!Array.isArray(obj)) {
                    for (var key in obj) {
                        if (key.startsWith('_') || key.startsWith('$')) continue;
                        try {
                            findMembers(obj[key], path + '.' + key);
                        } catch(e) {}
                    }
                }
            }
            
            findMembers(data, 'data');
            findMembers(props, 'props');
            
            // Check computed properties
            if (v._computedWatchers) {
                for (var comp in v._computedWatchers) {
                    try {
                        var val = v[comp];
                        findMembers(val, 'computed.' + comp);
                    } catch(e) {}
                }
            }
        }
        
        // Recurse into children
        var children = el.children || [];
        for (var i = 0; i < children.length; i++) {
            walkDom(children[i], depth + 1);
        }
    }
    
    walkDom(document.body, 0);
    
    return JSON.stringify(result, null, 2);
})()`;

    const vueResult = await evaluate(vueComponentScript);
    console.log('Vue Components:', vueResult);
    
    // 2. Check for decrypt function in global scope
    console.log('\n=== Searching for Decrypt Functions ===');
    const decryptFuncScript = `
(function() {
    var result = { functions: [], modules: [] };
    
    // Search in require modules (Electron/webpack)
    if (typeof require === 'function') {
        try {
            // Try common crypto modules
            var possibleModules = ['crypto-js', 'aes-js', 'crypto', 'utils', 'decrypt'];
            possibleModules.forEach(function(m) {
                try {
                    var mod = require(m);
                    if (mod) {
                        result.modules.push({
                            name: m,
                            type: typeof mod,
                            keys: Object.keys(mod).slice(0, 20)
                        });
                    }
                } catch(e) {}
            });
        } catch(e) {}
    }
    
    // Search window for decrypt-like functions
    function searchObj(obj, path, depth) {
        if (depth > 4 || !obj) return;
        
        var keys = [];
        try { keys = Object.keys(obj); } catch(e) { return; }
        
        keys.forEach(function(key) {
            if (key.startsWith('_') || key === 'window' || key === 'self') return;
            
            try {
                var val = obj[key];
                var keyLower = key.toLowerCase();
                
                if (typeof val === 'function') {
                    if (keyLower.indexOf('decrypt') !== -1 ||
                        keyLower.indexOf('cipher') !== -1 ||
                        keyLower.indexOf('aes') !== -1 ||
                        (keyLower.indexOf('nick') !== -1 && keyLower.indexOf('name') !== -1)) {
                        result.functions.push({
                            path: path + '.' + key,
                            funcStr: val.toString().substring(0, 200)
                        });
                    }
                } else if (typeof val === 'object' && val !== null && depth < 4) {
                    searchObj(val, path + '.' + key, depth + 1);
                }
            } catch(e) {}
        });
    }
    
    // Check NIM instance for internal decrypt
    if (window.nim) {
        searchObj(window.nim, 'nim', 0);
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const decryptFuncs = await evaluate(decryptFuncScript);
    console.log('Decrypt Functions:', decryptFuncs);
    
    // 3. Check Custom field and try to decode nicknameCiphertext
    console.log('\n=== Analyzing Custom Field Encryption ===');
    const customFieldScript = `
(async function() {
    var result = { 
        samples: [],
        analysis: {}
    };
    
    if (!window.nim) return JSON.stringify({error: 'no nim'});
    
    var url = window.location.href;
    var match = url.match(/team-([0-9]+)/);
    if (!match) return JSON.stringify({error: 'no team'});
    
    var teamId = match[1];
    
    try {
        var teamData = await new Promise(function(resolve, reject) {
            window.nim.getTeamMembers({
                teamId: teamId,
                done: function(err, obj) {
                    if (err) reject(err);
                    else resolve(obj);
                }
            });
            setTimeout(reject, 10000);
        });
        
        var members = teamData.members || teamData || [];
        
        // Find members with custom field containing nicknameCiphertext
        members.forEach(function(m) {
            if (m.custom) {
                try {
                    var customObj = JSON.parse(m.custom);
                    if (customObj.nicknameCiphertext) {
                        result.samples.push({
                            account: m.account,
                            nickInTeam: m.nickInTeam,
                            ciphertext: customObj.nicknameCiphertext,
                            groupId: customObj.groupId
                        });
                    }
                } catch(e) {}
            }
        });
        
        // Analyze the ciphertext format
        if (result.samples.length > 0) {
            var sample = result.samples[0].ciphertext;
            result.analysis = {
                length: sample.length,
                isBase64: /^[A-Za-z0-9+/=]+$/.test(sample),
                base64DecodedLength: sample.length * 3 / 4
            };
            
            // Try to decode base64
            try {
                var decoded = atob(sample);
                result.analysis.decodedBytes = decoded.length;
                result.analysis.decodedHex = Array.from(decoded).map(function(c) {
                    return c.charCodeAt(0).toString(16).padStart(2, '0');
                }).join(' ');
            } catch(e) {
                result.analysis.base64Error = e.message;
            }
        }
        
    } catch(e) {
        result.error = e.message || String(e);
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const customField = await evaluate(customFieldScript);
    console.log('Custom Field Analysis:', customField);
    
    // 4. Try to find the decrypt key
    console.log('\n=== Searching for Encryption Key ===');
    const keySearchScript = `
(function() {
    var result = { possibleKeys: [] };
    
    // Search localStorage for keys
    for (var i = 0; i < localStorage.length; i++) {
        var key = localStorage.key(i);
        var keyLower = key.toLowerCase();
        if (keyLower.indexOf('key') !== -1 || 
            keyLower.indexOf('secret') !== -1 ||
            keyLower.indexOf('aes') !== -1 ||
            keyLower.indexOf('encrypt') !== -1) {
            var val = localStorage.getItem(key);
            result.possibleKeys.push({
                key: key,
                value: val ? val.substring(0, 100) : ''
            });
        }
    }
    
    // Check window for config with keys
    if (window.appConfig || window.config || window.__config__) {
        var config = window.appConfig || window.config || window.__config__;
        result.configKeys = Object.keys(config).slice(0, 20);
    }
    
    // Check NIM options
    if (window.nim && window.nim.options) {
        result.nimOptions = Object.keys(window.nim.options).filter(function(k) {
            var kl = k.toLowerCase();
            return kl.indexOf('key') !== -1 || kl.indexOf('secret') !== -1;
        });
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const keySearch = await evaluate(keySearchScript);
    console.log('Key Search:', keySearch);
    
    // 5. Try calling internal NIM functions that might decrypt
    console.log('\n=== Try NIM Internal Functions ===');
    const nimInternalScript = `
(async function() {
    var result = { methods: [], decryptedNames: [] };
    
    if (!window.nim) return JSON.stringify({error: 'no nim'});
    
    // List all NIM methods that might help
    for (var key in window.nim) {
        if (typeof window.nim[key] === 'function') {
            var keyLower = key.toLowerCase();
            if (keyLower.indexOf('nick') !== -1 ||
                keyLower.indexOf('name') !== -1 ||
                keyLower.indexOf('display') !== -1 ||
                keyLower.indexOf('render') !== -1 ||
                keyLower.indexOf('format') !== -1) {
                result.methods.push(key);
            }
        }
    }
    
    // Try getTeamMemberNickName if exists
    if (typeof window.nim.getTeamMemberNickName === 'function') {
        result.hasGetTeamMemberNickName = true;
    }
    
    // Try to get user display name through different means
    var url = window.location.href;
    var match = url.match(/team-([0-9]+)/);
    if (match) {
        var teamId = match[1];
        
        // Try getTeamMembersInfo if exists
        if (typeof window.nim.getTeamMembersInfo === 'function') {
            try {
                var info = await new Promise(function(resolve, reject) {
                    window.nim.getTeamMembersInfo({
                        teamId: teamId,
                        accounts: ['1391351554'],
                        done: function(err, data) {
                            if (err) reject(err);
                            else resolve(data);
                        }
                    });
                    setTimeout(reject, 5000);
                });
                result.membersInfo = info;
            } catch(e) {
                result.membersInfoError = e.message || String(e);
            }
        }
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const nimInternal = await evaluate(nimInternalScript);
    console.log('NIM Internal:', nimInternal);
    
    // 6. Check if there's a wangshangliao specific API
    console.log('\n=== Check WangShangLiao Specific APIs ===');
    const wslApiScript = `
(function() {
    var result = { apis: [], globalVars: [] };
    
    // Look for wangshangliao specific globals
    var searchTerms = ['wsl', 'wangshangliao', 'app', 'api', 'service', 'util'];
    
    for (var key in window) {
        var keyLower = key.toLowerCase();
        for (var i = 0; i < searchTerms.length; i++) {
            if (keyLower.indexOf(searchTerms[i]) !== -1) {
                try {
                    var val = window[key];
                    if (typeof val === 'object' && val !== null) {
                        result.globalVars.push({
                            name: key,
                            type: typeof val,
                            keys: Object.keys(val).slice(0, 15)
                        });
                    } else if (typeof val === 'function') {
                        result.apis.push(key);
                    }
                } catch(e) {}
                break;
            }
        }
    }
    
    // Check for Vue prototype extensions
    if (window.Vue) {
        result.vuePrototype = Object.keys(window.Vue.prototype).filter(function(k) {
            return k.startsWith('$') && k.length > 1;
        }).slice(0, 20);
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const wslApi = await evaluate(wslApiScript);
    console.log('WangShangLiao APIs:', wslApi);
    
    ws.close();
    console.log('\nExploration complete!');
}

explore().catch(console.error);

