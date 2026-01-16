const fs = require('fs');

// 读取主JS文件
const content = fs.readFileSync('C:\\Program Files (x86)\\wangshangliao_win_online\\resources\\app\\dist\\assets\\index-c221f02a.js', 'utf8');

// 搜索所有nim.xxx(调用
const nimMethods = new Set();
const nimRegex = /nim\.(\w+)\s*\(/g;
let match;
while ((match = nimRegex.exec(content)) !== null) {
    nimMethods.add(match[1]);
}

console.log('=== All NIM SDK Methods (' + nimMethods.size + ') ===');
console.log([...nimMethods].sort().join('\n'));

// 搜索所有事件监听
console.log('\n=== Event Listeners ===');
const eventRegex = /nim\.on\s*\(\s*['"](\w+)['"]/g;
const events = new Set();
while ((match = eventRegex.exec(content)) !== null) {
    events.add(match[1]);
}
console.log([...events].sort().join('\n'));
