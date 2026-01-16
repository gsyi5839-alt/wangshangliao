/**
 * 深入分析失败的API - getMutedTeamMembers, 黑名单API, getServerTime
 */
const WebSocket = require('ws');
const fs = require('fs');
const http = require('http');

let ws = null;
let msgId = 0;
const results = { timestamp: new Date().toISOString(), investigations: {} };

async function getWebSocketUrl() {
    return new Promise((resolve, reject) => {
        const req = http.get('http://127.0.0.1:9222/json', (res) => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', () => {
                try {
                    const pages = JSON.parse(data);
                    const mainPage = pages.find(p => p.url?.includes('index.html') || p.title?.includes('旺商聊')) || pages[0];
                    resolve(mainPage?.webSocketDebuggerUrl);
                } catch (e) { reject(e); }
            });
        });
        req.on('error', reject);
        req.setTimeout(5000, () => { req.destroy(); reject(new Error('Timeout')); });
    });
}

function evaluate(expression, awaitPromise = true) {
    return new Promise((resolve, reject) => {
        const id = ++msgId;
        const timeout = setTimeout(() => reject(new Error('Timeout')), 15000);
        const handler = (data) => {
            try {
                const msg = JSON.parse(data.toString());
                if (msg.id === id) {
                    clearTimeout(timeout);
                    ws.off('message', handler);
                    if (msg.error) reject(new Error(msg.error.message));
                    else if (msg.result?.exceptionDetails) reject(new Error(JSON.stringify(msg.result.exceptionDetails)));
                    else resolve(msg.result?.result?.value);
                }
            } catch (e) {}
        };
        ws.on('message', handler);
        ws.send(JSON.stringify({ id, method: 'Runtime.evaluate', params: { expression, awaitPromise, returnByValue: true } }));
    });
}

// ==================== 深入分析 ====================

async function investigateGetMutedTeamMembers() {
    console.log('\n🔍 === 1. 深入分析 getMutedTeamMembers ===\n');
    
    const investigation = { apiExists: false, signature: null, tests: [], error: null };
    
    // 1. 检查API是否存在及其签名
    try {
        const script = `(() => {
            if (!window.nim) return { error: 'nim not found' };
            var fn = window.nim.getMutedTeamMembers;
            if (!fn) return { error: 'getMutedTeamMembers not found' };
            return {
                exists: true,
                type: typeof fn,
                toString: fn.toString().substring(0, 500),
                length: fn.length
            };
        })()`;
        const result = await evaluate(script, false);
        investigation.apiExists = result?.exists;
        investigation.signature = result;
        console.log('API签名:', JSON.stringify(result, null, 2));
    } catch (e) {
        investigation.error = e.message;
        console.log('❌ 获取签名失败:', e.message);
    }
    
    // 2. 获取可用的群列表
    let teams = [];
    let myAccount = null;
    try {
        const script = `(async () => {
            var myInfo = await new Promise(r => {
                window.nim.getMyInfo({ done: (e, i) => r(i) });
                setTimeout(() => r(null), 5000);
            });
            var teams = await new Promise(r => {
                window.nim.getTeams({ done: (e, t) => r(t || []) });
                setTimeout(() => r([]), 5000);
            });
            return {
                myAccount: myInfo?.account,
                teams: teams.map(t => ({
                    teamId: t.teamId,
                    name: t.name,
                    owner: t.owner,
                    memberNum: t.memberNum,
                    type: t.type,
                    isOwner: t.owner === myInfo?.account
                }))
            };
        })()`;
        const result = await evaluate(script);
        teams = result?.teams || [];
        myAccount = result?.myAccount;
        console.log('\n当前账号:', myAccount);
        console.log('群列表:');
        teams.forEach(t => console.log(`  - ${t.teamId} (${t.name?.substring(0,20)}) owner:${t.owner} 是群主:${t.isOwner}`));
    } catch (e) {
        console.log('❌ 获取群列表失败:', e.message);
    }
    
    // 3. 在每个群尝试获取禁言成员
    for (const team of teams) {
        console.log(`\n🔸 测试群 ${team.teamId} (是群主:${team.isOwner})...`);
        try {
            const script = `(async () => {
                return new Promise((resolve) => {
                    window.nim.getMutedTeamMembers({
                        teamId: '${team.teamId}',
                        done: (err, members) => {
                            if (err) {
                                resolve({
                                    success: false,
                                    error: err.message || err.code || JSON.stringify(err),
                                    errorObj: JSON.stringify(err).substring(0, 500)
                                });
                            } else {
                                resolve({
                                    success: true,
                                    count: (members || []).length,
                                    members: (members || []).slice(0, 10).map(m => ({
                                        account: m.account,
                                        nick: m.nick,
                                        nickInTeam: m.nickInTeam,
                                        mute: m.mute,
                                        muteType: m.muteType
                                    }))
                                });
                            }
                        }
                    });
                    setTimeout(() => resolve({ success: false, error: 'Timeout' }), 8000);
                });
            })()`;
            const result = await evaluate(script);
            investigation.tests.push({ teamId: team.teamId, isOwner: team.isOwner, result });
            if (result.success) {
                console.log(`  ✅ 成功! 禁言成员数: ${result.count}`);
                if (result.members?.length > 0) {
                    console.log('  禁言成员:', result.members.map(m => m.account).join(', '));
                }
            } else {
                console.log(`  ❌ 失败: ${result.error}`);
                console.log(`  错误详情: ${result.errorObj}`);
            }
        } catch (e) {
            console.log(`  ❌ 异常: ${e.message}`);
            investigation.tests.push({ teamId: team.teamId, isOwner: team.isOwner, exception: e.message });
        }
    }
    
    // 4. 检查是否有其他类似API
    try {
        const script = `(() => {
            var muteAPIs = [];
            for (var key in window.nim) {
                if (typeof window.nim[key] === 'function' && 
                    (key.toLowerCase().includes('mute') || key.toLowerCase().includes('muted'))) {
                    muteAPIs.push(key);
                }
            }
            return muteAPIs;
        })()`;
        const result = await evaluate(script, false);
        investigation.relatedAPIs = result;
        console.log('\n📋 相关禁言API:', result);
    } catch (e) {}
    
    results.investigations.getMutedTeamMembers = investigation;
}

