/**
 * xclient 通信代理
 * 监听旺商聊和xclient之间的实际通信
 */

const net = require('net');
const fs = require('fs');

const XCLIENT_PORT = 21303;
const PROXY_PORT = 21304;
const LOG_FILE = 'xclient_traffic.log';

let packetCount = 0;

function logPacket(direction, data) {
    const timestamp = new Date().toISOString();
    const hex = data.toString('hex').match(/.{1,2}/g).join(' ');
    const ascii = data.toString('utf8').replace(/[^\x20-\x7E]/g, '.');
    
    const log = `
[${timestamp}] #${++packetCount} ${direction}
长度: ${data.length} bytes
HEX: ${hex.substring(0, 200)}${hex.length > 200 ? '...' : ''}
ASCII: ${ascii.substring(0, 100)}${ascii.length > 100 ? '...' : ''}
---`;
    
    console.log(log);
    fs.appendFileSync(LOG_FILE, log + '\n');
}

// 创建代理服务器
const proxy = net.createServer((clientSocket) => {
    console.log('客户端连接');
    
    // 连接到真正的xclient
    const targetSocket = net.createConnection(XCLIENT_PORT, '127.0.0.1', () => {
        console.log('已连接到xclient');
    });
    
    // 客户端 -> xclient
    clientSocket.on('data', (data) => {
        logPacket('客户端 -> xclient', data);
        targetSocket.write(data);
    });
    
    // xclient -> 客户端
    targetSocket.on('data', (data) => {
        logPacket('xclient -> 客户端', data);
        clientSocket.write(data);
    });
    
    clientSocket.on('close', () => {
        console.log('客户端断开');
        targetSocket.end();
    });
    
    targetSocket.on('close', () => {
        console.log('xclient断开');
        clientSocket.end();
    });
    
    clientSocket.on('error', (e) => console.log('客户端错误:', e.message));
    targetSocket.on('error', (e) => console.log('xclient错误:', e.message));
});

proxy.listen(PROXY_PORT, '127.0.0.1', () => {
    console.log(`代理监听 127.0.0.1:${PROXY_PORT}`);
    console.log(`将流量转发到 127.0.0.1:${XCLIENT_PORT}`);
    console.log(`日志保存到: ${LOG_FILE}`);
    console.log('\n等待连接...');
});

// 同时测试直接连接并发送数据
setTimeout(() => {
    console.log('\n[主动测试xclient协议]');
    
    const testData = [
        // 格式1: Type + Flags + Length(BE) + JSON
        Buffer.concat([
            Buffer.from([0x01, 0x00]), // type=1, flags=0
            Buffer.from([0x00, 0x00, 0x00, 0x14]), // length=20 BE
            Buffer.from('{"action":"buildin"}')
        ]),
        // 格式2: 纯JSON
        Buffer.from('{"type":"buildin"}\n'),
    ];
    
    testData.forEach((data, i) => {
        const client = new net.Socket();
        client.connect(XCLIENT_PORT, '127.0.0.1', () => {
            console.log(`\n测试 ${i+1}: 发送 ${data.length} bytes`);
            console.log('HEX:', data.toString('hex'));
            client.write(data);
        });
        
        client.on('data', (response) => {
            console.log(`测试 ${i+1} 响应: ${response.length} bytes`);
            console.log('HEX:', response.toString('hex'));
            
            // 解析响应
            if (response.length >= 6) {
                const type = response[0];
                const flags = response[1];
                const len = response.readUInt32BE(2);
                console.log(`  Type=${type}, Flags=${flags}, Len=${len}`);
                if (response.length > 6) {
                    console.log('  Payload:', response.slice(6).toString('utf8'));
                }
            }
            client.end();
        });
        
        client.on('error', (e) => console.log(`测试 ${i+1} 错误:`, e.message));
        
        setTimeout(() => client.end(), 2000);
    });
}, 1000);
