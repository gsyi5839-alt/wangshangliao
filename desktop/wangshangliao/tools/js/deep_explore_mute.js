// 深度探索旺商聊禁言和消息免打扰的底层实现
// Deep exploration of mute functions

const WebSocket = require('ws');
const http = require('http');
const fs = require('fs');

async function getDebuggerUrl() {
    return new Promise((resolve, reject) => {
        http.get('http://127.0.0.1:9222/json', (res) => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', () => {
                const pages = JSON.parse(data);
                const mainPage = pages.find(p => p.url.includes('index.html'));
                if (mainPage) resolve(mainPage.webSocketDebuggerUrl);
                else reject(new Error('未找到旺商聊主页面'));
            });
        }).on('error', reject);
    });
}

async function evaluate(ws, code, awaitPromise = false) {
    return new Promise((resolve, reject) => {
        const id = Math.floor(Math.random() * 100000);
        const handler = (data) => {
            const msg = JSON.parse(data.toString());
            if (msg.id === id) {
                ws.removeListener('message', handler);
                if (msg.error) reject(new Error(msg.error.message));
                else if (msg.result && msg.result.result) {
                    if (msg.result.result.value !== undefined) resolve(msg.result.result.value);
                    else resolve(msg.result.result);
                } else resolve(null);
            }
        };
        ws.on('message', handler);
        ws.send(JSON.stringify({
            id, method: 'Runtime.evaluate',
            params: { expression: code, returnByValue: true, awaitPromise }
        }));
        setTimeout(() => { ws.removeListener('message', handler); reject(new Error('Timeout')); }, 30000);
    });
}

