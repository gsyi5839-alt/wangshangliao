/**
 * 安全分析旺商聊消息协议 - 只读取不修改
 */
const WebSocket = require('ws');
const http = require('http');
const crypto = require('crypto');

let ws = null;
let msgId = 0;

const LOGO_ACCOUNT = '1391351554';

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

// 分析Base64数据的二进制结构
function analyzeBase64(b64) {
    try {
        let std = b64.replace(/-/g, '+').replace(/_/g, '/');
        const mod = std.length % 4;
        if (mod) std += '='.repeat(4 - mod);
        
        const buf = Buffer.from(std, 'base64');
        return {
            length: buf.length,
            hex: buf.toString('hex'),
            // 分析头部结构
            header: {
                byte0: buf[0]?.toString(16).padStart(2, '0'),
                bytes1_4: buf.slice(1, 5).toString('hex'),
                bytes5_8: buf.slice(5, 9).toString('hex'),
                bytes9_12: buf.slice(9, 13).toString('hex'),
            }
        };
    } catch (e) {
        return { error: e.message };
    }
}

async function main() {
    console.log('🔍 安全分析旺商聊消息协议\n');
    
    const wsUrl = await getWebSocketUrl();
    if (!wsUrl) {
        console.log('❌ 无法连接到旺商聊');
        return;
    }
    
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    console.log('✅ 已连接\n');
    
    // 1. 获取历史消息分析
    console.log('=== 1. 分析历史消息的加密格式 ===\n');
    const history = await evaluate(`(async () => {
        return new Promise(r => {
            window.nim.getHistoryMsgs({
                scene: 'p2p',
                to: '${LOGO_ACCOUNT}',
                limit: 15,
                done: (err, obj) => {
                    if (err) r({ error: err.message });
                    else r((obj.msgs || []).map(m => ({
                        flow: m.flow,
                        type: m.type,
                        time: m.time,
                        text: m.text,
                        content: m.content
                    })));
                }
            });
            setTimeout(() => r({ timeout: true }), 10000);
        });
    })()`);
    
    if (history.error || history.timeout) {
        console.log('获取历史失败:', history);
    } else {
        // 分析每条消息
        const customMsgs = history.filter(m => m.type === 'custom' && m.content);
        const textMsgs = history.filter(m => m.type === 'text');
        
        console.log(`Custom消息: ${customMsgs.length}条, Text消息: ${textMsgs.length}条\n`);
        
        // 详细分析custom消息
        console.log('--- Custom消息分析 ---\n');
        customMsgs.slice(0, 5).forEach((msg, i) => {
            console.log(`${i + 1}. [${msg.flow}] ${new Date(msg.time).toLocaleTimeString()}`);
            try {
                const content = JSON.parse(msg.content);
                if (content.b) {
                    const analysis = analyzeBase64(content.b);
                    console.log(`   字节长度: ${analysis.length}`);
                    console.log(`   头部: ${analysis.header?.byte0} | ${analysis.header?.bytes1_4} | ${analysis.header?.bytes5_8}`);
                    console.log(`   完整HEX: ${analysis.hex.substring(0, 80)}...`);
                }
            } catch (e) {
                console.log(`   解析失败: ${e.message}`);
            }
            console.log('');
        });
        
        // 分析text消息
        console.log('--- Text消息分析 ---\n');
        textMsgs.slice(0, 3).forEach((msg, i) => {
            console.log(`${i + 1}. [${msg.flow}] ${new Date(msg.time).toLocaleTimeString()}`);
            console.log(`   内容: ${msg.text?.substring(0, 50)}`);
            console.log('');
        });
    }
    
    // 2. 比较收发消息的格式差异
    console.log('\n=== 2. 收发消息格式对比 ===\n');
    
    const inMsgs = (history || []).filter(m => m.flow === 'in' && m.type === 'custom' && m.content);
    const outMsgs = (history || []).filter(m => m.flow === 'out' && m.type === 'custom' && m.content);
    
    if (inMsgs.length > 0) {
        console.log('收到的消息(in)特征:');
        const inContent = JSON.parse(inMsgs[0].content);
        const inAnalysis = analyzeBase64(inContent.b);
        console.log(`  协议头: 0x${inAnalysis.header?.byte0}`);
        console.log(`  魔数: ${inAnalysis.header?.bytes1_4}`);
        console.log(`  子类型: ${inAnalysis.header?.bytes5_8}`);
    }
    
    if (outMsgs.length > 0) {
        console.log('\n发出的消息(out)特征:');
        const outContent = JSON.parse(outMsgs[0].content);
        const outAnalysis = analyzeBase64(outContent.b);
        console.log(`  协议头: 0x${outAnalysis.header?.byte0}`);
        console.log(`  魔数: ${outAnalysis.header?.bytes1_4}`);
        console.log(`  子类型: ${outAnalysis.header?.bytes5_8}`);
    }
    
    // 3. 协议格式推断
    console.log('\n\n=== 3. 协议格式推断 ===\n');
    
    if (inMsgs.length > 0) {
        // 分析多条消息找出固定部分和变化部分
        const analyses = inMsgs.slice(0, 3).map(m => {
            const content = JSON.parse(m.content);
            return analyzeBase64(content.b);
        });
        
        console.log('协议结构推断:');
        console.log('  字节0: 协议版本 (固定 0x09)');
        console.log('  字节1-4: 魔数/标识');
        console.log('  字节5-8: 子协议类型');
        console.log('  字节9-12: 可能是时间戳或序列号');
        console.log('  字节13+: 加密的消息内容');
        
        // 检查是否所有消息都有相同的协议头
        const allSameHeader = analyses.every(a => 
            a.header?.byte0 === analyses[0].header?.byte0 &&
            a.header?.bytes1_4 === analyses[0].header?.bytes1_4
        );
        console.log(`\n  协议头一致性: ${allSameHeader ? '✅ 是' : '❌ 否'}`);
    }
    
    // 4. 检查是否可以直接复制消息格式
    console.log('\n\n=== 4. 测试消息发送可行性 ===\n');
    
    // 检查sendCustomMsg是否可用
    const sendCheck = await evaluate(`(() => {
        return {
            hasSendCustomMsg: typeof window.nim.sendCustomMsg === 'function',
            hasSendText: typeof window.nim.sendText === 'function',
            nimAccount: window.nim.options?.account
        };
    })()`, false);
    console.log('发送能力检查:', sendCheck);
    
    console.log('\n========================================');
    console.log('分析完成！');
    console.log('========================================\n');
    
    console.log('【结论】');
    console.log('1. 旺商聊私聊使用custom类型消息');
    console.log('2. content格式: {"b":"BASE64加密数据"}');
    console.log('3. 加密数据使用特定协议头 (0x09 + 魔数)');
    console.log('4. 需要逆向分析编码逻辑才能正确发送');
    console.log('\n建议: 暂时使用text类型发送，测试对方是否能收到');
    
    ws.close();
}

main().catch(console.error);
