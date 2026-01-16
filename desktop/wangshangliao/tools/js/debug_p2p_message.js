/**
 * 调试私聊消息内容 - 查看完整消息结构
 */
const WebSocket = require('ws');
const http = require('http');

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

async function main() {
    console.log('🔍 调试私聊消息内容\n');
    
    const wsUrl = await getWebSocketUrl();
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    console.log('✅ 已连接\n');
    
    // 注入详细消息捕获
    await evaluate(`(() => {
        window.__debugP2PMessages = [];
        var orig = window.nim.options.onmsg;
        window.nim.options.onmsg = function(msg) {
            // 捕获完整消息对象
            window.__debugP2PMessages.push({
                // 基本信息
                scene: msg.scene,
                from: msg.from,
                to: msg.to,
                flow: msg.flow,
                
                // 消息类型
                type: msg.type,
                
                // 内容字段
                text: msg.text,
                content: msg.content,
                body: msg.body,
                attach: msg.attach,
                custom: msg.custom,
                
                // 文件信息（如果有）
                file: msg.file,
                
                // 时间
                time: msg.time,
                
                // 其他
                idClient: msg.idClient,
                idServer: msg.idServer,
                
                // 完整键
                allKeys: Object.keys(msg)
            });
            if (orig) orig(msg);
        };
        return true;
    })()`, false);
    
    console.log('已注入详细消息捕获');
    console.log(`\n⏳ 请从 logo (${LOGO_ACCOUNT}) 发送私聊消息...\n`);
    console.log('支持的消息类型: 文本、图片、表情、文件\n');
    
    for (let i = 0; i < 60; i++) {
        await new Promise(r => setTimeout(r, 1000));
        
        const msgs = await evaluate(`(() => {
            return window.__debugP2PMessages || [];
        })()`, false);
        
        // 查找来自 logo 的私聊消息
        const fromLogo = (msgs || []).filter(m => 
            m.scene === 'p2p' && 
            m.from === '${LOGO_ACCOUNT}' &&
            m.flow === 'in'
        );
        
        if (fromLogo.length > 0) {
            console.log('\n\n📩 收到 logo 的私聊消息!\n');
            
            fromLogo.forEach((m, idx) => {
                console.log(`=== 消息 ${idx + 1} ===`);
                console.log('类型:', m.type);
                console.log('场景:', m.scene);
                console.log('来自:', m.from);
                console.log('到:', m.to);
                console.log('流向:', m.flow);
                console.log('');
                console.log('text:', m.text || '(空)');
                console.log('content:', JSON.stringify(m.content) || '(空)');
                console.log('body:', JSON.stringify(m.body) || '(空)');
                console.log('attach:', JSON.stringify(m.attach) || '(空)');
                console.log('custom:', JSON.stringify(m.custom) || '(空)');
                console.log('file:', JSON.stringify(m.file) || '(空)');
                console.log('');
                console.log('所有字段:', m.allKeys?.join(', '));
                console.log('');
            });
            
            // 如果是自定义消息，尝试解析 content
            const customMsg = fromLogo.find(m => m.type === 'custom');
            if (customMsg && customMsg.content) {
                console.log('\n📦 解析自定义消息 content:');
                try {
                    const parsed = typeof customMsg.content === 'string' 
                        ? JSON.parse(customMsg.content) 
                        : customMsg.content;
                    console.log(JSON.stringify(parsed, null, 2));
                    
                    // 提取可能的文本内容
                    const possibleText = parsed.text || parsed.msg || parsed.message || 
                                        parsed.data?.text || parsed.data?.msg;
                    if (possibleText) {
                        console.log('\n提取的文本:', possibleText);
                    }
                } catch (e) {
                    console.log('解析失败:', e.message);
                }
            }
            
            break;
        }
        
        process.stdout.write(`\r等待消息... ${60 - i}秒 (消息数: ${msgs?.length || 0})`);
    }
    
    console.log('\n');
    ws.close();
}

main().catch(console.error);
