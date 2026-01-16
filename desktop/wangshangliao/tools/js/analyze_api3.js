const fs = require('fs');

// 读取两个主要文件
const indexContent = fs.readFileSync('C:\\Program Files (x86)\\wangshangliao_win_online\\resources\\app\\dist\\assets\\index-c221f02a.js', 'utf8');
const zhContent = fs.readFileSync('C:\\Program Files (x86)\\wangshangliao_win_online\\resources\\app\\dist\\assets\\zh-cn-acff1ed5.js', 'utf8');

const content = indexContent + zhContent;

// 搜索所有常见SDK操作
console.log('=== Message Operations ===');
const msgOps = ['sendText', 'sendImage', 'sendFile', 'sendAudio', 'sendVideo', 'sendCustomMsg', 'sendTipMsg', 'forwardMsg', 'revokeMsg', 'deleteMsg'];
msgOps.forEach(op => {
    const count = (content.match(new RegExp(op, 'g')) || []).length;
    if (count > 0) console.log(op + ': ' + count);
});

console.log('\n=== Team/Group Operations ===');
const teamOps = ['getTeam', 'getTeamMembers', 'createTeam', 'dismissTeam', 'leaveTeam', 'transferTeam', 
    'addTeamMembers', 'removeTeamMembers', 'kickTeamMembers', 'muteTeamAll', 'muteTeamMember',
    'updateTeam', 'updateTeamMemberNick', 'addTeamManagers', 'removeTeamManagers'];
teamOps.forEach(op => {
    const count = (content.match(new RegExp(op, 'g')) || []).length;
    if (count > 0) console.log(op + ': ' + count);
});

console.log('\n=== User Operations ===');
const userOps = ['getUser', 'getUsers', 'updateMyInfo', 'getMyInfo'];
userOps.forEach(op => {
    const count = (content.match(new RegExp(op, 'g')) || []).length;
    if (count > 0) console.log(op + ': ' + count);
});

console.log('\n=== Friend Operations ===');
const friendOps = ['addFriend', 'deleteFriend', 'updateFriend', 'applyFriend', 'passFriendApply', 'rejectFriendApply', 'getFriends'];
friendOps.forEach(op => {
    const count = (content.match(new RegExp(op, 'g')) || []).length;
    if (count > 0) console.log(op + ': ' + count);
});

console.log('\n=== Session Operations ===');
const sessionOps = ['getLocalSession', 'getLocalSessions', 'deleteLocalSession', 'resetSessionUnread', 'getServerSessions'];
sessionOps.forEach(op => {
    const count = (content.match(new RegExp(op, 'g')) || []).length;
    if (count > 0) console.log(op + ': ' + count);
});

console.log('\n=== Encryption Functions ===');
const encOps = ['encrypt', 'decrypt', 'AES', 'CryptoJS', 'decryptNick', 'decryptTeamNick', 'AES_decryptNick'];
encOps.forEach(op => {
    const count = (content.match(new RegExp(op, 'g')) || []).length;
    if (count > 0) console.log(op + ': ' + count);
});

// 搜索AppKey
console.log('\n=== AppKey Search ===');
const appKeyMatch = content.match(/appKey['"]*\s*[:=]\s*['"]([a-f0-9]{32})['"]/i);
if (appKeyMatch) {
    console.log('AppKey:', appKeyMatch[1]);
}

// 搜索WebSocket URL
console.log('\n=== WebSocket URLs ===');
const wsUrls = content.match(/wss?:\/\/[^'"]+/g);
if (wsUrls) {
    [...new Set(wsUrls)].forEach(url => console.log(url.substring(0, 80)));
}
