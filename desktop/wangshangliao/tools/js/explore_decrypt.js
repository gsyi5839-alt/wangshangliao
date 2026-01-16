// Find and test decryption for nicknameCiphertext
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
    
    // Method 1: Search for decrypt/decode functions in global scope
    console.log('\n=== Searching for Decrypt Functions ===');
    const searchScript = `
(function() {
    var result = { 
        functions: [],
        cryptoFuncs: [],
        decryptFound: []
    };
    
    // Search window for crypto/decrypt functions
    function searchObj(obj, path, depth) {
        if (depth > 3 || !obj) return;
        
        var keys = [];
        try {
            keys = Object.keys(obj);
        } catch(e) { return; }
        
        for (var i = 0; i < keys.length; i++) {
            var key = keys[i];
            var keyLower = key.toLowerCase();
            
            try {
                var val = obj[key];
                
                if (typeof val === 'function') {
                    // Look for decrypt/cipher/aes related functions
                    if (keyLower.indexOf('decrypt') !== -1 ||
                        keyLower.indexOf('cipher') !== -1 ||
                        keyLower.indexOf('aes') !== -1 ||
                        keyLower.indexOf('crypto') !== -1) {
                        result.decryptFound.push({
                            path: path + '.' + key,
                            funcString: val.toString().substring(0, 200)
                        });
                    }
                }
                
                // Search deeper
                if (val && typeof val === 'object' && depth < 3) {
                    searchObj(val, path + '.' + key, depth + 1);
                }
            } catch(e) {}
        }
    }
    
    // Search common locations
    searchObj(window, 'window', 0);
    
    return JSON.stringify(result, null, 2);
})()`;

    const searchResult = await evaluate(searchScript);
    console.log('Search Result:', searchResult);
    
    // Method 2: Check Vue app for decrypt methods
    console.log('\n=== Checking Vue App for Decrypt Methods ===');
    const vueDecryptScript = `
(function() {
    var result = { methods: [], found: false };
    
    var app = document.querySelector('#app');
    if (!app || !app.__vue__) return JSON.stringify({error: 'No Vue'});
    
    // Search Vue methods
    var vue = app.__vue__;
    
    function searchVue(obj, path, depth) {
        if (depth > 5 || !obj) return;
        
        var keys = [];
        try { keys = Object.keys(obj); } catch(e) { return; }
        
        for (var i = 0; i < keys.length; i++) {
            var key = keys[i];
            var keyLower = key.toLowerCase();
            
            if (keyLower.indexOf('decrypt') !== -1 ||
                keyLower.indexOf('nick') !== -1 ||
                keyLower.indexOf('name') !== -1) {
                try {
                    var val = obj[key];
                    if (typeof val === 'function') {
                        result.methods.push({
                            path: path + '.' + key,
                            func: val.toString().substring(0, 300)
                        });
                    }
                } catch(e) {}
            }
        }
    }
    
    searchVue(vue, 'vue', 0);
    searchVue(vue.$options, 'options', 0);
    searchVue(vue.$root, 'root', 0);
    
    return JSON.stringify(result, null, 2);
})()`;

    const vueDecrypt = await evaluate(vueDecryptScript);
    console.log('Vue Decrypt:', vueDecrypt);
    
    // Method 3: Look at how DOM displays nicknames - trace back to source
    console.log('\n=== Tracing DOM Nickname Display ===');
    const domTraceScript = `
(function() {
    var result = { elements: [], computed: [] };
    
    // Find elements that display member names
    var memberElements = document.querySelectorAll('.member-item, .member-name, .nick, [class*="member"]');
    
    memberElements = Array.from(memberElements).slice(0, 5);
    
    memberElements.forEach(function(el) {
        var data = {
            className: el.className,
            text: el.textContent.substring(0, 50),
            vueData: null
        };
        
        // Check if element has Vue binding
        if (el.__vue__) {
            var v = el.__vue__;
            data.vueData = {
                hasData: !!v.$data,
                props: v.$props ? Object.keys(v.$props) : [],
                computed: v._computedWatchers ? Object.keys(v._computedWatchers) : []
            };
        }
        
        result.elements.push(data);
    });
    
    return JSON.stringify(result, null, 2);
})()`;

    const domTrace = await evaluate(domTraceScript);
    console.log('DOM Trace:', domTrace);
    
    // Method 4: Try to find CryptoJS or similar library
    console.log('\n=== Looking for CryptoJS or Similar ===');
    const cryptoLibScript = `
(function() {
    var result = { 
        cryptoJS: !!window.CryptoJS,
        forge: !!window.forge,
        jsencrypt: !!window.JSEncrypt,
        aesjs: !!window.aesjs,
        otherCrypto: []
    };
    
    // Search for any crypto-like global
    for (var key in window) {
        var keyLower = key.toLowerCase();
        if (keyLower.indexOf('crypto') !== -1 ||
            keyLower.indexOf('aes') !== -1 ||
            keyLower.indexOf('encrypt') !== -1) {
            try {
                var val = window[key];
                if (val && typeof val === 'object') {
                    result.otherCrypto.push({
                        name: key,
                        type: typeof val,
                        keys: Object.keys(val).slice(0, 10)
                    });
                }
            } catch(e) {}
        }
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const cryptoLib = await evaluate(cryptoLibScript);
    console.log('Crypto Libraries:', cryptoLib);
    
    // Method 5: Check the group member list component directly
    console.log('\n=== Checking Group Member List Component ===');
    const memberListScript = `
