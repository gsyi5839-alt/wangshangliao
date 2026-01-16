const fs = require('fs');

const indexContent = fs.readFileSync('C:\\Program Files (x86)\\wangshangliao_win_online\\resources\\app\\dist\\assets\\index-c221f02a.js', 'utf8');
const zhContent = fs.readFileSync('C:\\Program Files (x86)\\wangshangliao_win_online\\resources\\app\\dist\\assets\\zh-cn-acff1ed5.js', 'utf8');
const content = indexContent + zhContent;

console.log('=== RTC/Calling Operations ===');
const rtcOps = ['signaling', 'rtc', 'calling', 'joinChannel', 'leaveChannel', 'startCall', 'endCall', 'acceptCall', 'rejectCall'];
rtcOps.forEach(op => {
    const count = (content.match(new RegExp(op, 'gi')) || []).length;
    if (count > 0) console.log(op + ': ' + count);
});

console.log('\n=== Blacklist Operations ===');
const blackOps = ['markInBlacklist', 'blacklist', 'getBlacklist', 'addToBlacklist', 'removeFromBlacklist'];
blackOps.forEach(op => {
    const count = (content.match(new RegExp(op, 'g')) || []).length;
    if (count > 0) console.log(op + ': ' + count);
});

console.log('\n=== System Message Operations ===');
const sysMsgOps = ['sysMsg', 'systemMsg', 'notification', 'onSysMsg', 'getLocalSysMsg'];
sysMsgOps.forEach(op => {
    const count = (content.match(new RegExp(op, 'g')) || []).length;
    if (count > 0) console.log(op + ': ' + count);
});

console.log('\n=== File/Media Operations ===');
const fileOps = ['uploadFile', 'downloadFile', 'previewFile', 'getFile', 'fileProgress'];
fileOps.forEach(op => {
    const count = (content.match(new RegExp(op, 'g')) || []).length;
    if (count > 0) console.log(op + ': ' + count);
});

console.log('\n=== Message Receipt Operations ===');
const receiptOps = ['sendMsgReceipt', 'getMsgReceipts', 'teamMsgReceipt', 'msgReceipt'];
receiptOps.forEach(op => {
    const count = (content.match(new RegExp(op, 'g')) || []).length;
    if (count > 0) console.log(op + ': ' + count);
});

console.log('\n=== Search Operations ===');
const searchOps = ['searchMsg', 'searchTeam', 'searchUser', 'searchLocal', 'searchHistory'];
searchOps.forEach(op => {
    const count = (content.match(new RegExp(op, 'g')) || []).length;
    if (count > 0) console.log(op + ': ' + count);
});

console.log('\n=== Moment/Circle Operations ===');
const momentOps = ['moment', 'circle', 'feed', 'comment', 'like'];
momentOps.forEach(op => {
    const count = (content.match(new RegExp(op, 'gi')) || []).length;
    if (count > 0) console.log(op + ': ' + count);
});

console.log('\n=== Pin/Stick Operations ===');
const pinOps = ['stickTop', 'pin', 'unpin', 'addStickTopSession', 'deleteStickTopSession'];
pinOps.forEach(op => {
    const count = (content.match(new RegExp(op, 'g')) || []).length;
    if (count > 0) console.log(op + ': ' + count);
});
