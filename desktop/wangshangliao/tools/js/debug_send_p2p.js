/**
 * 深度调试私聊发送问题
 */
const WebSocket = require('ws');
const http = require('http');

let ws = null;
let msgId = 0;

const ROBOT_ACCOUNT = '1948408648';  // 机器人（法拉利客服）
const LOGO_ACCOUNT = '1391351554';   // logo

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
        const timeout = setTimeout(() => reject(new Error('Timeout')), 20000);
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
    console.log('🔍 深度调试私聊发送\n');
    
    const wsUrl = await getWebSocketUrl();
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    console.log('✅ 已连接\n');
    
    // 1. 确认当前登录账号
    console.log('=== 1. 当前登录账号 ===\n');
    const myInfo = await evaluate(`(async () => {
        return new Promise(r => {
            window.nim.getMyInfo({ done: (e, i) => r(i || {}) });
            setTimeout(() => r({}), 5000);
        });
    })()`);
    console.log('当前账号:', myInfo?.account);
    console.log('昵称:', myInfo?.nick);
    
    if (myInfo?.account !== ROBOT_ACCOUNT) {
        console.log('\n⚠️ 警告: 当前登录的不是机器人账号!');
        console.log(`   期望: ${ROBOT_ACCOUNT}`);
        console.log(`   实际: ${myInfo?.account}`);
    }
    
    // 2. 检查与 logo 的会话历史
    console.log('\n=== 2. 检查与 logo 的会话 ===\n');
    const history = await evaluate(`(async () => {
        return new Promise(r => {
            window.nim.getHistoryMsgs({
                scene: 'p2p',
                to: '${LOGO_ACCOUNT}',
                limit: 10,
                done: (err, obj) => {
                    if (err) r({ error: err.message });
                    else r({ msgs: (obj?.msgs || []).map(m => ({
                        flow: m.flow,
                        text: m.text?.substring(0, 50) || '',
                        type: m.type,
                        time: m.time,
                        idServer: m.idServer
                    }))});
                }
            });
            setTimeout(() => r({ error: 'Timeout' }), 10000);
        });
    })()`);
    
    if (history?.error) {
        console.log('获取历史失败:', history.error);
    } else {
        console.log('最近消息记录:');
        (history?.msgs || []).forEach((m, i) => {
            const dir = m.flow === 'in' ? '📥收到' : '📤发出';
            const time = m.time ? new Date(m.time).toLocaleTimeString() : '?';
            console.log(`  ${i + 1}. [${time}] ${dir} | ${m.type} | ${m.text || '(空)'}`);
        });
    }
    
    // 3. 发送测试消息并详细检查结果
    console.log('\n=== 3. 发送测试消息 ===\n');
    
    const testMsg = `【调试测试】机器人回复 ${new Date().toLocaleTimeString()}`;
    console.log('发送内容:', testMsg);
    console.log('目标账号:', LOGO_ACCOUNT);
    console.log('场景: p2p (私聊)');
    
    const sendResult = await evaluate(`(async () => {
        return new Promise((resolve) => {
            console.log('[DEBUG] 开始发送...');
            
            var payload = {
                scene: 'p2p',
                to: '${LOGO_ACCOUNT}',
                text: '${testMsg}'
            };
            
            console.log('[DEBUG] payload:', JSON.stringify(payload));
            
            window.nim.sendText({
                scene: payload.scene,
                to: payload.to,
                text: payload.text,
                done: function(err, msg) {
                    console.log('[DEBUG] done callback, err:', err, 'msg:', msg);
                    
                    if (err) {
                        resolve({
                            success: false,
                            error: err.message || String(err),
                            code: err.code,
                            errObj: JSON.stringify(err)
                        });
                    } else {
                        resolve({
                            success: true,
                            idClient: msg?.idClient,
                            idServer: msg?.idServer,
                            to: msg?.to,
                            scene: msg?.scene,
                            flow: msg?.flow,
                            time: msg?.time,
                            status: msg?.status,
                            fullMsg: JSON.stringify(msg).substring(0, 500)
                        });
                    }
                }
            });
            
            setTimeout(function() {
                resolve({ success: false, error: 'Timeout 20s' });
            }, 20000);
        });
    })()`);
    
    console.log('\n发送结果:');
    console.log(JSON.stringify(sendResult, null, 2));
    
    if (sendResult?.success) {
        console.log('\n✅ API 返回成功');
        console.log('   idServer:', sendResult.idServer);
        console.log('   idClient:', sendResult.idClient);
        console.log('   to:', sendResult.to);
        console.log('   scene:', sendResult.scene);
        console.log('   flow:', sendResult.flow);
        console.log('   status:', sendResult.status);
    } else {
        console.log('\n❌ API 返回失败');
        console.log('   错误:', sendResult?.error);
        console.log('   错误码:', sendResult?.code);
    }
    
    // 4. 再次检查历史，确认消息是否真的发出
    console.log('\n=== 4. 发送后再次检查历史 ===\n');
    await new Promise(r => setTimeout(r, 2000)); // 等待2秒
    
    const history2 = await evaluate(`(async () => {
        return new Promise(r => {
            window.nim.getHistoryMsgs({
                scene: 'p2p',
                to: '${LOGO_ACCOUNT}',
                limit: 5,
                done: (err, obj) => {
                    if (err) r({ error: err.message });
                    else r({ msgs: (obj?.msgs || []).map(m => ({
                        flow: m.flow,
                        text: m.text?.substring(0, 50) || '',
                        type: m.type,
                        time: m.time,
                        idServer: m.idServer
                    }))});
                }
            });
            setTimeout(() => r({ error: 'Timeout' }), 10000);
        });
    })()`);
    
    if (!history2?.error) {
        console.log('发送后的消息记录:');
        (history2?.msgs || []).forEach((m, i) => {
            const dir = m.flow === 'in' ? '📥收到' : '📤发出';
            const time = m.time ? new Date(m.time).toLocaleTimeString() : '?';
            const mark = m.text?.includes('调试测试') ? '⭐ NEW' : '';
            console.log(`  ${i + 1}. [${time}] ${dir} | ${m.type} | ${m.text || '(空)'} ${mark}`);
        });
    }
    
    // 5. 检查是否是好友关系
    console.log('\n=== 5. 检查好友关系 ===\n');
    const isFriend = await evaluate(`(() => {
        return window.nim.isMyFriend({ account: '${LOGO_ACCOUNT}' });
    })()`, false);
    console.log('是否好友:', isFriend ? '✅ 是' : '❌ 否');
    
    console.log('\n========================================');
    console.log('📌 调试完成');
    console.log('========================================\n');
    
    ws.close();
}

main().catch(console.error);
