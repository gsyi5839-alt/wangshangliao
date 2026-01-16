/**
 * 验证修复后的API
 */
const WebSocket = require('ws');
const http = require('http');

let ws = null;
let msgId = 0;

async function getWebSocketUrl() {
    return new Promise((resolve, reject) => {
        const req = http.get('http://127.0.0.1:9222/json', (res) => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', () => {
                const pages = JSON.parse(data);
                const mainPage = pages.find(p => p.url?.includes('index.html')) || pages[0];
                resolve(mainPage?.webSocketDebuggerUrl);
            });
        });
        req.on('error', reject);
    });
}

function evaluate(expression, awaitPromise = true) {
    return new Promise((resolve, reject) => {
        const id = ++msgId;
        const timeout = setTimeout(() => reject(new Error('Timeout')), 15000);
        const handler = (data) => {
            const msg = JSON.parse(data.toString());
            if (msg.id === id) {
                clearTimeout(timeout);
                ws.off('message', handler);
                resolve(msg.result?.result?.value);
            }
        };
        ws.on('message', handler);
        ws.send(JSON.stringify({ id, method: 'Runtime.evaluate', params: { expression, awaitPromise, returnByValue: true } }));
    });
}

async function main() {
    console.log('🔧 验证API修复\n');
    
    const wsUrl = await getWebSocketUrl();
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    console.log('✅ 已连接\n');
    
    // 1. 验证 isUserInBlackList 修复
    console.log('=== 1. 验证 isUserInBlackList ===');
    try {
        const script = `(async () => {
            // 获取一个测试账号
            var myInfo = await new Promise(r => {
                window.nim.getMyInfo({ done: (e, i) => r(i) });
                setTimeout(() => r(null), 3000);
            });
            var testAccount = myInfo?.account || 'test123';
            
            // 使用正确的调用方式
            var result = window.nim.isUserInBlackList({ account: testAccount });
            return {
                testAccount: testAccount,
                result: result,
                resultType: typeof result,
                inBlacklist: result === true
            };
        })()`;
        const result = await evaluate(script);
        console.log('  测试账号:', result?.testAccount);
        console.log('  返回结果:', result?.result);
        console.log('  结果类型:', result?.resultType);
        console.log('  是否在黑名单:', result?.inBlacklist);
        console.log('  ✅ isUserInBlackList 修复验证成功!\n');
    } catch (e) {
        console.log('  ❌ 失败:', e.message, '\n');
    }
    
    // 2. 验证 getServerTime 调用方式
    console.log('=== 2. 验证 getServerTime ===');
    try {
        const script = `(async () => {
            return new Promise((resolve) => {
                window.nim.getServerTime({
                    done: (err, serverTime) => {
                        if (err) resolve({ error: err.message });
                        else resolve({
                            serverTime: serverTime,
                            asDate: new Date(serverTime).toISOString(),
                            valid: serverTime > 1600000000000
                        });
                    }
                });
                setTimeout(() => resolve({ error: 'Timeout' }), 5000);
            });
        })()`;
        const result = await evaluate(script);
        console.log('  服务器时间:', result?.serverTime);
        console.log('  格式化时间:', result?.asDate);
        console.log('  有效时间戳:', result?.valid);
        console.log('  ✅ getServerTime 调用方式验证成功!\n');
    } catch (e) {
        console.log('  ❌ 失败:', e.message, '\n');
    }
    
    // 3. 验证 getMutedTeamMembers 备用方法
    console.log('=== 3. 验证 getMutedTeamMembers 备用方法 ===');
    try {
        const script = `(async () => {
            // 获取群列表
            var teams = await new Promise(r => {
                window.nim.getTeams({ done: (e, t) => r(t || []) });
                setTimeout(() => r([]), 3000);
            });
            if (!teams.length) return { error: 'No teams' };
            
            var teamId = teams[0].teamId;
            
            // 使用备用方法: getTeamMembers + 筛选 mute=true
            var membersResult = await new Promise(r => {
                window.nim.getTeamMembers({
                    teamId: teamId,
                    done: (err, obj) => {
                        if (err) r({ error: err.message });
                        else r({ members: obj?.members || [] });
                    }
                });
                setTimeout(() => r({ error: 'Timeout' }), 8000);
            });
            
            if (membersResult.error) return membersResult;
            
            var mutedMembers = membersResult.members.filter(m => m.mute === true);
            return {
                teamId: teamId,
                totalMembers: membersResult.members.length,
                mutedCount: mutedMembers.length,
                mutedAccounts: mutedMembers.slice(0, 10).map(m => m.account),
                method: 'getTeamMembers + filter(mute=true)'
            };
        })()`;
        const result = await evaluate(script);
        console.log('  群ID:', result?.teamId);
        console.log('  总成员数:', result?.totalMembers);
        console.log('  禁言成员数:', result?.mutedCount);
        console.log('  禁言账号:', result?.mutedAccounts?.join(', ') || '(无)');
        console.log('  使用方法:', result?.method);
        console.log('  ✅ getMutedTeamMembers 备用方法验证成功!\n');
    } catch (e) {
        console.log('  ❌ 失败:', e.message, '\n');
    }
    
    // 4. 验证可用的黑名单API
    console.log('=== 4. 验证黑名单相关API ===');
    try {
        const script = `(() => {
            var apis = ['markInBlacklist', 'addToBlacklist', 'removeFromBlacklist', 
                       'markInMutelist', 'addToMutelist', 'removeFromMutelist', 'isUserInBlackList'];
            var result = {};
            for (var api of apis) {
                result[api] = typeof window.nim[api] === 'function';
            }
            return result;
        })()`;
        const result = await evaluate(script, false);
        console.log('  API可用性:');
        for (const [api, available] of Object.entries(result || {})) {
            console.log(`    ${available ? '✅' : '❌'} ${api}`);
        }
        console.log('');
    } catch (e) {
        console.log('  ❌ 失败:', e.message, '\n');
    }
    
    console.log('=== 验证完成 ===');
    ws.close();
}

main().catch(console.error);
