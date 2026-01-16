/**
 * 分析旺商聊的custom消息加密格式
 */
const WebSocket = require('ws');
const http = require('http');
const crypto = require('crypto');

let ws = null;
let msgId = 0;

const LOGO_ACCOUNT = '1391351554';

// AES解密参数（已知）
const AES_KEY = Buffer.from('wangshang@#!1234', 'utf8');  // 16字节
const AES_IV = Buffer.from('1234wangshang@#!', 'utf8');   // 16字节

// 尝试解密Base64数据
function tryDecryptAES(base64Data) {
    try {
        // URL-safe Base64 转标准 Base64
        let std = base64Data.replace(/-/g, '+').replace(/_/g, '/');
        const mod = std.length % 4;
        if (mod) std += '='.repeat(4 - mod);
        
        const encrypted = Buffer.from(std, 'base64');
        
        // 尝试AES-256-CBC解密
        try {
            const decipher = crypto.createDecipheriv('aes-256-cbc', 
                Buffer.concat([AES_KEY, AES_KEY]), // 32字节key
                AES_IV);
            decipher.setAutoPadding(true);
            let decrypted = decipher.update(encrypted);
            decrypted = Buffer.concat([decrypted, decipher.final()]);
            return { method: 'AES-256-CBC', result: decrypted.toString('utf8') };
        } catch (e) {}
        
        // 尝试AES-128-CBC解密
        try {
            const decipher = crypto.createDecipheriv('aes-128-cbc', AES_KEY, AES_IV);
            decipher.setAutoPadding(true);
            let decrypted = decipher.update(encrypted);
            decrypted = Buffer.concat([decrypted, decipher.final()]);
            return { method: 'AES-128-CBC', result: decrypted.toString('utf8') };
        } catch (e) {}
        
        // 返回原始字节分析
        return { 
            method: 'raw',
            hex: encrypted.toString('hex').substring(0, 100),
            utf8Try: encrypted.toString('utf8').substring(0, 100),
            length: encrypted.length
        };
    } catch (e) {
        return { error: e.message };
    }
}

// AES加密
function encryptAES(text) {
    try {
        const cipher = crypto.createCipheriv('aes-256-cbc', 
            Buffer.concat([AES_KEY, AES_KEY]), // 32字节key
            AES_IV);
        let encrypted = cipher.update(text, 'utf8');
        encrypted = Buffer.concat([encrypted, cipher.final()]);
        
        // 转URL-safe Base64
        return encrypted.toString('base64')
            .replace(/\+/g, '-')
            .replace(/\//g, '_')
            .replace(/=/g, '');
    } catch (e) {
        return null;
    }
}

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
    console.log('🔍 分析旺商聊custom消息加密格式\n');
    
    const wsUrl = await getWebSocketUrl();
    ws = new WebSocket(wsUrl);
    await new Promise(r => { ws.onopen = r; });
    console.log('✅ 已连接\n');
    
    // 获取收到的custom消息
    console.log('=== 1. 获取收到的custom消息 ===\n');
    const customMsgs = await evaluate(`(async () => {
        return new Promise(r => {
            window.nim.getHistoryMsgs({
                scene: 'p2p',
                to: '${LOGO_ACCOUNT}',
                limit: 10,
                done: (err, obj) => {
                    if (err) r([]);
                    else r((obj?.msgs || []).filter(m => m.type === 'custom' && m.flow === 'in'));
                }
            });
            setTimeout(() => r([]), 10000);
        });
    })()`);
    
    console.log(`找到 ${customMsgs?.length || 0} 条custom消息\n`);
    
    // 分析每条消息的content
    (customMsgs || []).slice(0, 3).forEach((msg, i) => {
        console.log(`--- 消息 ${i + 1} ---`);
        console.log('时间:', new Date(msg.time).toLocaleTimeString());
        
        if (msg.content) {
            try {
                const content = typeof msg.content === 'string' ? JSON.parse(msg.content) : msg.content;
                console.log('content结构:', Object.keys(content));
                
                if (content.b) {
                    console.log('b字段长度:', content.b.length);
                    console.log('b字段前50字符:', content.b.substring(0, 50));
                    
                    // 尝试解密
                    const decrypted = tryDecryptAES(content.b);
                    console.log('解密尝试:', decrypted);
                }
            } catch (e) {
                console.log('解析失败:', e.message);
            }
        }
        console.log('');
    });
    
    // 2. 在旺商聊中查找加密函数
    console.log('=== 2. 搜索旺商聊中的加密函数 ===\n');
    const cryptoFuncs = await evaluate(`(() => {
        var results = [];
        
        // 搜索全局对象中的加密相关函数
        for (var key in window) {
            if (key.toLowerCase().includes('encrypt') || 
                key.toLowerCase().includes('crypto') ||
                key.toLowerCase().includes('aes')) {
                results.push({ name: key, type: typeof window[key] });
            }
        }
        
        // 检查常见的加密库
        results.push({ 'CryptoJS': typeof window.CryptoJS });
        results.push({ 'crypto': typeof window.crypto });
        results.push({ 'forge': typeof window.forge });
        
        return results;
    })()`, false);
    console.log('找到的加密相关对象:', cryptoFuncs);
    
    // 3. 尝试用正确的格式发送custom消息
    console.log('\n=== 3. 尝试用加密格式发送消息 ===\n');
    
    // 尝试加密文本
    const testText = '机器人测试消息';
    const encrypted = encryptAES(testText);
    console.log('测试文本:', testText);
    console.log('加密后:', encrypted?.substring(0, 50));
    
    if (encrypted) {
        const sendResult = await evaluate(`(async () => {
            var content = JSON.stringify({ b: '${encrypted}' });
            console.log('发送content:', content);
            
            return new Promise(r => {
                window.nim.sendCustomMsg({
                    scene: 'p2p',
                    to: '${LOGO_ACCOUNT}',
                    content: content,
                    done: (err, msg) => {
                        if (err) r({ success: false, error: err.message, code: err.code });
                        else r({ 
                            success: true, 
                            idServer: msg?.idServer,
                            content: msg?.content
                        });
                    }
                });
                setTimeout(() => r({ error: 'Timeout' }), 10000);
            });
        })()`);
        console.log('发送结果:', sendResult);
    }
    
    // 4. 分析旺商聊源码中的发送逻辑
    console.log('\n=== 4. 搜索旺商聊的消息发送组件 ===\n');
    const sendLogic = await evaluate(`(() => {
        // 查找消息输入组件
        var inputAreas = document.querySelectorAll('[class*="input"], [class*="editor"], textarea');
        var results = [];
        
        inputAreas.forEach(el => {
            var vueComp = el.__vue__ || el._vnode?.component?.proxy;
            if (vueComp) {
                var methods = Object.keys(vueComp).filter(k => 
                    typeof vueComp[k] === 'function' && 
                    (k.includes('send') || k.includes('submit') || k.includes('msg'))
                );
                if (methods.length > 0) {
                    results.push({
                        className: el.className?.substring(0, 30),
                        methods: methods
                    });
                }
            }
        });
        
        return results;
    })()`, false);
    console.log('消息发送组件:', sendLogic);
    
    console.log('\n');
    ws.close();
}

main().catch(console.error);
