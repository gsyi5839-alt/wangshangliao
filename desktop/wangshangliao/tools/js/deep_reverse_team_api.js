/**
 * 深度逆向解析旺商聊群聊API
 * 全面提取所有群聊相关的API、事件、数据结构
 */
const WebSocket = require('ws');

const WS_URL = 'ws://127.0.0.1:9222/devtools/page/8322EEB7A02952E8C4C59B59B616C299';

let messageId = 1;
let ws;

function send(expression) {
    return new Promise((resolve, reject) => {
        const id = messageId++;
        const timeout = setTimeout(() => reject(new Error('Timeout')), 30000);
        
        const handler = (data) => {
            const response = JSON.parse(data);
            if (response.id === id) {
                clearTimeout(timeout);
                ws.off('message', handler);
                resolve(response);
            }
        };
        
        ws.on('message', handler);
        ws.send(JSON.stringify({
            id,
            method: 'Runtime.evaluate',
            params: {
                expression,
                returnByValue: true,
                awaitPromise: true
            }
        }));
    });
}

async function deepReverse() {
    ws = new WebSocket(WS_URL);
    
    await new Promise(resolve => ws.on('open', resolve));
    console.log('='.repeat(80));
    console.log('深度逆向解析旺商聊群聊API');
    console.log('='.repeat(80));
    
    // ============================================================
    // 1. 提取所有 nim 对象方法 (完整分类)
    // ============================================================
    console.log('\n【1. NIM SDK 完整方法列表】\n');
    
    const nimMethods = await send(`
        (function() {
            if (!window.nim) return { error: 'nim not found' };
            
            const methods = {};
            const categories = {
                team: [],      // 群组相关
                superTeam: [], // 超级群相关
                msg: [],       // 消息相关
                user: [],      // 用户相关
                friend: [],    // 好友相关
                session: [],   // 会话相关
                sync: [],      // 同步相关
                nos: [],       // 文件相关
                other: []      // 其他
            };
            
            for (let key of Object.keys(window.nim)) {
                if (typeof window.nim[key] === 'function') {
                    const keyLower = key.toLowerCase();
                    
                    if (keyLower.includes('superteam')) {
                        categories.superTeam.push(key);
                    } else if (keyLower.includes('team')) {
                        categories.team.push(key);
                    } else if (keyLower.includes('msg') || keyLower.includes('message') || keyLower.includes('send') || keyLower.includes('recall')) {
                        categories.msg.push(key);
                    } else if (keyLower.includes('user') || keyLower.includes('account') || keyLower.includes('nick')) {
                        categories.user.push(key);
                    } else if (keyLower.includes('friend') || keyLower.includes('relation')) {
                        categories.friend.push(key);
                    } else if (keyLower.includes('session')) {
                        categories.session.push(key);
                    } else if (keyLower.includes('sync') || keyLower.includes('roam')) {
                        categories.sync.push(key);
                    } else if (keyLower.includes('file') || keyLower.includes('upload') || keyLower.includes('download') || keyLower.includes('nos') || keyLower.includes('preview')) {
                        categories.nos.push(key);
                    } else {
                        categories.other.push(key);
                    }
                }
            }
            
            return categories;
        })()
    `);
    
    if (nimMethods.result?.result?.value) {
        const cats = nimMethods.result.result.value;
        console.log('【群组API (Team)】');
        cats.team?.forEach(m => console.log(`  - ${m}`));
        console.log('\n【超级群API (SuperTeam)】');
        cats.superTeam?.forEach(m => console.log(`  - ${m}`));
        console.log('\n【消息API】');
        cats.msg?.forEach(m => console.log(`  - ${m}`));
        console.log('\n【用户API】');
        cats.user?.forEach(m => console.log(`  - ${m}`));
        console.log('\n【会话API】');
        cats.session?.forEach(m => console.log(`  - ${m}`));
        console.log('\n【文件API】');
        cats.nos?.forEach(m => console.log(`  - ${m}`));
    }

    // ============================================================
    // 2. 深度提取每个群组API的参数签名
    // ============================================================
    console.log('\n' + '='.repeat(80));
    console.log('【2. 群组API详细参数签名】');
    console.log('='.repeat(80) + '\n');
    
    const teamApiDetails = await send(`
        (function() {
            if (!window.nim) return { error: 'nim not found' };
            
            const teamMethods = [
                'createTeam', 'updateTeam', 'getTeam', 'getTeams', 'getTeamMembers',
                'getLocalTeams', 'getLocalTeamMembers', 'getMutedTeamMembers',
                'addTeamMembers', 'removeTeamMembers', 'updateTeamMember',
                'addTeamManagers', 'removeTeamManagers', 'transferTeam',
                'acceptTeamInvite', 'rejectTeamInvite', 'passTeamApply', 'rejectTeamApply',
                'applyTeam', 'leaveTeam', 'dismissTeam',
                'muteTeamAll', 'updateMuteStateInTeam', 'updateNickInTeam',
                'updateInfoInTeam', 'notifyForNewTeamMsg',
                'getTeamMsgReads', 'sendTeamMsgReceipt', 'teamMsgReceipts',
                'getTeamMemberByTeamIdAndAccounts', 'subscribeTeamMembers'
            ];
            
            const details = {};
            
            teamMethods.forEach(method => {
                if (window.nim[method]) {
                    const fnStr = window.nim[method].toString();
                    // 提取参数
                    const match = fnStr.match(/^function\\s*\\([^)]*\\)|^\\([^)]*\\)\\s*=>|^[^(]*\\(([^)]*)\\)/);
                    details[method] = {
                        exists: true,
                        preview: fnStr.substring(0, 200)
                    };
                } else {
                    details[method] = { exists: false };
                }
            });
            
            return details;
        })()
    `);
    
    if (teamApiDetails.result?.result?.value) {
        const details = teamApiDetails.result.result.value;
        for (let [method, info] of Object.entries(details)) {
            if (info.exists) {
                console.log(`✅ ${method}`);
                // console.log(`   ${info.preview?.substring(0, 100)}...`);
            } else {
                console.log(`❌ ${method} (不存在)`);
            }
        }
    }

    // ============================================================
    // 3. 提取nim.options中的所有事件回调
    // ============================================================
    console.log('\n' + '='.repeat(80));
    console.log('【3. NIM事件回调 (nim.options)】');
    console.log('='.repeat(80) + '\n');
    
    const nimOptions = await send(`
        (function() {
            if (!window.nim || !window.nim.options) return { error: 'nim.options not found' };
            
            const options = window.nim.options;
            const callbacks = {};
            
            for (let key of Object.keys(options)) {
                if (key.startsWith('on') || key.includes('Callback') || key.includes('Handler')) {
                    const val = options[key];
                    callbacks[key] = {
                        type: typeof val,
                        isFunction: typeof val === 'function',
                        preview: typeof val === 'function' ? val.toString().substring(0, 150) : String(val)?.substring(0, 100)
                    };
                }
            }
            
            return callbacks;
        })()
    `);
    
    if (nimOptions.result?.result?.value) {
        const callbacks = nimOptions.result.result.value;
        
        // 群组相关事件
        console.log('【群组相关事件】');
        for (let [key, info] of Object.entries(callbacks)) {
            if (key.toLowerCase().includes('team')) {
                console.log(`  ${key}: ${info.type} ${info.isFunction ? '✓' : ''}`);
            }
        }
        
        // 消息相关事件
        console.log('\n【消息相关事件】');
        for (let [key, info] of Object.entries(callbacks)) {
            if (key.toLowerCase().includes('msg') || key.toLowerCase().includes('message')) {
                console.log(`  ${key}: ${info.type} ${info.isFunction ? '✓' : ''}`);
            }
        }
        
        // 系统相关事件
        console.log('\n【系统相关事件】');
        for (let [key, info] of Object.entries(callbacks)) {
            if (key.toLowerCase().includes('sys') || key.toLowerCase().includes('sync') || key.toLowerCase().includes('connect')) {
                console.log(`  ${key}: ${info.type} ${info.isFunction ? '✓' : ''}`);
            }
        }
        
        // 所有on开头的事件
        console.log('\n【所有事件回调列表】');
        for (let [key, info] of Object.entries(callbacks)) {
            console.log(`  ${key}`);
        }
    }

    // ============================================================
    // 4. 提取当前群聊的完整数据结构
    // ============================================================
    console.log('\n' + '='.repeat(80));
    console.log('【4. 当前群聊完整数据结构】');
    console.log('='.repeat(80) + '\n');
    
    const teamData = await send(`
        (function() {
            const result = {
                currentSession: null,
                teamInfo: null,
                teamMembers: null,
                pinia: {}
            };
            
            // 获取当前会话
            const app = document.querySelector('#app');
            if (app && app.__vue_app__) {
                const pinia = app.__vue_app__._context?.provides?.pinia || 
                             app.__vue_app__.config?.globalProperties?.$pinia;
                if (pinia) {
                    // appStore
                    if (pinia.state?.value?.app) {
                        const appState = pinia.state.value.app;
                        result.currentSession = appState.currentSession || appState.currSession;
                        result.pinia.userInfo = appState.userInfo;
                        result.pinia.groupList = appState.groupList?.slice(0, 3);
                    }
                    
                    // sdkStore
                    if (pinia.state?.value?.sdk) {
                        result.pinia.sdkConnected = pinia.state.value.sdk.connected;
                    }
                    
                    // cacheStore
                    if (pinia.state?.value?.cache) {
                        const cache = pinia.state.value.cache;
                        result.pinia.cacheKeys = Object.keys(cache).slice(0, 20);
                    }
                }
            }
            
            return result;
        })()
    `);
    
    if (teamData.result?.result?.value) {
        console.log('当前会话:', JSON.stringify(teamData.result.result.value.currentSession, null, 2));
        console.log('\nPinia状态:', JSON.stringify(teamData.result.result.value.pinia, null, 2));
    }

    // ============================================================
    // 5. 获取当前群的详细信息
    // ============================================================
    console.log('\n' + '='.repeat(80));
    console.log('【5. 获取群详细信息 (getTeam)】');
    console.log('='.repeat(80) + '\n');
    
    const currentTeamInfo = await send(`
        (function() {
            return new Promise((resolve) => {
                // 先获取当前teamId
                const app = document.querySelector('#app');
                let teamId = null;
                
                if (app && app.__vue_app__) {
                    const pinia = app.__vue_app__._context?.provides?.pinia;
                    if (pinia?.state?.value?.app?.currentSession) {
                        teamId = pinia.state.value.app.currentSession.to || 
                                pinia.state.value.app.currentSession.id;
                    }
                }
                
                if (!teamId) {
                    resolve({ error: 'No teamId found' });
                    return;
                }
                
                if (!window.nim || !window.nim.getTeam) {
                    resolve({ error: 'nim.getTeam not found' });
                    return;
                }
                
                window.nim.getTeam({
                    teamId: teamId,
                    done: (err, team) => {
                        if (err) {
                            resolve({ error: err.message || String(err) });
                        } else {
                            // 提取所有字段
                            const fields = {};
                            for (let key in team) {
                                fields[key] = team[key];
                            }
                            resolve({ teamId, fields });
                        }
                    }
                });
            });
        })()
    `);
    
    if (currentTeamInfo.result?.result?.value) {
        const info = currentTeamInfo.result.result.value;
        if (info.fields) {
            console.log('群ID:', info.teamId);
            console.log('群信息字段:');
            for (let [key, value] of Object.entries(info.fields)) {
                const valueStr = typeof value === 'object' ? JSON.stringify(value) : String(value);
                console.log(`  ${key}: ${valueStr.substring(0, 100)}${valueStr.length > 100 ? '...' : ''}`);
            }
        } else {
            console.log('获取失败:', info.error);
        }
    }

    // ============================================================
    // 6. 获取群成员详细信息
    // ============================================================
    console.log('\n' + '='.repeat(80));
    console.log('【6. 群成员详细信息 (getTeamMembers)】');
    console.log('='.repeat(80) + '\n');
    
    const membersInfo = await send(`
        (function() {
            return new Promise((resolve) => {
                const app = document.querySelector('#app');
                let teamId = null;
                
                if (app && app.__vue_app__) {
                    const pinia = app.__vue_app__._context?.provides?.pinia;
                    if (pinia?.state?.value?.app?.currentSession) {
                        teamId = pinia.state.value.app.currentSession.to;
                    }
                }
                
                if (!teamId || !window.nim?.getTeamMembers) {
                    resolve({ error: 'Cannot get members' });
                    return;
                }
                
                window.nim.getTeamMembers({
                    teamId: teamId,
                    done: (err, result) => {
                        if (err) {
                            resolve({ error: err.message });
                            return;
                        }
                        
                        const members = result.members || result;
                        const sample = members.slice(0, 3).map(m => {
                            const fields = {};
                            for (let key in m) {
                                fields[key] = m[key];
                            }
                            return fields;
                        });
                        
                        // 提取成员可用字段
                        const memberFields = members[0] ? Object.keys(members[0]) : [];
                        
                        resolve({
                            teamId,
                            totalCount: members.length,
                            memberFields,
                            sample
                        });
                    }
                });
            });
        })()
    `);
    
    if (membersInfo.result?.result?.value) {
        const info = membersInfo.result.result.value;
        console.log('群ID:', info.teamId);
        console.log('成员总数:', info.totalCount);
        console.log('成员字段:', info.memberFields?.join(', '));
        console.log('\n样本成员:');
        info.sample?.forEach((m, i) => {
            console.log(`  成员${i+1}:`, JSON.stringify(m, null, 2).substring(0, 500));
        });
    }

    // ============================================================
    // 7. 探索所有网络请求相关的API
    // ============================================================
    console.log('\n' + '='.repeat(80));
    console.log('【7. 网络请求Hook】');
    console.log('='.repeat(80) + '\n');
    
    const networkHook = await send(`
        (function() {
            // 保存原始fetch
            const originalFetch = window.fetch;
            const requests = [];
            
            // Hook fetch
            window.__teamApiRequests = requests;
            window.fetch = function(...args) {
                const url = args[0]?.url || args[0];
                if (typeof url === 'string' && (url.includes('team') || url.includes('group') || url.includes('nim'))) {
                    requests.push({
                        type: 'fetch',
                        url: url,
                        time: new Date().toISOString()
                    });
                }
                return originalFetch.apply(this, args);
            };
            
            // Hook XMLHttpRequest
            const originalXHR = window.XMLHttpRequest;
            window.XMLHttpRequest = function() {
                const xhr = new originalXHR();
                const originalOpen = xhr.open;
                xhr.open = function(method, url, ...rest) {
                    if (typeof url === 'string' && (url.includes('team') || url.includes('group') || url.includes('nim'))) {
                        requests.push({
                            type: 'xhr',
                            method,
                            url,
                            time: new Date().toISOString()
                        });
                    }
                    return originalOpen.apply(this, [method, url, ...rest]);
                };
                return xhr;
            };
            
            return { hooked: true, message: '网络请求已Hook，执行群操作后可查看请求' };
        })()
    `);
    
    console.log('网络Hook状态:', networkHook.result?.result?.value);

    // ============================================================
    // 8. 深度探索nim内部结构
    // ============================================================
    console.log('\n' + '='.repeat(80));
    console.log('【8. NIM内部结构深度探索】');
    console.log('='.repeat(80) + '\n');
    
    const nimInternal = await send(`
        (function() {
            if (!window.nim) return { error: 'nim not found' };
            
            const result = {
                topLevelKeys: [],
                prototypeMethods: [],
                internalObjects: {},
                config: {}
            };
            
            // 顶层键
            result.topLevelKeys = Object.keys(window.nim).slice(0, 50);
            
            // 原型方法
            const proto = Object.getPrototypeOf(window.nim);
            if (proto) {
                result.prototypeMethods = Object.getOwnPropertyNames(proto).filter(k => k !== 'constructor');
            }
            
            // 内部对象
            for (let key of Object.keys(window.nim)) {
                const val = window.nim[key];
                if (val && typeof val === 'object' && !Array.isArray(val)) {
                    result.internalObjects[key] = {
                        type: val.constructor?.name || 'Object',
                        keys: Object.keys(val).slice(0, 10)
                    };
                }
            }
            
            // nim.options 配置
            if (window.nim.options) {
                const opts = window.nim.options;
                for (let key of Object.keys(opts)) {
                    if (typeof opts[key] !== 'function') {
                        result.config[key] = typeof opts[key] === 'object' 
                            ? JSON.stringify(opts[key])?.substring(0, 100)
                            : String(opts[key])?.substring(0, 50);
                    }
                }
            }
            
            return result;
        })()
    `);
    
    if (nimInternal.result?.result?.value) {
        const internal = nimInternal.result.result.value;
        console.log('顶层键:', internal.topLevelKeys?.join(', '));
        console.log('\n内部对象:');
        for (let [key, info] of Object.entries(internal.internalObjects || {})) {
            console.log(`  ${key} (${info.type}): ${info.keys?.join(', ')}`);
        }
        console.log('\n配置项:');
        for (let [key, val] of Object.entries(internal.config || {})) {
            console.log(`  ${key}: ${val}`);
        }
    }

    // ============================================================
    // 9. 测试每个群组API的实际调用
    // ============================================================
    console.log('\n' + '='.repeat(80));
    console.log('【9. 群组API实际调用测试】');
    console.log('='.repeat(80) + '\n');
    
    const apiTests = await send(`
        (function() {
            return new Promise(async (resolve) => {
                const results = {};
                
                // 获取当前teamId
                const app = document.querySelector('#app');
                let teamId = null;
                if (app && app.__vue_app__) {
                    const pinia = app.__vue_app__._context?.provides?.pinia;
                    if (pinia?.state?.value?.app?.currentSession) {
                        teamId = pinia.state.value.app.currentSession.to;
                    }
                }
                
                if (!teamId) {
                    resolve({ error: 'No teamId', results });
                    return;
                }
                
                results.teamId = teamId;
                
                // 测试 getTeam
                try {
                    const team = await new Promise((res, rej) => {
                        window.nim.getTeam({
                            teamId,
                            done: (e, t) => e ? rej(e) : res(t)
                        });
                    });
                    results.getTeam = { success: true, fields: Object.keys(team) };
                } catch(e) {
                    results.getTeam = { success: false, error: e.message };
                }
                
                // 测试 getTeams
                try {
                    const teams = await new Promise((res, rej) => {
                        window.nim.getTeams({
                            done: (e, t) => e ? rej(e) : res(t)
                        });
                    });
                    results.getTeams = { success: true, count: teams?.length };
                } catch(e) {
                    results.getTeams = { success: false, error: e.message };
                }
                
                // 测试 getTeamMembers
                try {
                    const members = await new Promise((res, rej) => {
                        window.nim.getTeamMembers({
                            teamId,
                            done: (e, r) => e ? rej(e) : res(r)
                        });
                    });
                    const list = members.members || members;
                    results.getTeamMembers = { 
                        success: true, 
                        count: list.length,
                        fields: list[0] ? Object.keys(list[0]) : []
                    };
                } catch(e) {
                    results.getTeamMembers = { success: false, error: e.message };
                }
                
                // 测试 getLocalTeams
                try {
                    const localTeams = await new Promise((res, rej) => {
                        window.nim.getLocalTeams({
                            done: (e, t) => e ? rej(e) : res(t)
                        });
                    });
                    results.getLocalTeams = { success: true, count: localTeams?.length };
                } catch(e) {
                    results.getLocalTeams = { success: false, error: e.message };
                }
                
                // 测试 getLocalTeamMembers
                try {
                    const localMembers = await new Promise((res, rej) => {
                        window.nim.getLocalTeamMembers({
                            teamId,
                            done: (e, r) => e ? rej(e) : res(r)
                        });
                    });
                    results.getLocalTeamMembers = { success: true, count: localMembers?.length };
                } catch(e) {
                    results.getLocalTeamMembers = { success: false, error: e.message };
                }
                
                // 测试 getMutedTeamMembers
                if (window.nim.getMutedTeamMembers) {
                    try {
                        const muted = await new Promise((res, rej) => {
                            window.nim.getMutedTeamMembers({
                                teamId,
                                done: (e, r) => e ? rej(e) : res(r)
                            });
                        });
                        results.getMutedTeamMembers = { success: true, count: muted?.length || 0 };
                    } catch(e) {
                        results.getMutedTeamMembers = { success: false, error: e.message };
                    }
                }
                
                resolve(results);
            });
        })()
    `);
    
    if (apiTests.result?.result?.value) {
        const results = apiTests.result.result.value;
        console.log('测试群ID:', results.teamId);
        for (let [api, result] of Object.entries(results)) {
            if (api !== 'teamId') {
                const status = result.success ? '✅' : '❌';
                console.log(`${status} ${api}:`, JSON.stringify(result));
            }
        }
    }

    // ============================================================
    // 10. 探索Pinia Store中群聊相关的所有方法
    // ============================================================
    console.log('\n' + '='.repeat(80));
    console.log('【10. Pinia Store 群聊相关方法】');
    console.log('='.repeat(80) + '\n');
    
    const piniaStores = await send(`
        (function() {
            const result = {
                appStore: {},
                sdkStore: {},
                cacheStore: {}
            };
            
            const app = document.querySelector('#app');
            if (!app || !app.__vue_app__) return result;
            
            const pinia = app.__vue_app__._context?.provides?.pinia;
            if (!pinia) return result;
            
            // appStore
            if (pinia._s?.has('app')) {
                const store = pinia._s.get('app');
                const actions = [];
                const getters = [];
                const state = {};
                
                for (let key of Object.keys(store)) {
                    if (typeof store[key] === 'function' && !key.startsWith('$') && !key.startsWith('_')) {
                        actions.push(key);
                    } else if (key.includes('group') || key.includes('team') || key.includes('Group') || key.includes('Team') || key.includes('session') || key.includes('Session')) {
                        state[key] = typeof store[key];
                    }
                }
                
                result.appStore = { actions, teamRelatedState: state };
            }
            
            // sdkStore
            if (pinia._s?.has('sdk')) {
                const store = pinia._s.get('sdk');
                const actions = [];
                
                for (let key of Object.keys(store)) {
                    if (typeof store[key] === 'function' && !key.startsWith('$') && !key.startsWith('_')) {
                        actions.push(key);
                    }
                }
                
                result.sdkStore = { actions };
            }
            
            // cacheStore
            if (pinia._s?.has('cache')) {
                const store = pinia._s.get('cache');
                const actions = [];
                
                for (let key of Object.keys(store)) {
                    if (typeof store[key] === 'function' && !key.startsWith('$') && !key.startsWith('_')) {
                        actions.push(key);
                    }
                }
                
                result.cacheStore = { actions };
            }
            
            return result;
        })()
    `);
    
    if (piniaStores.result?.result?.value) {
        const stores = piniaStores.result.result.value;
        
        console.log('【appStore Actions】');
        stores.appStore?.actions?.forEach(a => console.log(`  - ${a}`));
        console.log('\n群聊相关状态:', stores.appStore?.teamRelatedState);
        
        console.log('\n【sdkStore Actions】');
        stores.sdkStore?.actions?.forEach(a => console.log(`  - ${a}`));
        
        console.log('\n【cacheStore Actions】');
        stores.cacheStore?.actions?.forEach(a => console.log(`  - ${a}`));
    }

    // ============================================================
    // 11. 消息类型和格式
    // ============================================================
    console.log('\n' + '='.repeat(80));
    console.log('【11. 消息类型和格式探索】');
    console.log('='.repeat(80) + '\n');
    
    const msgFormats = await send(`
        (function() {
            // 探索消息相关的API和格式
            const msgApis = {};
            
            if (!window.nim) return { error: 'nim not found' };
            
            // 发送相关
            const sendMethods = ['sendText', 'sendFile', 'sendCustomMsg', 'sendTipMsg', 'sendGeo', 'sendRobot'];
            sendMethods.forEach(m => {
                msgApis[m] = typeof window.nim[m] === 'function';
            });
            
            // 消息操作
            const msgOps = ['recallMsg', 'forwardMsg', 'resendMsg', 'deleteMsg', 'deleteLocalMsg', 'deleteMsgSelf', 'deleteMsgSelfBatch'];
            msgOps.forEach(m => {
                msgApis[m] = typeof window.nim[m] === 'function';
            });
            
            // 消息获取
            const getMethods = ['getHistoryMsgs', 'getLocalMsgs', 'getLocalMsgByIdClient', 'getLocalMsgsByIdClients'];
            getMethods.forEach(m => {
                msgApis[m] = typeof window.nim[m] === 'function';
            });
            
            // 消息已读
            const readMethods = ['sendMsgReceipt', 'getTeamMsgReads', 'getTeamMsgReadAccounts', 'sendTeamMsgReceipt'];
            readMethods.forEach(m => {
                msgApis[m] = typeof window.nim[m] === 'function';
            });
            
            return msgApis;
        })()
    `);
    
    if (msgFormats.result?.result?.value) {
        const apis = msgFormats.result.result.value;
        console.log('消息相关API可用性:');
        for (let [api, exists] of Object.entries(apis)) {
            console.log(`  ${exists ? '✅' : '❌'} ${api}`);
        }
    }

    // ============================================================
    // 12. 完整提取所有可调用的API
    // ============================================================
    console.log('\n' + '='.repeat(80));
    console.log('【12. 完整API清单】');
    console.log('='.repeat(80) + '\n');
    
    const allApis = await send(`
        (function() {
            if (!window.nim) return { error: 'nim not found' };
            
            const all = {};
            let count = 0;
            
            for (let key of Object.keys(window.nim)) {
                if (typeof window.nim[key] === 'function') {
                    all[key] = true;
                    count++;
                }
            }
            
            return { count, apis: Object.keys(all).sort() };
        })()
    `);
    
    if (allApis.result?.result?.value) {
        const data = allApis.result.result.value;
        console.log(`总计 ${data.count} 个API方法:\n`);
        data.apis?.forEach((api, i) => {
            console.log(`${String(i+1).padStart(3)}. ${api}`);
        });
    }

    ws.close();
    console.log('\n' + '='.repeat(80));
    console.log('逆向解析完成！');
    console.log('='.repeat(80));
}

deepReverse().catch(console.error);
