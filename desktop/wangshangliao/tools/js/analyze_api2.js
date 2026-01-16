const fs = require('fs');

// 读取zh-cn文件
const content = fs.readFileSync('C:\\Program Files (x86)\\wangshangliao_win_online\\resources\\app\\dist\\assets\\zh-cn-acff1ed5.js', 'utf8');

// 搜索所有nim.xxx(调用
const nimMethods = new Set();
const nimRegex = /nim\.(\w+)\s*\(/g;
let match;
while ((match = nimRegex.exec(content)) !== null) {
    nimMethods.add(match[1]);
}

console.log('=== NIM SDK Methods in zh-cn (' + nimMethods.size + ') ===');
console.log([...nimMethods].sort().join('\n'));

// 搜索useSdkStore的actions
const sdkStoreMatch = content.match(/useSdkStore\s*=\s*defineStore\([^)]+,\s*\{[^}]+state:[^}]+\}[^}]+actions:\s*\{([^]*?)\}\s*\}\)/);
if (sdkStoreMatch) {
    console.log('\n=== useSdkStore actions found ===');
    // 简单提取函数名
    const actionsContent = sdkStoreMatch[1];
    const funcNames = actionsContent.match(/async\s+(\w+)|(\w+)\s*\([^)]*\)\s*\{/g);
    if (funcNames) {
        console.log('Actions:', funcNames.slice(0, 20).join(', '));
    }
}

// 搜索全局函数
console.log('\n=== Global Functions containing "decrypt" ===');
const decryptFuncs = content.match(/(\w+)\s*=\s*(?:\w+\s*=>|function)\s*[^;]*decrypt/g);
if (decryptFuncs) {
    decryptFuncs.forEach(f => console.log(f.substring(0, 80)));
}
