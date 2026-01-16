/**
 * 深度逆向解析旺商聊群聊API v2
 * 使用更可靠的方式提取所有API
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
    console.log('深度逆向解析旺商聊群聊API v2');
    console.log('='.repeat(80));
    
    // 1. 使用getOwnPropertyNames获取所有方法
    console.log('\n【1. NIM SDK 所有方法 (getOwnPropertyNames + prototype)】\n');
    
    const allMethods = await send(`
        (function() {
            if (!window.nim) return { error: 'nim not found' };
            
            const methods = new Set();
            
            // 自身属性
            Object.getOwnPropertyNames(window.nim).forEach(k => {
                if (typeof window.nim[k] === 'function') methods.add(k);
            });
            
            // 可枚举属性
            for (let k in window.nim) {
                if (typeof window.nim[k] === 'function') methods.add(k);
            }
            
            // 原型链
            let proto = Object.getPrototypeOf(window.nim);
            while (proto && proto !== Object.prototype) {
                Object.getOwnPropertyNames(proto).forEach(k => {
                    if (typeof window.nim[k] === 'function') methods.add(k);
                });
                proto = Object.getPrototypeOf(proto);
            }
            
            return {
                count: methods.size,
                methods: Array.from(methods).sort()
            };
        })()
    `);
    
    if (allMethods.result?.result?.value) {
        const data = allMethods.result.result.value;
        console.log(`共发现 ${data.count} 个方法:\n`);
        
        // 分类
        const categories = {
            team: [],
            superTeam: [],
            msg: [],
            user: [],
            friend: [],
            session: [],
            file: [],
            other: []
        };
        
        data.methods.forEach(m => {
            const lower = m.toLowerCase();
            if (lower.includes('superteam')) categories.superTeam.push(m);
            else if (lower.includes('team')) categories.team.push(m);
            else if (lower.includes('msg') || lower.includes('send') || lower.includes('recall') || lower.includes('forward')) categories.msg.push(m);
            else if (lower.includes('user') || lower.includes('account') || lower.includes('myinfo')) categories.user.push(m);
            else if (lower.includes('friend') || lower.includes('blacklist') || lower.includes('mutelist')) categories.friend.push(m);
            else if (lower.includes('session')) categories.session.push(m);
            else if (lower.includes('file') || lower.includes('upload') || lower.includes('download') || lower.includes('preview') || lower.includes('nos')) categories.file.push(m);
            else categories.other.push(m);
        });
        
        console.log('【群组API (Team)】', categories.team.length);
        categories.team.forEach(m => console.log(`  - ${m}`));
        
        console.log('\n【超级群API (SuperTeam)】', categories.superTeam.length);
        categories.superTeam.forEach(m => console.log(`  - ${m}`));
        
        console.log('\n【消息API】', categories.msg.length);
        categories.msg.forEach(m => console.log(`  - ${m}`));
        
        console.log('\n【用户API】', categories.user.length);
        categories.user.forEach(m => console.log(`  - ${m}`));
        
        console.log('\n【好友/黑名单API】', categories.friend.length);
        categories.friend.forEach(m => console.log(`  - ${m}`));
        
        console.log('\n【会话API】', categories.session.length);
        categories.session.forEach(m => console.log(`  - ${m}`));
        
        console.log('\n【文件API】', categories.file.length);
        categories.file.forEach(m => console.log(`  - ${m}`));
        
        console.log('\n【其他API】', categories.other.length);
        categories.other.forEach(m => console.log(`  - ${m}`));
    }

    // 2. 获取当前群ID
    console.log('\n' + '='.repeat(80));
    console.log('【2. 当前会话信息】');
    console.log('='.repeat(80) + '\n');
    
    const sessionInfo = await send(`
        (function() {
            // 方法1: 从URL获取
            const url = window.location.href;
            const match = url.match(/sessionId=team-([\\d]+)/);
            if (match) return { teamId: match[1], source: 'url' };
            
            // 方法2: 从Pinia获取
            const app = document.querySelector('#app');
            if (app && app.__vue_app__) {
                const pinia = app.__vue_app__._context?.provides?.pinia;
                if (pinia?.state?.value?.app) {
                    const appState = pinia.state.value.app;
                    const session = appState.currentSession || appState.currSession;
                    if (session) {
                        return { 
                            teamId: session.to || session.id, 
                            session: session,
                            source: 'pinia'
                        };
                    }
                }
            }
            
            return { error: 'No session found' };
        })()
    `);
    
    let teamId = null;
    if (sessionInfo.result?.result?.value) {
        const info = sessionInfo.result.result.value;
        teamId = info.teamId;
        console.log('来源:', info.source);
        console.log('群ID:', teamId);
        if (info.session) {
            console.log('会话详情:', JSON.stringify(info.session, null, 2));
        }
    }

    // 3. 获取群详细信息
    if (teamId) {
        console.log('\n' + '='.repeat(80));
        console.log('【3. 群详细信息 (getTeam)】');
        console.log('='.repeat(80) + '\n');
        
        const teamInfo = await send(`
            (function() {
                return new Promise((resolve) => {
                    window.nim.getTeam({
                        teamId: '${teamId}',
                        done: (err, team) => {
                            if (err) {
                                resolve({ error: err.message || String(err) });
                                return;
                            }
                            
                            // 完整字段
                            const fields = {};
                            for (let key in team) {
                                const val = team[key];
                                if (typeof val === 'function') continue;
                                fields[key] = val;
                            }
                            resolve(fields);
                        }
                    });
                });
            })()
        `);
        
        if (teamInfo.result?.result?.value) {
            const team = teamInfo.result.result.value;
            console.log('群信息完整字段:');
            for (let [key, value] of Object.entries(team)) {
                const valStr = typeof value === 'object' ? JSON.stringify(value) : String(value);
                console.log(`  ${key}: ${valStr.substring(0, 150)}${valStr.length > 150 ? '...' : ''}`);
            }
        }

        // 4. 获取群成员
        console.log('\n' + '='.repeat(80));
        console.log('【4. 群成员详细信息 (getTeamMembers)】');
        console.log('='.repeat(80) + '\n');
        
        const members = await send(`
            (function() {
                return new Promise((resolve) => {
                    window.nim.getTeamMembers({
                        teamId: '${teamId}',
                        done: (err, result) => {
                            if (err) {
                                resolve({ error: err.message });
                                return;
                            }
                            
                            const list = result.members || result;
                            
                            // 获取所有字段
                            const memberFields = list[0] ? Object.keys(list[0]) : [];
                            
                            // 示例成员
                            const samples = list.slice(0, 5).map(m => {
                                const obj = {};
                                for (let k in m) {
                                    if (typeof m[k] !== 'function') {
                                        obj[k] = m[k];
                                    }
                                }
                                return obj;
                            });
                            
                            resolve({
                                totalCount: list.length,
                                memberFields,
                                samples
                            });
                        }
                    });
                });
            })()
        `);
        
        if (members.result?.result?.value) {
            const data = members.result.result.value;
            console.log('成员总数:', data.totalCount);
            console.log('成员字段:', data.memberFields.join(', '));
            console.log('\n示例成员:');
            data.samples.forEach((m, i) => {
                console.log(`\n成员${i+1}:`);
                for (let [k, v] of Object.entries(m)) {
                    const valStr = typeof v === 'object' ? JSON.stringify(v) : String(v);
                    console.log(`  ${k}: ${valStr.substring(0, 100)}`);
                }
            });
        }
    }

    // 5. 事件回调完整列表
    console.log('\n' + '='.repeat(80));
    console.log('【5. NIM事件回调完整列表 (nim.options)】');
    console.log('='.repeat(80) + '\n');
    
    const callbacks = await send(`
        (function() {
            if (!window.nim || !window.nim.options) return { error: 'nim.options not found' };
            
            const result = {
                teamCallbacks: [],
                msgCallbacks: [],
                sysCallbacks: [],
                syncCallbacks: [],
                otherCallbacks: []
            };
            
            for (let key of Object.keys(window.nim.options)) {
                if (typeof window.nim.options[key] === 'function') {
                    const lower = key.toLowerCase();
                    if (lower.includes('team')) result.teamCallbacks.push(key);
                    else if (lower.includes('msg') || lower.includes('message')) result.msgCallbacks.push(key);
                    else if (lower.includes('sys')) result.sysCallbacks.push(key);
                    else if (lower.includes('sync') || lower.includes('roam')) result.syncCallbacks.push(key);
                    else result.otherCallbacks.push(key);
                }
            }
            
            return result;
        })()
    `);
    
    if (callbacks.result?.result?.value) {
        const cbs = callbacks.result.result.value;
        console.log('【群组事件】', cbs.teamCallbacks?.length);
        cbs.teamCallbacks?.forEach(c => console.log(`  - ${c}`));
        
        console.log('\n【消息事件】', cbs.msgCallbacks?.length);
        cbs.msgCallbacks?.forEach(c => console.log(`  - ${c}`));
        
        console.log('\n【系统事件】', cbs.sysCallbacks?.length);
        cbs.sysCallbacks?.forEach(c => console.log(`  - ${c}`));
        
        console.log('\n【同步事件】', cbs.syncCallbacks?.length);
        cbs.syncCallbacks?.forEach(c => console.log(`  - ${c}`));
        
        console.log('\n【其他事件】', cbs.otherCallbacks?.length);
        cbs.otherCallbacks?.forEach(c => console.log(`  - ${c}`));
    }

    // 6. Pinia Store 详细探索
    console.log('\n' + '='.repeat(80));
    console.log('【6. Pinia Store 详细探索】');
    console.log('='.repeat(80) + '\n');
    
    const piniaData = await send(`
        (function() {
            const app = document.querySelector('#app');
            if (!app || !app.__vue_app__) return { error: 'Vue app not found' };
            
            const pinia = app.__vue_app__._context?.provides?.pinia;
            if (!pinia) return { error: 'Pinia not found' };
            
            const result = {
                stores: [],
                appStoreActions: [],
                appStoreGetters: [],
                appStoreState: {},
                cacheStoreActions: [],
                sdkStoreActions: []
            };
            
            // 获取所有store
            if (pinia._s) {
                pinia._s.forEach((store, name) => {
                    result.stores.push(name);
                });
            }
            
            // appStore
            if (pinia._s?.has('app')) {
                const store = pinia._s.get('app');
                for (let key of Object.keys(store)) {
                    if (key.startsWith('$') || key.startsWith('_')) continue;
                    
                    if (typeof store[key] === 'function') {
                        result.appStoreActions.push(key);
                    } else {
                        result.appStoreState[key] = typeof store[key];
                    }
                }
            }
            
            // cacheStore
            if (pinia._s?.has('cache')) {
                const store = pinia._s.get('cache');
                for (let key of Object.keys(store)) {
                    if (key.startsWith('$') || key.startsWith('_')) continue;
                    if (typeof store[key] === 'function') {
                        result.cacheStoreActions.push(key);
                    }
                }
            }
            
            // sdkStore
            if (pinia._s?.has('sdk')) {
                const store = pinia._s.get('sdk');
                for (let key of Object.keys(store)) {
                    if (key.startsWith('$') || key.startsWith('_')) continue;
                    if (typeof store[key] === 'function') {
                        result.sdkStoreActions.push(key);
                    }
                }
            }
            
            return result;
        })()
    `);
    
    if (piniaData.result?.result?.value) {
        const data = piniaData.result.result.value;
        console.log('所有Store:', data.stores?.join(', '));
        
        console.log('\n【appStore Actions】', data.appStoreActions?.length);
        data.appStoreActions?.forEach(a => console.log(`  - ${a}`));
        
        console.log('\n【appStore State】');
        for (let [k, v] of Object.entries(data.appStoreState || {})) {
            console.log(`  ${k}: ${v}`);
        }
        
        console.log('\n【cacheStore Actions】', data.cacheStoreActions?.length);
        data.cacheStoreActions?.forEach(a => console.log(`  - ${a}`));
        
        console.log('\n【sdkStore Actions】', data.sdkStoreActions?.length);
        data.sdkStoreActions?.forEach(a => console.log(`  - ${a}`));
    }

    // 7. 测试群组API
    console.log('\n' + '='.repeat(80));
    console.log('【7. 群组API功能测试】');
    console.log('='.repeat(80) + '\n');
    
    if (teamId) {
        const apiTest = await send(`
            (function() {
                return new Promise(async (resolve) => {
                    const teamId = '${teamId}';
                    const results = {};
                    
                    // getTeam
                    try {
                        const team = await new Promise((res, rej) => {
                            window.nim.getTeam({ teamId, done: (e, t) => e ? rej(e) : res(t) });
                        });
                        results.getTeam = { ok: true, name: team.name, memberNum: team.memberNum };
                    } catch(e) {
                        results.getTeam = { ok: false, error: e.message };
                    }
                    
                    // getTeams
                    try {
                        const teams = await new Promise((res, rej) => {
                            window.nim.getTeams({ done: (e, t) => e ? rej(e) : res(t) });
                        });
                        results.getTeams = { ok: true, count: teams.length };
                    } catch(e) {
                        results.getTeams = { ok: false, error: e.message };
                    }
                    
                    // getTeamMembers
                    try {
                        const members = await new Promise((res, rej) => {
                            window.nim.getTeamMembers({ teamId, done: (e, r) => e ? rej(e) : res(r) });
                        });
                        const list = members.members || members;
                        results.getTeamMembers = { ok: true, count: list.length };
                    } catch(e) {
                        results.getTeamMembers = { ok: false, error: e.message };
                    }
                    
                    // getLocalTeams
                    try {
                        const local = await new Promise((res, rej) => {
                            window.nim.getLocalTeams({ done: (e, t) => e ? rej(e) : res(t) });
                        });
                        results.getLocalTeams = { ok: true, count: local?.length };
                    } catch(e) {
                        results.getLocalTeams = { ok: false, error: e.message };
                    }
                    
                    // getLocalTeamMembers
                    try {
                        const local = await new Promise((res, rej) => {
                            window.nim.getLocalTeamMembers({ teamId, done: (e, r) => e ? rej(e) : res(r) });
                        });
                        results.getLocalTeamMembers = { ok: true, count: local?.length };
                    } catch(e) {
                        results.getLocalTeamMembers = { ok: false, error: e.message };
                    }
                    
                    // getMutedTeamMembers
                    try {
                        const muted = await new Promise((res, rej) => {
                            window.nim.getMutedTeamMembers({ teamId, done: (e, r) => e ? rej(e) : res(r) });
                        });
                        results.getMutedTeamMembers = { ok: true, count: muted?.length || 0 };
                    } catch(e) {
                        results.getMutedTeamMembers = { ok: false, error: e.message };
                    }
                    
                    resolve(results);
                });
            })()
        `);
        
        if (apiTest.result?.result?.value) {
            const results = apiTest.result.result.value;
            for (let [api, result] of Object.entries(results)) {
                const status = result.ok ? '✅' : '❌';
                console.log(`${status} ${api}:`, JSON.stringify(result));
            }
        }
    }

    // 8. 获取所有群列表
    console.log('\n' + '='.repeat(80));
    console.log('【8. 所有群列表 (getTeams)】');
    console.log('='.repeat(80) + '\n');
    
    const allTeams = await send(`
        (function() {
            return new Promise((resolve) => {
                window.nim.getTeams({
                    done: (err, teams) => {
                        if (err) {
                            resolve({ error: err.message });
                            return;
                        }
                        
                        resolve({
                            count: teams.length,
                            teams: teams.slice(0, 5).map(t => ({
                                teamId: t.teamId,
                                name: t.name,
                                memberNum: t.memberNum,
                                owner: t.owner,
                                type: t.type
                            }))
                        });
                    }
                });
            });
        })()
    `);
    
    if (allTeams.result?.result?.value) {
        const data = allTeams.result.result.value;
        console.log('群总数:', data.count);
        console.log('\n前5个群:');
        data.teams?.forEach((t, i) => {
            console.log(`  ${i+1}. ${t.name} (${t.teamId}) - ${t.memberNum}人`);
        });
    }

    // 9. 消息相关API测试
    console.log('\n' + '='.repeat(80));
    console.log('【9. 消息API列表】');
    console.log('='.repeat(80) + '\n');
    
    const msgApis = await send(`
        (function() {
            const apis = {
                send: [],
                receive: [],
                operate: [],
                read: [],
                history: []
            };
            
            // 发送
            ['sendText', 'sendFile', 'sendCustomMsg', 'sendTipMsg', 'sendGeo', 'sendAudio', 'sendVideo'].forEach(m => {
                if (typeof window.nim[m] === 'function') apis.send.push(m);
            });
            
            // 操作
            ['recallMsg', 'forwardMsg', 'resendMsg', 'deleteMsg', 'deleteLocalMsg', 'deleteMsgSelf', 'deleteMsgSelfBatch'].forEach(m => {
                if (typeof window.nim[m] === 'function') apis.operate.push(m);
            });
            
            // 已读
            ['sendMsgReceipt', 'getTeamMsgReads', 'getTeamMsgReadAccounts', 'sendTeamMsgReceipt'].forEach(m => {
                if (typeof window.nim[m] === 'function') apis.read.push(m);
            });
            
            // 历史
            ['getHistoryMsgs', 'getLocalMsgs', 'getLocalMsgByIdClient', 'getLocalMsgsByIdClients'].forEach(m => {
                if (typeof window.nim[m] === 'function') apis.history.push(m);
            });
            
            return apis;
        })()
    `);
    
    if (msgApis.result?.result?.value) {
        const apis = msgApis.result.result.value;
        console.log('【发送消息】', apis.send?.join(', '));
        console.log('【消息操作】', apis.operate?.join(', '));
        console.log('【已读回执】', apis.read?.join(', '));
        console.log('【历史消息】', apis.history?.join(', '));
    }

    // 10. 解密相关探索
    console.log('\n' + '='.repeat(80));
    console.log('【10. 解密逻辑探索】');
    console.log('='.repeat(80) + '\n');
    
    const decryptInfo = await send(`
        (function() {
            const result = {
                hasAES: false,
                hasCryptoJS: false,
                decryptFunctions: [],
                key: null,
                iv: null
            };
            
            // 检查全局CryptoJS
            if (window.CryptoJS) {
                result.hasCryptoJS = true;
            }
            
            // 搜索解密函数
            const searchKeys = ['decrypt', 'Decrypt', 'AES', 'aes', 'crypto'];
            for (let key in window) {
                if (searchKeys.some(s => key.includes(s))) {
                    result.decryptFunctions.push(key);
                }
            }
            
            return result;
        })()
    `);
    
    console.log('解密信息:', JSON.stringify(decryptInfo.result?.result?.value, null, 2));

    ws.close();
    console.log('\n' + '='.repeat(80));
    console.log('深度逆向完成！');
    console.log('='.repeat(80));
}

deepReverse().catch(console.error);
