// Search for decrypt function in WangShangLiao's bundled JS
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
    if (!page) return console.log('Not found');
    
    const ws = new WebSocket(page.webSocketDebuggerUrl);
    await new Promise((resolve, reject) => {
        ws.on('open', resolve);
        ws.on('error', reject);
    });
    
    let msgId = 1;
    async function evaluate(expression) {
        return new Promise((resolve) => {
            const id = msgId++;
            const handler = (data) => {
                const msg = JSON.parse(data);
                if (msg.id === id) {
                    ws.off('message', handler);
                    resolve(msg.result?.result?.value);
                }
            };
            ws.on('message', handler);
            ws.send(JSON.stringify({
                id,
                method: 'Runtime.evaluate',
                params: { expression, returnByValue: true, awaitPromise: true }
            }));
        });
    }
    
    // Try to find any module that handles nickname decryption
    console.log('\n=== Searching webpack modules for decrypt ===');
    const webpackSearch = `
(function() {
    var result = { modules: [], decryptFuncs: [] };
    
    // Try to access webpack modules via different methods
    var webpackRequire = null;
    
    // Method 1: webpackJsonp
    if (window.webpackJsonp) {
        result.hasWebpackJsonp = true;
        try {
            // Get the webpack require function
            var testModule = [['test'], { 'test': function(m, e, r) { webpackRequire = r; } }];
            window.webpackJsonp.push(testModule);
        } catch(e) {}
    }
    
    // Method 2: Look for __webpack_require__ in global
    if (window.__webpack_require__) {
        webpackRequire = window.__webpack_require__;
        result.hasWebpackRequire = true;
    }
    
    // Method 3: Check module.exports patterns
    if (typeof require === 'function') {
        result.hasRequire = true;
        
        // Try to find decrypt-related modules
        var modulePatterns = ['./decrypt', './crypto', './utils', './helper', './nick', './aes'];
        modulePatterns.forEach(function(p) {
            try {
                var mod = require(p);
                if (mod) {
                    result.modules.push({
                        pattern: p,
                        type: typeof mod,
                        keys: typeof mod === 'object' ? Object.keys(mod).slice(0, 10) : []
                    });
                }
            } catch(e) {}
        });
    }
    
    // Search in window for any decrypt-like functions
    function searchDeep(obj, path, depth) {
        if (depth > 4 || !obj || typeof obj !== 'object') return;
        if (path.indexOf('window.window') !== -1) return; // Avoid circular
        
        try {
            var keys = Object.keys(obj);
            for (var i = 0; i < keys.length; i++) {
                var key = keys[i];
                if (key.startsWith('_') || key === 'window' || key === 'self' || key === 'parent' || key === 'top' || key === 'frames') continue;
                
                try {
                    var val = obj[key];
                    var keyLower = key.toLowerCase();
                    
                    if (typeof val === 'function') {
                        var funcStr = val.toString();
                        // Look for AES/decrypt keywords in function body
                        if (funcStr.indexOf('decrypt') !== -1 || 
                            funcStr.indexOf('AES') !== -1 ||
                            funcStr.indexOf('aes') !== -1 ||
                            (funcStr.indexOf('nickname') !== -1 && funcStr.indexOf('Cipher') !== -1)) {
                            result.decryptFuncs.push({
                                path: path + '.' + key,
                                preview: funcStr.substring(0, 300)
                            });
                        }
                    } else if (typeof val === 'object' && val !== null) {
                        searchDeep(val, path + '.' + key, depth + 1);
                    }
                } catch(e) {}
            }
        } catch(e) {}
    }
    
    searchDeep(window, 'window', 0);
    
    return JSON.stringify(result, null, 2);
})()`;

    const webpack = await evaluate(webpackSearch);
    console.log('Webpack Search:', webpack);
    
    // Try to hook into the rendering process to capture decrypted names
    console.log('\n=== Try to find Vue filters or computed for nickname ===');
    const vueFilterSearch = `
(function() {
    var result = { filters: [], computed: [], methods: [] };
    
    // Check Vue global filters
    if (window.Vue && window.Vue.options && window.Vue.options.filters) {
        result.filters = Object.keys(window.Vue.options.filters);
    }
    
    // Walk DOM to find components that render member names
    function findMemberComponents(el, depth) {
        if (depth > 10 || !el) return;
        
        if (el.__vue__) {
            var v = el.__vue__;
            var name = v.$options.name || '';
            
            // Check computed properties for nickname-related
            if (v._computedWatchers) {
                for (var comp in v._computedWatchers) {
                    var compLower = comp.toLowerCase();
                    if (compLower.indexOf('nick') !== -1 || 
                        compLower.indexOf('name') !== -1 ||
                        compLower.indexOf('display') !== -1) {
                        try {
                            result.computed.push({
                                component: name,
                                property: comp,
                                value: v[comp]
                            });
                        } catch(e) {}
                    }
                }
            }
            
            // Check methods
            if (v.$options.methods) {
                for (var method in v.$options.methods) {
                    var methodLower = method.toLowerCase();
                    if (methodLower.indexOf('nick') !== -1 ||
                        methodLower.indexOf('decrypt') !== -1 ||
                        methodLower.indexOf('display') !== -1) {
                        result.methods.push({
                            component: name,
                            method: method
                        });
                    }
                }
            }
        }
        
        var children = el.children || [];
        for (var i = 0; i < children.length; i++) {
            findMemberComponents(children[i], depth + 1);
        }
    }
    
    findMemberComponents(document.body, 0);
    
    return JSON.stringify(result, null, 2);
})()`;

    const vueFilter = await evaluate(vueFilterSearch);
    console.log('Vue Filters:', vueFilter);
    
    // Try to intercept the member list render to get decrypted data
    console.log('\n=== Try to get displayed member data from DOM ===');
    const domDataSearch = `
(async function() {
    var result = { members: [] };
    
    // Find the member list in the right sidebar
    // Look for elements that have click handlers or data attributes
    var sidebar = document.querySelector('[class*="right"]') || 
                  document.querySelector('[class*="sidebar"]') ||
                  document.querySelector('[class*="member"]');
    
    if (!sidebar) {
        // Try to find by looking for 群成员 text
        var allElements = document.querySelectorAll('*');
        for (var i = 0; i < allElements.length; i++) {
            if (allElements[i].textContent.indexOf('群成员') !== -1 && 
                allElements[i].textContent.length < 20) {
                sidebar = allElements[i].closest('[class*="container"]') || 
                          allElements[i].parentElement.parentElement.parentElement;
                break;
            }
        }
    }
    
    if (!sidebar) {
        return JSON.stringify({error: 'No sidebar found'});
    }
    
    result.sidebarClass = sidebar.className;
    
    // Find individual member items
    var memberItems = sidebar.querySelectorAll('[class*="item"], [class*="member"] > div');
    result.itemCount = memberItems.length;
    
    // For each item, try to extract displayed name and any associated data
    memberItems.forEach(function(item, idx) {
        if (idx > 30) return; // Limit
        
        var memberInfo = {
            text: item.textContent.trim().substring(0, 50),
            className: item.className.substring(0, 50)
        };
        
        // Check for Vue data
        var el = item;
        while (el && !memberInfo.vueData) {
            if (el.__vue__) {
                var v = el.__vue__;
                // Look for member/user data in component
                var data = v.$data || {};
                var props = v.$props || {};
                
                if (data.member || data.item || data.user) {
                    var m = data.member || data.item || data.user;
                    memberInfo.vueData = {
                        account: m.account || m.accid,
                        nick: m.nick,
                        nickInTeam: m.nickInTeam,
                        displayName: m.displayName
                    };
                }
                if (props.member || props.item || props.user) {
                    var m = props.member || props.item || props.user;
                    memberInfo.vueData = {
                        account: m.account || m.accid,
                        nick: m.nick,
                        nickInTeam: m.nickInTeam,
                        displayName: m.displayName
                    };
                }
            }
            el = el.parentElement;
        }
        
        if (memberInfo.text.length > 1 && memberInfo.text.length < 20) {
            result.members.push(memberInfo);
        }
    });
    
    return JSON.stringify(result, null, 2);
})()`;

    const domData = await evaluate(domDataSearch);
    console.log('DOM Data:', domData);
    
    ws.close();
}

explore().catch(console.error);