async function investigateBlacklistAPIs() {
    console.log('\n🔍 === 2. 深入分析黑名单/静音API ===\n');
    
    const investigation = { apis: {}, availableAPIs: [], tests: [] };
    
    // 1. 搜索所有黑名单/静音相关API
    try {
        const script = `(() => {
            var blacklistAPIs = {};
            var keywords = ['black', 'Black', 'mute', 'Mute', 'block', 'Block', 'silent', 'Silent'];
            for (var key in window.nim) {
                if (typeof window.nim[key] === 'function') {
                    var lower = key.toLowerCase();
                    if (keywords.some(k => lower.includes(k.toLowerCase()))) {
                        blacklistAPIs[key] = {
                            type: typeof window.nim[key],
                            length: window.nim[key].length,
                            source: window.nim[key].toString().substring(0, 200)
                        };
                    }
                }
            }
            return blacklistAPIs;
        })()`;
        const result = await evaluate(script, false);
        investigation.apis = result;
        investigation.availableAPIs = Object.keys(result || {});
        console.log('找到的黑名单/静音API:');
        for (const [name, info] of Object.entries(result || {})) {
            console.log(`  ✅ ${name}`);
        }
    } catch (e) {
        console.log('❌ 搜索API失败:', e.message);
    }
    
    // 2. 测试 isUserInBlackList 的各种调用方式
    console.log('\n🔸 测试 isUserInBlackList 各种调用方式:');
    const testCases = [
        { name: '直接传字符串', code: `window.nim.isUserInBlackList('test123')` },
        { name: '传对象{account}', code: `window.nim.isUserInBlackList({account:'test123'})` },
        { name: '传对象{userId}', code: `window.nim.isUserInBlackList({userId:'test123'})` },
        { name: '不传参数看错误', code: `window.nim.isUserInBlackList()` },
    ];
    
    for (const tc of testCases) {
        try {
            const script = `(() => {
                try {
                    var result = ${tc.code};
                    return { success: true, result: result, type: typeof result };
                } catch(e) {
                    return { success: false, error: e.message };
                }
            })()`;
            const result = await evaluate(script, false);
            investigation.tests.push({ case: tc.name, result });
            console.log(`  ${tc.name}: ${result.success ? '✅ ' + JSON.stringify(result.result) : '❌ ' + result.error}`);
        } catch (e) {
            console.log(`  ${tc.name}: ❌ ${e.message}`);
        }
    }
    
    // 3. 测试 getBlacklist
    console.log('\n🔸 测试 getBlacklist:');
    try {
        const script = `(async () => {
            if (typeof window.nim.getBlacklist !== 'function') {
                // 尝试其他可能的API名
                var alternatives = ['getBlackList', 'blacklist', 'getBlack', 'getBlocklist', 'getBlockList'];
                for (var alt of alternatives) {
                    if (typeof window.nim[alt] === 'function') {
                        return { found: alt, type: 'alternative' };
                    }
                }
                return { error: 'getBlacklist not found, no alternatives' };
            }
            return new Promise(r => {
                window.nim.getBlacklist({
                    done: (err, list) => {
                        if (err) r({ error: err.message || JSON.stringify(err) });
                        else r({ success: true, count: (list||[]).length, list: (list||[]).slice(0,10) });
                    }
                });
                setTimeout(() => r({ error: 'Timeout' }), 5000);
            });
        })()`;
        const result = await evaluate(script);
        investigation.getBlacklist = result;
        console.log('  结果:', JSON.stringify(result));
    } catch (e) {
        console.log('  ❌ 异常:', e.message);
    }
    
    // 4. 测试 getMutelist
    console.log('\n🔸 测试 getMutelist:');
    try {
        const script = `(async () => {
            if (typeof window.nim.getMutelist !== 'function') {
                var alternatives = ['getMuteList', 'mutelist', 'getMute', 'getMutedList'];
                for (var alt of alternatives) {
                    if (typeof window.nim[alt] === 'function') {
                        return { found: alt, type: 'alternative' };
                    }
                }
                return { error: 'getMutelist not found, no alternatives' };
            }
            return new Promise(r => {
                window.nim.getMutelist({
                    done: (err, list) => {
                        if (err) r({ error: err.message || JSON.stringify(err) });
                        else r({ success: true, count: (list||[]).length, list: (list||[]).slice(0,10) });
                    }
                });
                setTimeout(() => r({ error: 'Timeout' }), 5000);
            });
        })()`;
        const result = await evaluate(script);
        investigation.getMutelist = result;
        console.log('  结果:', JSON.stringify(result));
    } catch (e) {
        console.log('  ❌ 异常:', e.message);
    }
    
    // 5. 尝试实际添加和检查黑名单
    console.log('\n🔸 测试 addToBlacklist + isUserInBlackList 联合:');
    try {
        const script = `(async () => {
            // 先获取一个好友账号
            var friends = await new Promise(r => {
                window.nim.getFriends({ done: (e, f) => r(f || []) });
                setTimeout(() => r([]), 3000);
            });
            if (friends.length === 0) return { error: 'No friends to test' };
            var testAccount = friends[0].account;
            
            // 检查当前是否在黑名单
            var before = window.nim.isUserInBlackList(testAccount);
            
            return {
                testAccount: testAccount,
                beforeCheck: before,
                note: 'Use addToBlacklist then check again to verify'
            };
        })()`;
        const result = await evaluate(script);
        investigation.combinedTest = result;
        console.log('  结果:', JSON.stringify(result));
    } catch (e) {
        console.log('  ❌ 异常:', e.message);
    }
    
    results.investigations.blacklistAPIs = investigation;
}

