/**
 * 检查旺商聊UI中显示的图片消息
 */
const WebSocket = require('ws');
const http = require('http');

http.get('http://localhost:9222/json', (res) => {
    let data = '';
    res.on('data', chunk => data += chunk);
    res.on('end', async () => {
        const pages = JSON.parse(data);
        const page = pages.find(p => p.url && p.url.includes('wangshangliao'));
        if (!page) return console.log('Not found');
        
        const ws = new WebSocket(page.webSocketDebuggerUrl);
        
        ws.on('open', () => {
            ws.send(JSON.stringify({
                id: 1,
                method: 'Runtime.evaluate',
                params: {
                    expression: `
(function() {
    var result = { uiImages: [], listInfo: null };
    
    // 查找所有图片元素
    var allImgs = document.querySelectorAll('img');
    var chatImgs = [];
    allImgs.forEach(function(img) {
        if (img.src && img.src.includes('nim-nosdn.netease.im')) {
            chatImgs.push({
                src: img.src.substring(0, 100),
                width: img.width,
                height: img.height,
                visible: img.offsetParent !== null
            });
        }
    });
    result.uiImages = chatImgs.slice(0, 10);
    
    // 查找消息列表
    var msgContainer = document.querySelector('.chat-message-list') || 
                       document.querySelector('.message-list') ||
                       document.querySelector('[class*="msg-list"]');
    if (msgContainer) {
        result.listInfo = {
            className: msgContainer.className,
            childCount: msgContainer.children.length,
            scrollTop: msgContainer.scrollTop,
            scrollHeight: msgContainer.scrollHeight
        };
    }
    
    // 检查最后几条消息
    var msgItems = document.querySelectorAll('[class*="msg-item"], [class*="message-item"], .m-message');
    result.msgCount = msgItems.length;
    result.lastMsgs = [];
    for (var i = Math.max(0, msgItems.length - 5); i < msgItems.length; i++) {
        var m = msgItems[i];
        result.lastMsgs.push({
            index: i,
            hasImg: m.querySelector('img') !== null,
            text: (m.textContent || '').substring(0, 50)
        });
    }
    
    return JSON.stringify(result, null, 2);
})();`,
                    returnByValue: true
                }
            }));
        });
        
        ws.on('message', (msg) => {
            const data = JSON.parse(msg);
            if (data.result && data.result.result) {
                console.log('=== 旺商聊UI中的图片和消息 ===');
                console.log(data.result.result.value);
            }
            ws.close();
        });
    });
}).on('error', console.error);

