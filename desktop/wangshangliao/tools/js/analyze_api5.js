const fs = require('fs');

const indexContent = fs.readFileSync('C:\\Program Files (x86)\\wangshangliao_win_online\\resources\\app\\dist\\assets\\index-c221f02a.js', 'utf8');
const zhContent = fs.readFileSync('C:\\Program Files (x86)\\wangshangliao_win_online\\resources\\app\\dist\\assets\\zh-cn-acff1ed5.js', 'utf8');
const content = indexContent + zhContent;

console.log('=== Electron IPC Messages ===');
// 搜索ipcRenderer.send 和 invoke
const ipcSendMatches = content.match(/ipcRenderer\.(send|invoke)\s*\(\s*['"]([^'"]+)['"]/g);
if (ipcSendMatches) {
    const uniqueIpc = [...new Set(ipcSendMatches)];
    uniqueIpc.forEach(m => console.log(m));
}

console.log('\n=== Window Operations ===');
const winOps = ['openWindow', 'closeWindow', 'minimize', 'maximize', 'setAlwaysOnTop'];
winOps.forEach(op => {
    const count = (content.match(new RegExp(op, 'g')) || []).length;
    if (count > 0) console.log(op + ': ' + count);
});

console.log('\n=== Notification Operations ===');
const notifyOps = ['showNotification', 'pushNotification', 'playSound', 'badge'];
notifyOps.forEach(op => {
    const count = (content.match(new RegExp(op, 'gi')) || []).length;
    if (count > 0) console.log(op + ': ' + count);
});

console.log('\n=== Storage Operations ===');
const storageOps = ['localStorage', 'sessionStorage', 'indexedDB', 'getItem', 'setItem', 'removeItem'];
storageOps.forEach(op => {
    const count = (content.match(new RegExp(op, 'g')) || []).length;
    if (count > 0) console.log(op + ': ' + count);
});

console.log('\n=== Connection/Network Operations ===');
const netOps = ['connect', 'disconnect', 'reconnect', 'online', 'offline', 'network'];
netOps.forEach(op => {
    const count = (content.match(new RegExp(op, 'gi')) || []).length;
    if (count > 0) console.log(op + ': ' + count);
});

console.log('\n=== Login/Auth Operations ===');
const authOps = ['login', 'logout', 'register', 'auth', 'token', 'password', 'verify'];
authOps.forEach(op => {
    const count = (content.match(new RegExp(op, 'gi')) || []).length;
    if (count > 0) console.log(op + ': ' + count);
});
