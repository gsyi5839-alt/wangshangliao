// WangShangLiao Window Object Explorer
// Run with: node explore_window.js

const http = require('http');
const WebSocket = require('ws');

const DEBUG_PORT = 9222;

async function getTargets() {
    return new Promise((resolve, reject) => {
        http.get(`http://localhost:${DEBUG_PORT}/json`, (res) => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', () => resolve(JSON.parse(data)));
        }).on('error', reject);
    });
}

async function connectWS(wsUrl) {
    return new Promise((resolve, reject) => {
        const ws = new WebSocket(wsUrl);
        ws.on('open', () => resolve(ws));
        ws.on('error', reject);
    });
}

async function sendCDP(ws, method, params = {}) {
    return new Promise((resolve, reject) => {
        const id = Math.floor(Math.random() * 100000);
        
        const handler = (data) => {
            const msg = JSON.parse(data);
            if (msg.id === id) {
                ws.removeListener('message', handler);
                resolve(msg);
            }
        };
        
        ws.on('message', handler);
        ws.send(JSON.stringify({ id, method, params }));
        
        setTimeout(() => {
            ws.removeListener('message', handler);
            reject(new Error('Timeout'));
        }, 30000);
    });
}

async function evaluate(ws, expression, awaitPromise = false) {
    const result = await sendCDP(ws, 'Runtime.evaluate', {
        expression,
        returnByValue: true,
        awaitPromise
    });
    
    if (result.result && result.result.result) {
        const val = result.result.result.value;
        if (typeof val === 'string') {
            try {
                return JSON.parse(val);
            } catch {
                return val;
            }
        }
        return val;
    }
    return null;
}