async function investigateGetServerTime() {
    console.log('\n🔍 === 3. 深入分析 getServerTime ===\n');
    
    const investigation = { tests: [], analysis: {} };
    
    // 1. 检查API签名
    try {
        const script = `(() => {
            if (!window.nim?.getServerTime) return { error: 'not found' };
            return {
                type: typeof window.nim.getServerTime,
                length: window.nim.getServerTime.length,
                source: window.nim.getServerTime.toString().substring(0, 300)
            };
        })()`;
        const result = await evaluate(script, false);
        investigation.signature = result;
        console.log('API签名:', JSON.stringify(result, null, 2));
    } catch (e) {
        console.log('❌ 获取签名失败:', e.message);
    }
    
    // 2. 多种方式调用 getServerTime
    console.log('\n🔸 测试各种调用方式:');
    
    // 方式1: 直接调用
    try {
        const script = `(() => {
            try {
                var result = window.nim.getServerTime();
                return {
                    success: true,
                    value: result,
                    type: typeof result,
                    isNumber: typeof result === 'number',
                    isFinite: Number.isFinite(result),
                    asDate: result > 0 ? new Date(result).toISOString() : 'invalid'
                };
            } catch(e) {
                return { success: false, error: e.message };
            }
        })()`;
        const result = await evaluate(script, false);
        investigation.tests.push({ method: '直接调用', result });
        console.log('  直接调用:', JSON.stringify(result));
    } catch (e) {
        console.log('  直接调用: ❌', e.message);
    }
    
    // 方式2: 带回调
    try {
        const script = `(async () => {
            try {
                return new Promise((resolve) => {
                    var result = window.nim.getServerTime({
                        done: (err, time) => {
                            if (err) resolve({ method: 'callback', error: err.message });
                            else resolve({ method: 'callback', success: true, value: time });
                        }
                    });
                    // 如果直接返回了值
                    if (result !== undefined) {
                        resolve({ method: 'callback+return', value: result, type: typeof result });
                    }
                    setTimeout(() => resolve({ method: 'callback', timeout: true }), 3000);
                });
            } catch(e) {
                return { error: e.message };
            }
        })()`;
        const result = await evaluate(script);
        investigation.tests.push({ method: '带回调', result });
        console.log('  带回调:', JSON.stringify(result));
    } catch (e) {
        console.log('  带回调: ❌', e.message);
    }
    
    // 3. 获取原始数值并分析
    try {
        const script = `(() => {
            var rawValue = window.nim.getServerTime();
            var clientTime = Date.now();
            return {
                raw: rawValue,
                rawString: String(rawValue),
                rawLength: String(rawValue).length,
                clientTime: clientTime,
                clientTimeLength: String(clientTime).length,
                diff: rawValue - clientTime,
                // 尝试不同的解析方式
                asMillis: rawValue > 1000000000000 ? new Date(rawValue).toISOString() : 'too small for millis',
                asSeconds: rawValue < 10000000000 ? new Date(rawValue * 1000).toISOString() : 'too big for seconds',
                // 检查是否是有效时间
                isValidMillis: rawValue > 1600000000000 && rawValue < 2000000000000,
                isValidSeconds: rawValue > 1600000000 && rawValue < 2000000000
            };
        })()`;
        const result = await evaluate(script, false);
        investigation.analysis = result;
        console.log('\n📊 数值分析:');
        console.log('  原始值:', result.raw);
        console.log('  原始值长度:', result.rawLength, '位');
        console.log('  客户端时间:', result.clientTime, '(', result.clientTimeLength, '位)');
        console.log('  差值:', result.diff, 'ms');
        console.log('  作为毫秒解析:', result.asMillis);
        console.log('  作为秒解析:', result.asSeconds);
        console.log('  是有效毫秒时间戳:', result.isValidMillis);
        console.log('  是有效秒时间戳:', result.isValidSeconds);
    } catch (e) {
        console.log('❌ 分析失败:', e.message);
    }
    
    // 4. 检查其他时间相关API
    try {
        const script = `(() => {
            var timeAPIs = [];
            for (var key in window.nim) {
                if (typeof window.nim[key] === 'function' && 
                    (key.toLowerCase().includes('time') || key.toLowerCase().includes('sync'))) {
                    timeAPIs.push(key);
                }
            }
            return timeAPIs;
        })()`;
        const result = await evaluate(script, false);
        investigation.relatedAPIs = result;
        console.log('\n📋 相关时间API:', result);
    } catch (e) {}
    
    results.investigations.getServerTime = investigation;
}