(function() {
    var result = { found: false, component: null, data: [] };
    
    // Find the member list in sidebar
    var memberList = document.querySelector('.member-list, .team-member-list, [class*="GroupMember"]');
    if (!memberList) {
        // Try to find by class pattern
        var allElements = document.querySelectorAll('*');
        for (var i = 0; i < allElements.length; i++) {
            if (allElements[i].className && 
                typeof allElements[i].className === 'string' &&
                (allElements[i].className.indexOf('member') !== -1 || 
                 allElements[i].className.indexOf('Member') !== -1)) {
                memberList = allElements[i];
                break;
            }
        }
    }
    
    if (!memberList) {
        return JSON.stringify({error: 'Member list not found'});
    }
    
    result.found = true;
    result.className = memberList.className;
    
    // Get text content of member names
    var nameElements = memberList.querySelectorAll('[class*="name"], [class*="nick"], span');
    result.names = Array.from(nameElements).slice(0, 10).map(function(el) {
        return {
            text: el.textContent.trim().substring(0, 30),
            className: el.className
        };
    }).filter(function(n) { return n.text.length > 0; });
    
    // Check Vue binding
    if (memberList.__vue__) {
        var v = memberList.__vue__;
        result.vueComponent = {
            name: v.$options.name || 'unknown',
            dataKeys: v.$data ? Object.keys(v.$data) : [],
            propsKeys: v.$props ? Object.keys(v.$props) : []
        };
        
        // Try to get member data
        if (v.members || v.memberList || v.teamMembers) {
            var members = v.members || v.memberList || v.teamMembers;
            result.memberData = Array.isArray(members) ? 
                members.slice(0, 3).map(function(m) {
                    return {
                        account: m.account,
                        nick: m.nick,
                        displayName: m.displayName,
                        nickInTeam: m.nickInTeam
                    };
                }) : 'not an array';
        }
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const memberList = await evaluate(memberListScript);
    console.log('Member List Component:', memberList);
    
    // Method 6: Search webpack modules for decrypt
    console.log('\n=== Searching Webpack Modules ===');
    const webpackScript = `
(function() {
    var result = { modules: [], decryptFuncs: [] };
    
    // Try to access webpack modules
    var webpackJsonp = window.webpackJsonp || window.__webpack_modules__;
    
    if (!webpackJsonp) {
        // Try other webpack globals
        for (var key in window) {
            if (key.indexOf('webpack') !== -1 || key.indexOf('__webpack') !== -1) {
                result.modules.push(key);
            }
        }
    }
    
    // Search for require function
    if (typeof require === 'function') {
        result.hasRequire = true;
    }
    
    // Check for bundled decrypt functions in any global
    var searchTerms = ['decryptNickname', 'decryptName', 'aesDecrypt', 'decrypt'];
    
    function searchForFunc(obj, path, depth) {
        if (depth > 2 || !obj) return;
        
        try {
            var keys = Object.keys(obj);
            for (var i = 0; i < keys.length; i++) {
                var key = keys[i];
                var keyLower = key.toLowerCase();
                
                for (var j = 0; j < searchTerms.length; j++) {
                    if (keyLower.indexOf(searchTerms[j].toLowerCase()) !== -1) {
                        result.decryptFuncs.push({
                            path: path + '.' + key,
                            type: typeof obj[key]
                        });
                    }
                }
            }
        } catch(e) {}
    }
    
    searchForFunc(window, 'window', 0);
    
    return JSON.stringify(result, null, 2);
})()`;

    const webpack = await evaluate(webpackScript);
    console.log('Webpack Modules:', webpack);
    
    ws.close();
    console.log('\nExploration complete!');
}

explore().catch(console.error);

