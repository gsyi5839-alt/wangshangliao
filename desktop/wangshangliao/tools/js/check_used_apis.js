/**
 * 检查WangShangLiaoBot软件使用的API与336个可用API的对比
 */
const fs = require('fs');
const path = require('path');

// 336个可用API
const allApis = {
    team: [
        'acceptTeamInvite', 'addTeamManagers', 'addTeamMembers', 'addTeamMembersFollow',
        'applyTeam', 'assembleTeamMembers', 'assembleTeamOwner', 'createTeam',
        'cutTeamMembers', 'cutTeamMembersByAccounts', 'cutTeams', 'deleteLocalTeam',
        'dismissTeam', 'findTeam', 'findTeamMember', 'genTeamMemberId',
        'getLocalTeamMembers', 'getLocalTeams', 'getMutedTeamMembers', 'getMyTeamMembers',
        'getTeam', 'getTeamMemberByTeamIdAndAccount', 'getTeamMemberInvitorAccid',
        'getTeamMembers', 'getTeamMembersFromDB', 'getTeamMsgReadAccounts', 'getTeamMsgReads',
        'getTeams', 'getTeamsById', 'getTeamsFromDB', 'leaveTeam', 'mergeTeamMembers',
        'mergeTeams', 'muteTeamAll', 'notifyForNewTeamMsg', 'passTeamApply',
        'rejectTeamApply', 'rejectTeamInvite', 'removeTeamManagers', 'removeTeamMembers',
        'removeTeamMembersFollow', 'sendTeamMsgReceipt', 'transferTeam',
        'updateInfoInTeam', 'updateMuteStateInTeam', 'updateNickInTeam', 'updateTeam'
    ],
    msg: [
        'sendText', 'sendFile', 'sendCustomMsg', 'sendTipMsg', 'sendGeo', 'sendRobotMsg',
        'recallMsg', 'forwardMsg', 'resendMsg', 'deleteMsg', 'deleteLocalMsg',
        'deleteMsgSelf', 'deleteMsgSelfBatch', 'getHistoryMsgs', 'getLocalMsgs',
        'getLocalMsgByIdClient', 'getLocalMsgsByIdClients', 'sendMsgReceipt',
        'markMsgRead', 'getMsgPins', 'addMsgPin', 'deleteMsgPin', 'updateMsgPin',
        'previewFile'
    ],
    user: [
        'getMyInfo', 'updateMyInfo', 'getUser', 'getUsers', 'getUsersFromDB',
        'findUser', 'isUserInBlackList'
    ],
    friend: [
        'getFriends', 'addFriend', 'deleteFriend', 'updateFriend',
        'addToBlacklist', 'removeFromBlacklist', 'addToMutelist', 'removeFromMutelist'
    ],
    session: [
        'getLocalSessions', 'getLocalSession', 'setCurrSession', 'resetCurrSession',
        'resetSessionUnread', 'resetAllSessionUnread', 'deleteSession',
        'addStickTopSession', 'deleteStickTopSession'
    ]
};

// 读取ChatService.cs文件
const chatServicePath = path.join(__dirname, 'src/WangShangLiaoBot/Services/ChatService.cs');
const content = fs.readFileSync(chatServicePath, 'utf-8');

// 检查每个API是否被使用
const usedApis = new Set();
const notUsedApis = [];

console.log('='.repeat(80));
console.log('WangShangLiaoBot 软件 API 使用情况分析');
console.log('='.repeat(80));

// 搜索所有API
const allApiList = Object.values(allApis).flat();

allApiList.forEach(api => {
    // 搜索多种可能的引用方式
    const patterns = [
        `nim.${api}`,
        `'${api}'`,
        `"${api}"`,
        api
    ];
    
    let found = false;
    for (const pattern of patterns) {
        if (content.includes(pattern)) {
            found = true;
            break;
        }
    }
    
    if (found) {
        usedApis.add(api);
    } else {
        notUsedApis.push(api);
    }
});

// 按分类输出
console.log('\n【已使用的API】\n');

for (const [category, apis] of Object.entries(allApis)) {
    const used = apis.filter(api => usedApis.has(api));
    const notUsed = apis.filter(api => !usedApis.has(api));
    
    console.log(`\n${category.toUpperCase()} (${used.length}/${apis.length}):`);
    console.log('  ✅ 已使用:', used.join(', ') || '无');
    console.log('  ❌ 未使用:', notUsed.join(', ') || '无');
}

// 汇总
console.log('\n' + '='.repeat(80));
console.log('【汇总】');
console.log('='.repeat(80));
console.log(`\n  总API数: ${allApiList.length}`);
console.log(`  已使用: ${usedApis.size} (${(usedApis.size/allApiList.length*100).toFixed(1)}%)`);
console.log(`  未使用: ${notUsedApis.length} (${(notUsedApis.length/allApiList.length*100).toFixed(1)}%)`);

// 建议添加的API
console.log('\n【建议添加的API】\n');
const recommendations = [
    { api: 'updateNickInTeam', use: '修改群成员昵称', priority: '高' },
    { api: 'updateMuteStateInTeam', use: '单人禁言', priority: '高' },
    { api: 'addTeamManagers', use: '设置管理员', priority: '中' },
    { api: 'removeTeamManagers', use: '取消管理员', priority: '中' },
    { api: 'getHistoryMsgs', use: '获取历史消息', priority: '中' },
    { api: 'getMutedTeamMembers', use: '获取禁言成员列表', priority: '中' },
    { api: 'forwardMsg', use: '转发消息', priority: '低' },
    { api: 'addToBlacklist', use: '添加黑名单', priority: '低' },
    { api: 'notifyForNewTeamMsg', use: '消息通知设置', priority: '低' }
];

recommendations.forEach(r => {
    const status = usedApis.has(r.api) ? '✅' : '❌';
    console.log(`  ${status} ${r.api} - ${r.use} [${r.priority}]`);
});

console.log('\n' + '='.repeat(80));
