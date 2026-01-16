/**
 * 私聊自动回复机器人
 * 独立运行，不依赖C#软件
 */
const WebSocket = require('ws');
const http = require('http');

let ws = null;
let msgId = 0;

// 配置
const CONFIG = {
    // 自动回复内容
    autoReplyContent: '【机器人自动回复】您好，已收到您的消息，稍后回复~',
    
    // 关键词回复（关键词: 回复内容）
    keywordReplies: {
        '你好': '您好！有什么可以帮您的？',
        '在吗': '在的，请说~',
        '查': '请发送：查 + 金额，例如：查100',
        '1': '您选择了选项1',
        '测试': '测试回复成功！'
    },
    
    // 是否启用默认自动回复（无关键词匹配时）
    enableDefaultReply: true,
    
    // 已处理消息缓存（防止重复回复）
    processedMsgs: new Set()
};

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

// 解码Base64消息内容
function decodeBase64Content(base64) {
    if (!base64) return '';
    try {
        // URL-safe Base64 转标准 Base64
        let std = base64.replace(/-/g, '+').replace(/_/g, '/');
        const mod = std.length % 4;
        if (mod) std += '='.repeat(4 - mod);
        
        const buf = Buffer.from(std, 'base64');
        const text = buf.toString('utf8');
        
        // 提取中文字符
        const chineseMatch = text.match(/[\u4e00-\u9fff\w\d]+/g);
        return chineseMatch ? chineseMatch.join('') : text;
    } catch (e) {
        return '';
    }
}

// 解析消息内容
function parseMessageContent(msg) {
    // 如果有text，直接返回
    if (msg.text) return msg.text;
    
    // 尝试从content解析
    if (msg.content) {
        try {
            const content = typeof msg.content === 'string' ? JSON.parse(msg.content) : msg.content;
            
            // 尝试各种可能的文本字段
            if (content.text) return content.text;
            if (content.msg) return content.msg;
            if (content.message) return content.message;
            if (content.data?.text) return content.data.text;
            if (content.data?.msg) return content.data.msg;
            
            // 尝试解码 b 字段
            if (content.b) {
                return decodeBase64Content(content.b);
            }
        } catch (e) {}
    }
    
    return '';
}

// 检查关键词回复
function getKeywordReply(content) {
    if (!content) return null;
    
    for (const [keyword, reply] of Object.entries(CONFIG.keywordReplies)) {
        if (content.includes(keyword)) {
            return reply;
        }
    }
    
    return CONFIG.enableDefaultReply ? CONFIG.autoReplyContent : null;
}

// 发送私聊回复
async function sendP2PReply(to, text) {
    const result = await evaluate(`(async () => {
        return new Promise(resolve => {
            window.nim.sendText({
                scene: 'p2p',
                to: '${to}',
                text: '${text.replace(/'/g, "\\'")}',
                done: (err, msg) => {
                    if (err) resolve({ success: false, error: err.message });
                    else resolve({ success: true, idServer: msg?.idServer });
                }
            });
            setTimeout(() => resolve({ success: false, error: 'Timeout' }), 5000);
        });
    })()`);
    
    return result;
}

async function main() {
    console.log('🤖 私聊自动回复机器人启动\n');
    console.log('配置:');
    console.log('  - 默认回复:', CONFIG.enableDefaultReply ? '✅ 启用' : '❌ 禁用');
    console.log('  - 关键词数量:', Object.keys(CONFIG.keywordReplies).length);
    console.log('');
    
    const wsUrl = await getWebSocketUrl();
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    console.log('✅ 已连接旺商聊\n');
    
    // 获取当前账号
    const myInfo = await evaluate(`(async () => {
        return new Promise(r => {
            window.nim.getMyInfo({ done: (e, i) => r(i || {}) });
            setTimeout(() => r({}), 5000);
        });
    })()`);
    console.log('当前账号:', myInfo?.account);
    console.log('');
    
    // 安装消息监听Hook
    await evaluate(`(() => {
        window.__p2pAutoReplyQueue = [];
        
        var orig = window.__origOnmsgForAutoReply || window.nim.options?.onmsg;
        window.__origOnmsgForAutoReply = orig;
        
        window.nim.options.onmsg = function(msg) {
            // 只处理私聊入站消息
            if (msg.scene === 'p2p' && msg.flow === 'in') {
                window.__p2pAutoReplyQueue.push({
                    from: msg.from,
                    text: msg.text || '',
                    content: msg.content,
                    type: msg.type,
                    time: Date.now(),
                    idClient: msg.idClient
                });
            }
            if (orig) orig(msg);
        };
        return true;
    })()`, false);
    
    console.log('✅ 消息监听已启动\n');
    console.log('⏳ 等待私聊消息...\n');
    console.log('按 Ctrl+C 停止\n');
    console.log('========================================\n');
    
    // 轮询处理消息
    while (true) {
        try {
            const msgs = await evaluate(`(() => {
                var queue = window.__p2pAutoReplyQueue || [];
                window.__p2pAutoReplyQueue = [];
                return queue;
            })()`, false);
            
            for (const msg of (msgs || [])) {
                // 检查是否已处理
                const msgKey = `${msg.from}-${msg.idClient}`;
                if (CONFIG.processedMsgs.has(msgKey)) continue;
                CONFIG.processedMsgs.add(msgKey);
                
                // 解析消息内容
                const content = parseMessageContent(msg);
                const time = new Date(msg.time).toLocaleTimeString();
                
                console.log(`📩 [${time}] 收到私聊`);
                console.log(`   来自: ${msg.from}`);
                console.log(`   类型: ${msg.type}`);
                console.log(`   内容: "${content || '(空)'}"`);
                
                // 获取回复
                const reply = getKeywordReply(content);
                
                if (reply) {
                    console.log(`📤 发送回复: ${reply.substring(0, 30)}...`);
                    const result = await sendP2PReply(msg.from, reply);
                    
                    if (result?.success) {
                        console.log('   ✅ 回复成功');
                    } else {
                        console.log('   ❌ 回复失败:', result?.error);
                    }
                } else {
                    console.log('   ⏭️ 无匹配回复');
                }
                
                console.log('');
            }
            
            // 清理过旧的已处理消息缓存
            if (CONFIG.processedMsgs.size > 1000) {
                CONFIG.processedMsgs.clear();
            }
            
        } catch (e) {
            if (e.message !== 'Timeout') {
                console.error('错误:', e.message);
            }
        }
        
        await new Promise(r => setTimeout(r, 500)); // 500ms 轮询
    }
}

main().catch(e => {
    console.error('启动失败:', e.message);
    process.exit(1);
});