async function investigatePinia() {
    console.log('\n🔍 === 4. 深入分析 Pinia/Vue状态 ===\n');
    
    const investigation = { methods: [], found: {} };
    
    // 1. 搜索所有可能的状态存储位置
    console.log('🔸 搜索全局状态存储位置:');
    try {
        const script = `(() => {
            var result = { found: [] };
            
            // 检查各种可能的位置
            var checks = [
                { name: '__pinia', obj: window.__pinia },
                { name: 'pinia', obj: window.pinia },
                { name: '__VUE__', obj: window.__VUE__ },
                { name: '__VUE_APP__', obj: window.__VUE_APP__ },
                { name: 'app', obj: window.app },
                { name: '__vue_app__', obj: window.__vue_app__ },
                { name: 'Vue', obj: window.Vue },
                { name: '__VUE_DEVTOOLS_GLOBAL_HOOK__', obj: window.__VUE_DEVTOOLS_GLOBAL_HOOK__ },
            ];
            
            for (var c of checks) {
                if (c.obj) {
                    result.found.push({
                        name: c.name,
                        type: typeof c.obj,
                        keys: Object.keys(c.obj).slice(0, 20),
                        hasState: c.obj.state ? true : false,
                        hasStore: c.obj._stores || c.obj.stores ? true : false
                    });
                }
            }
            
            // 搜索 window 上所有可能包含 store 的属性
            for (var key in window) {
                try {
                    if (key.toLowerCase().includes('store') || key.toLowerCase().includes('pinia') || key.toLowerCase().includes('vue')) {
                        if (window[key] && typeof window[key] === 'object') {
                            result.found.push({
                                name: key,
                                type: typeof window[key],
                                keys: Object.keys(window[key]).slice(0, 10)
                            });
                        }
                    }
                } catch(e) {}
            }
            
            return result;
        })()`;
        const result = await evaluate(script, false);
        investigation.searchResult = result;
        console.log('找到的状态存储:');
        for (const f of result?.found || []) {
            console.log(`  ✅ ${f.name}: ${f.type}, keys: ${f.keys?.join(', ')?.substring(0, 100)}`);
        }
    } catch (e) {
        console.log('❌ 搜索失败:', e.message);
    }
    
    // 2. 尝试通过nim对象访问应用状态
    console.log('\n🔸 通过nim对象访问状态:');
    try {
        const script = `(() => {
            var result = { nimProperties: [] };
            if (window.nim) {
                // 检查nim对象的所有属性
                for (var key in window.nim) {
                    if (typeof window.nim[key] !== 'function') {
                        var val = window.nim[key];
                        result.nimProperties.push({
                            key: key,
                            type: typeof val,
                            isNull: val === null,
                            isUndefined: val === undefined,
                            sample: typeof val === 'object' && val ? Object.keys(val).slice(0,5).join(',') : String(val).substring(0,50)
                        });
                    }
                }
            }
            return result;
        })()`;
        const result = await evaluate(script, false);
        investigation.nimProperties = result?.nimProperties;
        console.log('nim对象非函数属性:');
        for (const p of (result?.nimProperties || []).slice(0, 20)) {
            console.log(`  - ${p.key}: ${p.type} = ${p.sample}`);
        }
    } catch (e) {
        console.log('❌ 获取nim属性失败:', e.message);
    }
    
    // 3. 尝试从document或其他位置获取Vue实例
    console.log('\n🔸 从DOM获取Vue实例:');
    try {
        const script = `(() => {
            var result = { found: false };
            
            // 尝试从根元素获取Vue实例
            var root = document.getElementById('app') || document.querySelector('#app') || document.body.firstElementChild;
            if (root) {
                var vueInstance = root.__vue__ || root.__vue_app__ || root._vnode?.component?.proxy;
                if (vueInstance) {
                    result.found = true;
                    result.type = typeof vueInstance;
                    result.keys = Object.keys(vueInstance).slice(0, 20);
                    // 检查是否有$store或$pinia
                    result.hasStore = vueInstance.$store ? true : false;
                    result.hasPinia = vueInstance.$pinia ? true : false;
                }
            }
            
            return result;
        })()`;
        const result = await evaluate(script, false);
        investigation.vueFromDOM = result;
        console.log('DOM Vue实例:', JSON.stringify(result));
    } catch (e) {
        console.log('❌ 获取DOM Vue失败:', e.message);
    }
    
    // 4. 检查options中的store配置
    console.log('\n🔸 检查nim.options中的store配置:');
    try {
        const script = `(() => {
            if (!window.nim?.options) return { error: 'no nim.options' };
            var opts = window.nim.options;
            return {
                hasOptions: true,
                optionKeys: Object.keys(opts).slice(0, 30),
                // 查找可能的store相关配置
                db: opts.db ? Object.keys(opts.db).slice(0,10) : null,
                syncConversations: opts.syncConversations,
                syncRelations: opts.syncRelations,
                syncTeams: opts.syncTeams,
                syncTeamMembers: opts.syncTeamMembers
            };
        })()`;
        const result = await evaluate(script, false);
        investigation.nimOptions = result;
        console.log('nim.options:', JSON.stringify(result, null, 2));
    } catch (e) {
        console.log('❌ 获取nim.options失败:', e.message);
    }
    
    results.investigations.pinia = investigation;
}

// 主函数
async function main() {
    console.log('🔬 旺商聊失败API深入分析');
    console.log('===========================\n');
    
    try {
        console.log('🔌 正在连接旺商聊客户端...');
        const wsUrl = await getWebSocketUrl();
        console.log(`✅ WebSocket URL: ${wsUrl}\n`);
        
        ws = new WebSocket(wsUrl);
        await new Promise((resolve, reject) => {
            ws.onopen = resolve;
            ws.onerror = reject;
            setTimeout(() => reject(new Error('连接超时')), 10000);
        });
        console.log('✅ 连接成功');
        
        // 执行深入分析
        await investigateGetMutedTeamMembers();
        await investigateBlacklistAPIs();
        await investigateGetServerTime();
        await investigatePinia();
        
        // 保存结果
        fs.writeFileSync('deep_investigation_results.json', JSON.stringify(results, null, 2), 'utf8');
        console.log('\n===========================');
        console.log('💾 详细分析结果已保存到: deep_investigation_results.json');
        
    } catch (error) {
        console.error('\n❌ 分析失败:', error.message);
    } finally {
        if (ws?.readyState === WebSocket.OPEN) ws.close();
    }
}

main();
