/**
 * 检查当前群的 GroupId
 */
const WebSocket = require('ws');
const http = require('http');

function httpGet(url) {
    return new Promise((resolve, reject) => {
        http.get(url, (res) => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', () => {
                try { resolve(JSON.parse(data)); }
                catch(e) { reject(new Error('Invalid JSON')); }
            });
        }).on('error', reject);
    });
}

async function main() {
    console.log('=== 检查群 GroupId ===\n');
    
    const pages = await httpGet('http://localhost:9222/json');
    const page = pages.find(p => p.url && p.url.includes('wangshangliao'));
    
    if (!page) {
        console.log('WangShangLiao not found');
        return;
    }
    
    const ws = new WebSocket(page.webSocketDebuggerUrl);
    let messageId = 1;
    const pending = new Map();
    
    ws.on('message', (data) => {
        const msg = JSON.parse(data);
        if (msg.id && pending.has(msg.id)) {
            pending.get(msg.id)(msg);
            pending.delete(msg.id);
        }
    });
    
    function sendCommand(method, params = {}) {
        return new Promise((resolve, reject) => {
            const id = messageId++;
            pending.set(id, resolve);
            ws.send(JSON.stringify({ id, method, params }));
            setTimeout(() => {
                if (pending.has(id)) {
                    pending.delete(id);
                    reject(new Error('Timeout'));
                }
            }, 30000);
        });
    }
    
    async function evaluate(expression, awaitPromise = true) {
        const result = await sendCommand('Runtime.evaluate', {
            expression,
            returnByValue: true,
            awaitPromise
        });
        return result.result?.result?.value;
    }
    
    await new Promise(resolve => ws.on('open', resolve));
    console.log('Connected!\n');
    
    // 获取当前会话的完整群信息
    const groupInfo = await evaluate(`
(function() {
    var result = {
        success: false,
        error: null
    };
    
    try {
        var state = JSON.parse(localStorage.getItem('managestate') || '{}');
        
        // 当前会话
        if (state.currSession) {
            result.currSession = {
                id: state.currSession.id,
                scene: state.currSession.scene,
                to: state.currSession.to
            };
            
            if (state.currSession.group) {
                result.group = {
                    groupId: state.currSession.group.groupId,        // 数字ID (后端API用)
                    groupCloudId: state.currSession.group.groupCloudId,  // NIM SDK ID
                    groupAccount: state.currSession.group.groupAccount,  // 群号
                    groupName: state.currSession.group.groupName
                };
            }
        }
        
        // 群列表
        if (state.groupList) {
            var allGroups = [
                ...(state.groupList.owner || []),
                ...(state.groupList.member || [])
            ];
            
            result.groupCount = allGroups.length;
            result.sampleGroups = allGroups.slice(0, 3).map(function(g) {
                return {
                    groupId: g.groupId,
                    groupCloudId: g.groupCloudId,
                    groupAccount: g.groupAccount,
                    groupName: g.groupName
                };
            });
        }
        
        result.success = true;
    } catch(e) {
        result.error = e.message;
    }
    
    return JSON.stringify(result, null, 2);
})();`, false);
    
    console.log('群信息:');
    console.log(groupInfo);
    
    const info = JSON.parse(groupInfo);
    
    if (info.group) {
        console.log('\n=== 关键ID对比 ===');
        console.log('groupId (数字，后端API用):', info.group.groupId);
        console.log('groupCloudId (NIM SDK用):', info.group.groupCloudId);
        console.log('groupAccount (群号):', info.group.groupAccount);
        
        if (!info.group.groupId || info.group.groupId <= 0) {
            console.log('\n⚠️ 警告: groupId 无效！这会导致 SetGroupMuteMode 等API调用失败');
        }
    }
    
    ws.close();
}

main().catch(console.error);