async function explore() {
    console.log('='.repeat(70));
    console.log('深度探索禁言和消息免打扰底层实现');
    console.log('='.repeat(70));
    
    const cdpUrl = await getDebuggerUrl();
    const ws = new WebSocket(cdpUrl);
    await new Promise(resolve => ws.on('open', resolve));
    console.log('✅ 已连接到旺商聊\n');

    const results = {};

    // ============================================
    // 1. 搜索所有与mute相关的NIM方法
    // ============================================
    console.log('【1. 搜索所有mute相关方法】\n');
    
    const muteMethods = await evaluate(ws, `
        (function() {
            const methods = [];
            const nim = window.nim;
            
            // 搜索nim对象上的mute方法
            for (let key in nim) {
                if (key.toLowerCase().includes('mute')) {
                    methods.push({
                        name: 'nim.' + key,
                        type: typeof nim[key]
                    });
                }
            }
            
            // 搜索nim原型链
            let proto = Object.getPrototypeOf(nim);
            while (proto) {
                for (let key of Object.getOwnPropertyNames(proto)) {
                    if (key.toLowerCase().includes('mute')) {
                        methods.push({
                            name: 'nim.prototype.' + key,
                            type: typeof proto[key]
                        });
                    }
                }
                proto = Object.getPrototypeOf(proto);
            }
            
            return methods;
        })()
    `);
    console.log('找到的mute方法:', JSON.stringify(muteMethods, null, 2));
    results.muteMethods = muteMethods;

    // ============================================
    // 2. 搜索与notify/disturb相关的方法（消息免打扰）
    // ============================================
    console.log('\n【2. 搜索消息免打扰相关方法】\n');
    
    const notifyMethods = await evaluate(ws, `
        (function() {
            const methods = [];
            const nim = window.nim;
            
            const keywords = ['notify', 'disturb', 'silent', 'dnd', 'setting'];
            
            for (let key in nim) {
                const keyLower = key.toLowerCase();
                for (let kw of keywords) {
                    if (keyLower.includes(kw)) {
                        methods.push({
                            name: 'nim.' + key,
                            type: typeof nim[key]
                        });
                        break;
                    }
                }
            }
            
            return methods;
        })()
    `);
    console.log('消息通知相关方法:', JSON.stringify(notifyMethods, null, 2));
    results.notifyMethods = notifyMethods;

    // ============================================
    // 3. 探索Pinia Store中的禁言相关方法
    // ============================================
    console.log('\n【3. 探索Pinia Store禁言方法】\n');
    
    const piniaStoreMethods = await evaluate(ws, `
        (function() {
            const results = {};
            
            // 查找pinia
            let pinia = window.pinia || window.__pinia;
            if (!pinia && window.__vue_app__) {
                const provides = window.__vue_app__._context.provides;
                for (let key in provides) {
                    if (provides[key] && provides[key]._s) {
                        pinia = provides[key];
                        break;
                    }
                }
            }
            
            if (!pinia || !pinia._s) {
                return {error: 'Pinia not found'};
            }
            
            // 遍历所有store
            pinia._s.forEach((store, name) => {
                const storeMethods = [];
                for (let key in store) {
                    const keyLower = key.toLowerCase();
                    if (keyLower.includes('mute') || keyLower.includes('silent') || 
                        keyLower.includes('disturb') || keyLower.includes('notify') ||
                        keyLower.includes('setting') || keyLower.includes('team')) {
                        storeMethods.push({
                            name: key,
                            type: typeof store[key]
                        });
                    }
                }
                if (storeMethods.length > 0) {
                    results[name] = storeMethods;
                }
            });
            
            return results;
        })()
    `);
    console.log('Pinia Store方法:', JSON.stringify(piniaStoreMethods, null, 2));
    results.piniaStoreMethods = piniaStoreMethods;

    // ============================================
    // 4. 搜索window上的全局方法
    // ============================================
    console.log('\n【4. 搜索window全局禁言方法】\n');
    
    const windowMethods = await evaluate(ws, `
        (function() {
            const methods = [];
            const keywords = ['mute', 'team', 'group', 'setting', 'notify'];
            
            for (let key in window) {
                try {
                    const keyLower = key.toLowerCase();
                    for (let kw of keywords) {
                        if (keyLower.includes(kw) && typeof window[key] === 'function') {
                            methods.push(key);
                            break;
                        }
                    }
                } catch(e) {}
            }
            
            return methods;
        })()
    `);
    console.log('Window全局方法:', windowMethods);
    results.windowMethods = windowMethods;

    // ============================================
    // 5. 深度探索nim.db中的方法
    // ============================================
    console.log('\n【5. 探索nim.db数据库方法】\n');
    
    const dbMethods = await evaluate(ws, `
        (function() {
            if (!window.nim.db) return {error: 'nim.db not found'};
            
            const methods = [];
            for (let key in window.nim.db) {
                if (typeof window.nim.db[key] === 'function') {
                    const keyLower = key.toLowerCase();
                    if (keyLower.includes('mute') || keyLower.includes('team') || 
                        keyLower.includes('setting') || keyLower.includes('update')) {
                        methods.push(key);
                    }
                }
            }
            return methods;
        })()
    `);
    console.log('nim.db方法:', dbMethods);
    results.dbMethods = dbMethods;

    // ============================================
    // 6. 获取当前群的完整信息（包括所有字段）
    // ============================================
    console.log('\n【6. 获取群完整信息】\n');
    
    const teamId = await evaluate(ws, `
        (function() {
            const url = window.location.href;
            const match = url.match(/team-(\\d+)/);
            return match ? match[1] : null;
        })()
    `);
    
    if (teamId) {
        const fullTeamInfo = await evaluate(ws, `
            new Promise(r => window.nim.getTeam({
                teamId: '${teamId}',
                done: (e, t) => {
                    if (e) {
                        r({error: e.message});
                        return;
                    }
                    // 获取所有字段
                    const info = {};
                    for (let key in t) {
                        info[key] = t[key];
                    }
                    r(info);
                }
            }))
        `, true);
        console.log('群完整信息:', JSON.stringify(fullTeamInfo, null, 2));
        results.fullTeamInfo = fullTeamInfo;
    }

    // ============================================
    // 7. 搜索updateTeam可用的所有参数
    // ============================================
    console.log('\n【7. 探索updateTeam可用参数】\n');
    
    const updateTeamParams = await evaluate(ws, `
        (function() {
            // 通过尝试调用来发现参数
            const knownParams = [
                'teamId', 'name', 'avatar', 'intro', 'announcement',
                'joinMode', 'beInviteMode', 'inviteMode', 'updateTeamMode',
                'updateCustomMode', 'teamMsgNotifyMode', 'custom', 'mute',
                'muteType', 'level', 'ext', 'serverExt'
            ];
            return knownParams;
        })()
    `);
    console.log('updateTeam已知参数:', updateTeamParams);

    // ============================================
    // 8. 测试teamMsgNotifyMode（消息通知模式）
    // ============================================
    console.log('\n【8. 测试teamMsgNotifyMode】\n');
    
    if (teamId) {
        // 尝试设置消息通知模式
        const notifyModeTest = await evaluate(ws, `
            new Promise(r => {
                // 尝试获取当前通知模式
                window.nim.getTeam({
                    teamId: '${teamId}',
                    done: (e, t) => {
                        if (e) {
                            r({error: e.message});
                            return;
                        }
                        r({
                            teamMsgNotifyMode: t.teamMsgNotifyMode,
                            mute: t.mute,
                            muteType: t.muteType,
                            allFields: Object.keys(t)
                        });
                    }
                });
            })
        `, true);
        console.log('当前通知模式:', notifyModeTest);
        results.notifyModeTest = notifyModeTest;
    }

    // ============================================
    // 9. 搜索所有nim方法名
    // ============================================
    console.log('\n【9. 搜索所有nim方法（完整列表）】\n');
    
    const allNimMethods = await evaluate(ws, `
        (function() {
            const methods = [];
            const nim = window.nim;
            
            // 直接属性
            for (let key in nim) {
                if (typeof nim[key] === 'function') {
                    methods.push(key);
                }
            }
            
            // 原型链
            let proto = Object.getPrototypeOf(nim);
            while (proto && proto !== Object.prototype) {
                for (let key of Object.getOwnPropertyNames(proto)) {
                    if (typeof proto[key] === 'function' && !methods.includes(key)) {
                        methods.push(key);
                    }
                }
                proto = Object.getPrototypeOf(proto);
            }
            
            return methods.sort();
        })()
    `);
    console.log('所有NIM方法 (' + allNimMethods.length + '个):');
    
    // 筛选与禁言相关的
    const muteRelated = allNimMethods.filter(m => 
        m.toLowerCase().includes('mute') || 
        m.toLowerCase().includes('team') ||
        m.toLowerCase().includes('notify') ||
        m.toLowerCase().includes('setting')
    );
    console.log('\n禁言/通知相关方法:', muteRelated);
    results.muteRelatedMethods = muteRelated;
    results.allNimMethodsCount = allNimMethods.length;

    // ============================================
    // 10. 尝试直接调用底层禁言API
    // ============================================
    console.log('\n【10. 测试底层禁言API】\n');
    
    if (teamId) {
        // 测试updateTeamMuteType
        const muteTypeTest = await evaluate(ws, `
            (function() {
                const hasMuteType = typeof window.nim.updateTeamMuteType === 'function';
                const hasUpdateMute = typeof window.nim.updateTeamMute === 'function';
                const hasTeamMute = typeof window.nim.teamMute === 'function';
                const hasSetTeamMute = typeof window.nim.setTeamMute === 'function';
                
                return {
                    updateTeamMuteType: hasMuteType,
                    updateTeamMute: hasUpdateMute,
                    teamMute: hasTeamMute,
                    setTeamMute: hasSetTeamMute
                };
            })()
        `);
        console.log('禁言API检查:', muteTypeTest);
        results.muteTypeTest = muteTypeTest;
        
        // 测试通过updateTeam设置mute
        const updateMuteTest = await evaluate(ws, `
            new Promise(r => {
                window.nim.updateTeam({
                    teamId: '${teamId}',
                    mute: true,
                    muteType: 'normal',
                    done: (e, t) => {
                        if (e) {
                            r({error: e.message, code: e.code});
                        } else {
                            r({success: true, mute: t.mute, muteType: t.muteType});
                        }
                    }
                });
            })
        `, true);
        console.log('updateTeam设置mute测试:', updateMuteTest);
        results.updateMuteTest = updateMuteTest;
    }

    // ============================================
    // 11. 探索options中的回调
    // ============================================
    console.log('\n【11. 探索nim.options相关回调】\n');
    
    const optionsCallbacks = await evaluate(ws, `
        (function() {
            const callbacks = [];
            const options = window.nim.options;
            
            for (let key in options) {
                if (key.startsWith('on') && typeof options[key] === 'function') {
                    callbacks.push(key);
                }
            }
            
            // 筛选与mute/team相关的
            const muteRelated = callbacks.filter(c => 
                c.toLowerCase().includes('mute') || 
                c.toLowerCase().includes('team')
            );
            
            return {
                total: callbacks.length,
                muteRelated: muteRelated,
                all: callbacks
            };
        })()
    `);
    console.log('Options回调:', optionsCallbacks);
    results.optionsCallbacks = optionsCallbacks;

    // ============================================
    // 12. 搜索源码中的禁言实现
    // ============================================
    console.log('\n【12. 搜索App/SDK Store中的禁言方法】\n');
    
    const storeSearch = await evaluate(ws, `
        (function() {
            const results = {};
            
            // 查找pinia
            let pinia = window.pinia || window.__pinia;
            if (!pinia && window.__vue_app__) {
                const provides = window.__vue_app__._context.provides;
                for (let key in provides) {
                    if (provides[key] && provides[key]._s) {
                        pinia = provides[key];
                        break;
                    }
                }
            }
            
            if (!pinia || !pinia._s) {
                return {error: 'Pinia not found'};
            }
            
            // 获取sdkStore
            const sdkStore = pinia._s.get('sdk');
            if (sdkStore) {
                const sdkMethods = [];
                for (let key in sdkStore) {
                    if (typeof sdkStore[key] === 'function') {
                        sdkMethods.push(key);
                    }
                }
                results.sdkStoreMethods = sdkMethods;
            }
            
            // 获取appStore
            const appStore = pinia._s.get('app');
            if (appStore) {
                const appMethods = [];
                for (let key in appStore) {
                    if (typeof appStore[key] === 'function') {
                        appMethods.push(key);
                    }
                }
                results.appStoreMethods = appMethods;
            }
            
            // 获取cacheStore
            const cacheStore = pinia._s.get('cache');
            if (cacheStore) {
                const cacheMethods = [];
                for (let key in cacheStore) {
                    if (typeof cacheStore[key] === 'function') {
                        cacheMethods.push(key);
                    }
                }
                results.cacheStoreMethods = cacheMethods;
            }
            
            return results;
        })()
    `);
    console.log('Store方法:', JSON.stringify(storeSearch, null, 2));
    results.storeSearch = storeSearch;

    // ============================================
    // 保存结果
    // ============================================
    fs.writeFileSync('deep_mute_exploration.json', JSON.stringify(results, null, 2));
    console.log('\n📄 结果已保存: deep_mute_exploration.json');
    
    ws.close();
}

explore().then(() => {
    console.log('\n===== 深度探索完成 =====');
    process.exit(0);
}).catch(err => {
    console.error('错误:', err);
    process.exit(1);
});