async function main() {
    console.log('=== WangShangLiao Window Explorer ===');
    console.log('Time:', new Date().toISOString());
    
    const targets = await getTargets();
    const target = targets.find(t => t.type === 'page');
    if (!target) {
        console.error('No page target found');
        process.exit(1);
    }
    
    console.log('Target:', target.title);
    console.log('URL:', target.url);
    
    const ws = await connectWS(target.webSocketDebuggerUrl);
    console.log('WebSocket connected!');
    
    // ============================================
    // PART 1: List all window properties
    // ============================================
    console.log('\n=== PART 1: Window Properties ===');
    
    const windowProps = await evaluate(ws, `
        (function() {
            var result = {
                customProps: [],
                vueInstances: [],
                nimProps: [],
                storeProps: [],
                appProps: []
            };
            
            // Get all non-standard window properties
            var standardProps = ['document', 'location', 'navigator', 'history', 'screen', 'console', 
                'alert', 'confirm', 'prompt', 'setTimeout', 'setInterval', 'clearTimeout', 'clearInterval',
                'fetch', 'XMLHttpRequest', 'WebSocket', 'localStorage', 'sessionStorage', 'indexedDB',
                'performance', 'crypto', 'Blob', 'File', 'FileReader', 'FormData', 'URL', 'URLSearchParams',
                'Image', 'Audio', 'Video', 'Canvas', 'JSON', 'Math', 'Date', 'RegExp', 'Error',
                'Array', 'Object', 'String', 'Number', 'Boolean', 'Function', 'Symbol', 'Map', 'Set',
                'Promise', 'Proxy', 'Reflect', 'WeakMap', 'WeakSet', 'ArrayBuffer', 'DataView',
                'Int8Array', 'Uint8Array', 'Uint8ClampedArray', 'Int16Array', 'Uint16Array',
                'Int32Array', 'Uint32Array', 'Float32Array', 'Float64Array', 'BigInt64Array', 'BigUint64Array',
                'window', 'self', 'parent', 'top', 'frames', 'length', 'name', 'closed', 'opener',
                'innerWidth', 'innerHeight', 'outerWidth', 'outerHeight', 'screenX', 'screenY',
                'pageXOffset', 'pageYOffset', 'scrollX', 'scrollY', 'devicePixelRatio',
                'getComputedStyle', 'matchMedia', 'requestAnimationFrame', 'cancelAnimationFrame',
                'postMessage', 'close', 'focus', 'blur', 'print', 'stop', 'open', 'scroll', 'scrollTo', 'scrollBy',
                'moveTo', 'moveBy', 'resizeTo', 'resizeBy', 'getSelection', 'find', 'atob', 'btoa',
                'eval', 'parseInt', 'parseFloat', 'isNaN', 'isFinite', 'decodeURI', 'decodeURIComponent',
                'encodeURI', 'encodeURIComponent', 'escape', 'unescape', 'undefined', 'NaN', 'Infinity',
                'Event', 'CustomEvent', 'EventTarget', 'Node', 'Element', 'HTMLElement', 'Document',
                'DocumentFragment', 'Text', 'Comment', 'DOMParser', 'MutationObserver', 'ResizeObserver',
                'IntersectionObserver', 'Worker', 'SharedWorker', 'ServiceWorker', 'BroadcastChannel',
                'MessageChannel', 'MessagePort', 'Notification', 'PushManager', 'Cache', 'CacheStorage',
                'Headers', 'Request', 'Response', 'ReadableStream', 'WritableStream', 'TransformStream',
                'TextEncoder', 'TextDecoder', 'Intl', 'BigInt', 'queueMicrotask', 'reportError',
                'structuredClone', 'createImageBitmap', 'origin', 'isSecureContext', 'crossOriginIsolated',
                'caches', 'clientInformation', 'customElements', 'external', 'frameElement',
                'locationbar', 'menubar', 'personalbar', 'scrollbars', 'statusbar', 'toolbar', 'status',
                'defaultStatus', 'onbeforeunload', 'onhashchange', 'onlanguagechange', 'onmessage',
                'onoffline', 'ononline', 'onpagehide', 'onpageshow', 'onpopstate', 'onrejectionhandled',
                'onstorage', 'onunhandledrejection', 'onunload', 'visualViewport', 'speechSynthesis'];
            
            var allProps = Object.keys(window);
            
            allProps.forEach(function(prop) {
                if (standardProps.indexOf(prop) === -1 && !prop.startsWith('webkit') && !prop.startsWith('on')) {
                    try {
                        var val = window[prop];
                        var type = typeof val;
                        var info = {
                            name: prop,
                            type: type
                        };
                        
                        if (type === 'object' && val !== null) {
                            info.keys = Object.keys(val).slice(0, 20);
                            if (val.constructor && val.constructor.name) {
                                info.constructorName = val.constructor.name;
                            }
                        } else if (type === 'function') {
                            info.funcName = val.name || 'anonymous';
                        }
                        
                        // Categorize
                        var lowerProp = prop.toLowerCase();
                        if (lowerProp.includes('vue') || lowerProp.includes('vuex')) {
                            result.vueInstances.push(info);
                        } else if (lowerProp.includes('nim') || lowerProp.includes('netease')) {
                            result.nimProps.push(info);
                        } else if (lowerProp.includes('store') || lowerProp.includes('state')) {
                            result.storeProps.push(info);
                        } else if (lowerProp.includes('app') || lowerProp === '$') {
                            result.appProps.push(info);
                        } else {
                            result.customProps.push(info);
                        }
                    } catch (e) {}
                }
            });
            
            return JSON.stringify(result);
        })();
    `);
    
    console.log('\nVue-related:', JSON.stringify(windowProps?.vueInstances, null, 2));
    console.log('\nNIM-related:', JSON.stringify(windowProps?.nimProps, null, 2));
    console.log('\nStore-related:', JSON.stringify(windowProps?.storeProps, null, 2));
    console.log('\nApp-related:', JSON.stringify(windowProps?.appProps, null, 2));
    console.log('\nOther custom props:', windowProps?.customProps?.map(p => p.name + ' (' + p.type + ')').join(', '));
    
    // ============================================
    // PART 2: Explore NIM object in detail
    // ============================================
    console.log('\n=== PART 2: NIM Object Detail ===');
    
    const nimDetail = await evaluate(ws, `
        (function() {
            var result = {
                exists: false,
                type: null,
                keys: [],
                methods: [],
                properties: []
            };
            
            if (!window.nim) return JSON.stringify(result);
            
            result.exists = true;
            result.type = typeof window.nim;
            
            var allKeys = [];
            for (var key in window.nim) {
                allKeys.push(key);
            }
            result.keys = allKeys;
            
            allKeys.forEach(function(k) {
                try {
                    var val = window.nim[k];
                    if (typeof val === 'function') {
                        result.methods.push(k);
                    } else {
                        result.properties.push({
                            name: k,
                            type: typeof val,
                            value: typeof val === 'string' || typeof val === 'number' || typeof val === 'boolean' 
                                ? val : (Array.isArray(val) ? '[array:' + val.length + ']' : '{object}')
                        });
                    }
                } catch (e) {}
            });
            
            return JSON.stringify(result);
        })();
    `);
    
    console.log('NIM exists:', nimDetail?.exists);
    console.log('NIM type:', nimDetail?.type);
    console.log('NIM keys count:', nimDetail?.keys?.length);
    console.log('\nNIM methods:', nimDetail?.methods?.join(', '));
    console.log('\nNIM properties:', JSON.stringify(nimDetail?.properties, null, 2));
    
    // ============================================
    // PART 3: Look for Vue instance in DOM
    // ============================================
    console.log('\n=== PART 3: Vue Instance Search ===');
    
    const vueSearch = await evaluate(ws, `
        (function() {
            var result = {
                appElement: null,
                hasVue: false,
                vueVersion: null,
                storeState: null,
                rootData: null,
                allElements: []
            };
            
            // Check #app element
            var app = document.querySelector('#app');
            if (app) {
                result.appElement = {
                    id: app.id,
                    className: app.className,
                    tagName: app.tagName
                };
                
                // Check for Vue 2
                if (app.__vue__) {
                    result.hasVue = true;
                    result.vueVersion = '2.x';
                    var vue = app.__vue__;
                    
                    if (vue.$store) {
                        result.storeState = Object.keys(vue.$store.state || {});
                    }
                    if (vue.$data) {
                        result.rootData = Object.keys(vue.$data);
                    }
                }
                
                // Check for Vue 3
                if (app._vnode || app.__vue_app__) {
                    result.hasVue = true;
                    result.vueVersion = '3.x';
                }
            }
            
            // Look for Vue in other elements
            var allElements = document.querySelectorAll('*');
            for (var i = 0; i < Math.min(allElements.length, 100); i++) {
                var el = allElements[i];
                if (el.__vue__ || el._vnode || el.__vue_app__) {
                    result.allElements.push({
                        tagName: el.tagName,
                        id: el.id,
                        className: el.className?.substring?.(0, 50)
                    });
                }
            }
            
            return JSON.stringify(result);
        })();
    `);
    
    console.log('App element:', JSON.stringify(vueSearch?.appElement));
    console.log('Has Vue:', vueSearch?.hasVue);
    console.log('Vue version:', vueSearch?.vueVersion);
    console.log('Store state keys:', vueSearch?.storeState);
    console.log('Root data keys:', vueSearch?.rootData);
    console.log('Elements with Vue:', JSON.stringify(vueSearch?.allElements, null, 2));
    
    // ============================================
    // PART 4: Look for any global stores
    // ============================================
    console.log('\n=== PART 4: Global Stores Search ===');
    
    const storeSearch = await evaluate(ws, `
        (function() {
            var result = {
                vuex: null,
                redux: null,
                mobx: null,
                pinia: null,
                customStores: []
            };
            
            // Check Vuex
            if (window.Vuex) {
                result.vuex = {
                    exists: true,
                    type: typeof window.Vuex
                };
            }
            
            // Check Redux
            if (window.__REDUX_DEVTOOLS_EXTENSION__) {
                result.redux = { exists: true };
            }
            
            // Look for store-like objects
            var storeKeywords = ['store', 'state', 'dispatch', 'commit', 'getters', 'mutations', 'actions'];
            
            Object.keys(window).forEach(function(key) {
                try {
                    var val = window[key];
                    if (val && typeof val === 'object') {
                        var valKeys = Object.keys(val);
                        var matches = valKeys.filter(function(k) {
                            return storeKeywords.indexOf(k) !== -1;
                        });
                        if (matches.length >= 2) {
                            result.customStores.push({
                                name: key,
                                matchedKeys: matches,
                                allKeys: valKeys.slice(0, 15)
                            });
                        }
                    }
                } catch (e) {}
            });
            
            return JSON.stringify(result);
        })();
    `);
    
    console.log('Vuex:', JSON.stringify(storeSearch?.vuex));
    console.log('Redux:', JSON.stringify(storeSearch?.redux));
    console.log('Custom stores:', JSON.stringify(storeSearch?.customStores, null, 2));
    
    // ============================================
    // PART 5: Explore all global objects with methods
    // ============================================
    console.log('\n=== PART 5: Objects with Team/Member Methods ===');
    
    const teamMethods = await evaluate(ws, `
        (function() {
            var result = [];
            
            var keywords = ['team', 'member', 'group', 'chat', 'message', 'session', 'user', 'contact'];
            
            Object.keys(window).forEach(function(key) {
                try {
                    var val = window[key];
                    if (val && typeof val === 'object') {
                        var methods = [];
                        for (var k in val) {
                            if (typeof val[k] === 'function') {
                                var lowerK = k.toLowerCase();
                                for (var i = 0; i < keywords.length; i++) {
                                    if (lowerK.includes(keywords[i])) {
                                        methods.push(k);
                                        break;
                                    }
                                }
                            }
                        }
                        if (methods.length > 0) {
                            result.push({
                                objectName: key,
                                relevantMethods: methods
                            });
                        }
                    }
                } catch (e) {}
            });
            
            return JSON.stringify(result);
        })();
    `);
    
    console.log('Objects with relevant methods:');
    for (const obj of (teamMethods || [])) {
        console.log(`\n  ${obj.objectName}:`);
        console.log(`    Methods: ${obj.relevantMethods.join(', ')}`);
    }
    
    // ============================================
    // PART 6: Try to find data in document
    // ============================================
    console.log('\n=== PART 6: Document Data Attributes ===');
    
    const docData = await evaluate(ws, `
        (function() {
            var result = {
                dataAttributes: [],
                scripts: []
            };
            
            // Find elements with data attributes
            var elements = document.querySelectorAll('[data-v-]');
            result.dataAttributes.push('Elements with data-v-*: ' + elements.length);
            
            // Look for script tags with inline data
            var scripts = document.querySelectorAll('script');
            scripts.forEach(function(s) {
                if (s.textContent && s.textContent.includes('__INITIAL_STATE__')) {
                    result.scripts.push('Found __INITIAL_STATE__ script');
                }
                if (s.textContent && s.textContent.includes('window.')) {
                    var matches = s.textContent.match(/window\\.([a-zA-Z_][a-zA-Z0-9_]*)/g);
                    if (matches) {
                        result.scripts.push('Window assignments: ' + matches.slice(0, 10).join(', '));
                    }
                }
            });
            
            return JSON.stringify(result);
        })();
    `);
    
    console.log('Data attributes:', docData?.dataAttributes);
    console.log('Scripts:', docData?.scripts);
    
    // ============================================
    // PART 7: Get actual NIM instance and its state
    // ============================================
    console.log('\n=== PART 7: NIM Instance State ===');
    
    const nimState = await evaluate(ws, `
        (function() {
            var result = {
                found: false,
                instanceKeys: [],
                account: null,
                teams: null,
                options: null
            };
            
            // Try to find nim instance
            var nim = window.nim || window.NIM || window._nim;
            if (!nim) {
                // Try to find in global scope
                Object.keys(window).forEach(function(k) {
                    try {
                        var val = window[k];
                        if (val && typeof val === 'object' && val.getTeamMembers && typeof val.getTeamMembers === 'function') {
                            nim = val;
                        }
                    } catch(e) {}
                });
            }
            
            if (!nim) return JSON.stringify(result);
            
            result.found = true;
            
            // Get all keys using for-in to include prototype chain
            var keys = [];
            for (var k in nim) {
                keys.push(k);
            }
            result.instanceKeys = keys;
            
            // Try to get account info
            if (nim.account) result.account = nim.account;
            if (nim.options) {
                result.options = {};
                Object.keys(nim.options).forEach(function(k) {
                    var val = nim.options[k];
                    if (typeof val === 'string' || typeof val === 'number' || typeof val === 'boolean') {
                        result.options[k] = val;
                    }
                });
            }
            
            return JSON.stringify(result);
        })();
    `);
    
    console.log('NIM Found:', nimState?.found);
    console.log('Instance keys count:', nimState?.instanceKeys?.length);
    console.log('Instance keys:', nimState?.instanceKeys?.join(', '));
    console.log('Account:', nimState?.account);
    console.log('Options:', JSON.stringify(nimState?.options, null, 2));
    
    // ============================================
    // PART 8: Try calling getTeamMembers directly
    // ============================================
    console.log('\n=== PART 8: Direct NIM API Call ===');
    
    // First, let's find the team ID from URL
    const urlMatch = target.url.match(/sessionId=team-(\d+)/);
    const teamId = urlMatch ? urlMatch[1] : null;
    console.log('Team ID from URL:', teamId);
    
    if (teamId) {
        const directCall = await evaluate(ws, `
            (async function() {
                var result = {
                    success: false,
                    error: null,
                    memberCount: 0,
                    members: []
                };
                
                if (!window.nim) {
                    result.error = 'nim not found';
                    return JSON.stringify(result);
                }
                
                if (typeof window.nim.getTeamMembers !== 'function') {
                    // Try to list all functions
                    var funcs = [];
                    for (var k in window.nim) {
                        if (typeof window.nim[k] === 'function') {
                            funcs.push(k);
                        }
                    }
                    result.error = 'getTeamMembers not found. Available functions: ' + funcs.join(', ');
                    return JSON.stringify(result);
                }
                
                try {
                    var members = await new Promise(function(resolve, reject) {
                        window.nim.getTeamMembers({
                            teamId: '${teamId}',
                            done: function(err, obj) {
                                if (err) {
                                    reject(err);
                                } else {
                                    resolve(obj);
                                }
                            }
                        });
                        setTimeout(function() { reject(new Error('Timeout')); }, 15000);
                    });
                    
                    result.success = true;
                    if (members.members) {
                        result.memberCount = members.members.length;
                        result.members = members.members.slice(0, 5).map(function(m) {
                            return {
                                id: m.id,
                                account: m.account,
                                nick: m.nick || m.nickInTeam,
                                type: m.type,
                                fields: Object.keys(m)
                            };
                        });
                    } else if (Array.isArray(members)) {
                        result.memberCount = members.length;
                        result.members = members.slice(0, 5).map(function(m) {
                            return {
                                id: m.id,
                                account: m.account,
                                nick: m.nick || m.nickInTeam,
                                type: m.type,
                                fields: Object.keys(m)
                            };
                        });
                    }
                } catch (e) {
                    result.error = e.message || String(e);
                }
                
                return JSON.stringify(result);
            })();
        `, true);
        
        console.log('Direct call result:', JSON.stringify(directCall, null, 2));
    }
    
    ws.close();
    console.log('\n=== Exploration Complete ===');
}

main().catch(console.error);

