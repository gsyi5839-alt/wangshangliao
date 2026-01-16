/**
 * 深度提取API参数和调用签名
 */
const WebSocket = require('ws');

const WS_URL = 'ws://127.0.0.1:9222/devtools/page/8322EEB7A02952E8C4C59B59B616C299';
const TEAM_ID = '40821608989';

let messageId = 1;
let ws;

function send(expression) {
    return new Promise((resolve, reject) => {
        const id = messageId++;
        const timeout = setTimeout(() => reject(new Error('Timeout')), 30000);
        
        const handler = (data) => {
            const response = JSON.parse(data);
            if (response.id === id) {
                clearTimeout(timeout);
                ws.off('message', handler);
                resolve(response);
            }
        };
        
        ws.on('message', handler);
        ws.send(JSON.stringify({
            id,
            method: 'Runtime.evaluate',
            params: {
                expression,
                returnByValue: true,
                awaitPromise: true
            }
        }));
    });
}

async function main() {
    ws = new WebSocket(WS_URL);
    await new Promise(resolve => ws.on('open', resolve));
    
    console.log('='.repeat(80));
    console.log('深度API参数提取');
    console.log('='.repeat(80));
    
    // 1. 测试 updateTeam 参数
    console.log('\n【1. updateTeam 可用参数】\n');
    
    const updateTeamTest = await send(`
        (function() {
            return new Promise((resolve) => {
                // 测试获取群详情后分析可修改字段
                window.nim.getTeam({
                    teamId: '${TEAM_ID}',
                    done: (err, team) => {
                        if (err) {
                            resolve({ error: err.message });
                            return;
                        }
                        
                        // 可更新的字段
                        const updatableFields = [
                            'name',           // 群名称
                            'avatar',         // 群头像
                            'intro',          // 群介绍
                            'announcement',   // 群公告
                            'joinMode',       // 入群方式: noVerify, needVerify, rejectAll
                            'beInviteMode',   // 被邀请方式: needVerify, noVerify
                            'inviteMode',     // 邀请权限: manager, all
                            'updateTeamMode', // 谁可以修改群资料: manager, all
                            'updateCustomMode',// 谁可以修改自定义: manager, all
                            'custom'          // 自定义字段
                        ];
                        
                        const currentValues = {};
                        updatableFields.forEach(f => {
                            currentValues[f] = team[f];
                        });
                        
                        resolve({
                            teamId: team.teamId,
                            updatableFields,
                            currentValues
                        });
                    }
                });
            });
        })()
    `);
    
    if (updateTeamTest.result?.result?.value) {
        console.log(JSON.stringify(updateTeamTest.result.result.value, null, 2));
    }

    // 2. 测试 updateInfoInTeam 可用参数
    console.log('\n【2. updateInfoInTeam 可用参数】\n');
    
    const updateInfoTest = await send(`
        (function() {
            // 这个API用于修改自己在群内的设置
            // muteTeam: 消息免打扰
            // muteNotiType: 通知类型
            return {
                api: 'updateInfoInTeam',
                params: {
                    teamId: '群ID (必须)',
                    muteTeam: 'boolean - 消息免打扰 true/false',
                    muteNotiType: '0=接收全部, 1=仅@我, 2=不接收',
                    custom: 'string - 自定义字段',
                    done: 'callback(err, result)'
                },
                example: "nim.updateInfoInTeam({ teamId: 'xxx', muteTeam: true, done: cb })"
            };
        })()
    `);
    
    if (updateInfoTest.result?.result?.value) {
        console.log(JSON.stringify(updateInfoTest.result.result.value, null, 2));
    }

    // 3. 测试 muteTeamAll 详细参数
    console.log('\n【3. muteTeamAll 详细测试】\n');
    
    const muteAllTest = await send(`
        (function() {
            return {
                api: 'muteTeamAll',
                params: {
                    teamId: 'string - 群ID (必须)',
                    mute: 'boolean - true禁言, false解禁 (必须)',
                    done: 'callback(err, result)'
                },
                example: "nim.muteTeamAll({ teamId: '${TEAM_ID}', mute: true, done: cb })",
                note: '需要群主或管理员权限'
            };
        })()
    `);
    
    if (muteAllTest.result?.result?.value) {
        console.log(JSON.stringify(muteAllTest.result.result.value, null, 2));
    }

    // 4. 测试 updateMuteStateInTeam 详细参数
    console.log('\n【4. updateMuteStateInTeam 详细测试】\n');
    
    const muteStateTest = await send(`
        (function() {
            return {
                api: 'updateMuteStateInTeam',
                params: {
                    teamId: 'string - 群ID (必须)',
                    account: 'string - 成员账号 (必须)',
                    mute: 'boolean - true禁言, false解禁 (必须)',
                    done: 'callback(err, result)'
                },
                example: "nim.updateMuteStateInTeam({ teamId: 'xxx', account: 'yyy', mute: true, done: cb })",
                note: '需要群主或管理员权限'
            };
        })()
    `);
    
    if (muteStateTest.result?.result?.value) {
        console.log(JSON.stringify(muteStateTest.result.result.value, null, 2));
    }

    // 5. 测试 sendText 详细参数
    console.log('\n【5. sendText 完整参数】\n');
    
    const sendTextTest = await send(`
        (function() {
            return {
                api: 'sendText',
                params: {
                    scene: "'p2p' 或 'team' (必须)",
                    to: 'string - 目标ID (必须, p2p为账号, team为群ID)',
                    text: 'string - 消息内容 (必须)',
                    custom: 'string - 自定义扩展字段 (可选)',
                    pushContent: 'string - 推送文案 (可选)',
                    pushPayload: 'string - 推送payload (可选)',
                    needPushNick: 'boolean - 推送是否需要昵称 (可选)',
                    needMsgReceipt: 'boolean - 是否需要已读回执 (可选)',
                    isHistoryable: 'boolean - 是否存云端历史 (可选)',
                    isRoamingable: 'boolean - 是否支持漫游 (可选)',
                    isUnreadable: 'boolean - 是否计入未读 (可选)',
                    isSyncable: 'boolean - 是否同步到其他端 (可选)',
                    isPushable: 'boolean - 是否需要推送 (可选)',
                    isOfflinable: 'boolean - 是否支持离线 (可选)',
                    antiSpamOption: 'object - 反垃圾选项 (可选)',
                    done: 'callback(err, msg) (必须)'
                },
                example: "nim.sendText({ scene: 'team', to: '${TEAM_ID}', text: 'Hello', done: cb })"
            };
        })()
    `);
    
    if (sendTextTest.result?.result?.value) {
        console.log(JSON.stringify(sendTextTest.result.result.value, null, 2));
    }

    // 6. 测试 passTeamApply 参数
    console.log('\n【6. passTeamApply 完整参数】\n');
    
    const passApplyTest = await send(`
        (function() {
            return {
                api: 'passTeamApply',
                params: {
                    teamId: 'string - 群ID (必须)',
                    from: 'string - 申请人账号 (必须)',
                    idServer: 'string - 服务端ID (必须, 从系统消息获取)',
                    ps: 'string - 附言 (可选)',
                    done: 'callback(err, result)'
                },
                systemMsgFormat: {
                    type: "'applyTeam'",
                    from: '申请人账号',
                    to: '群主账号',
                    teamId: '群ID',
                    idServer: '用于通过/拒绝的服务端ID'
                },
                example: "nim.passTeamApply({ teamId: 'xxx', from: 'yyy', idServer: 'zzz', done: cb })"
            };
        })()
    `);
    
    if (passApplyTest.result?.result?.value) {
        console.log(JSON.stringify(passApplyTest.result.result.value, null, 2));
    }

    // 7. 测试 getHistoryMsgs 参数
    console.log('\n【7. getHistoryMsgs 完整参数】\n');
    
    const historyMsgsTest = await send(`
        (function() {
            return {
                api: 'getHistoryMsgs',
                params: {
                    scene: "'p2p' 或 'team' (必须)",
                    to: 'string - 目标ID (必须)',
                    beginTime: 'number - 开始时间戳 (可选)',
                    endTime: 'number - 结束时间戳 (可选)',
                    lastMsgId: 'string - 最后一条消息ID (可选, 用于分页)',
                    limit: 'number - 获取数量 (可选, 默认100)',
                    reverse: 'boolean - 是否倒序 (可选)',
                    msgTypes: 'array - 消息类型过滤 (可选)',
                    done: 'callback(err, { msgs })'
                },
                example: "nim.getHistoryMsgs({ scene: 'team', to: '${TEAM_ID}', limit: 50, done: cb })"
            };
        })()
    `);
    
    if (historyMsgsTest.result?.result?.value) {
        console.log(JSON.stringify(historyMsgsTest.result.result.value, null, 2));
    }

    // 8. 实际调用 getHistoryMsgs 测试
    console.log('\n【8. getHistoryMsgs 实际调用】\n');
    
    const historyResult = await send(`
        (function() {
            return new Promise((resolve) => {
                window.nim.getHistoryMsgs({
                    scene: 'team',
                    to: '${TEAM_ID}',
                    limit: 5,
                    done: (err, result) => {
                        if (err) {
                            resolve({ error: err.message });
                            return;
                        }
                        
                        const msgs = (result.msgs || []).map(m => ({
                            type: m.type,
                            from: m.from,
                            text: m.text?.substring(0, 50),
                            time: m.time,
                            idServer: m.idServer
                        }));
                        
                        resolve({
                            count: result.msgs?.length || 0,
                            msgs
                        });
                    }
                });
            });
        })()
    `);
    
    if (historyResult.result?.result?.value) {
        console.log(JSON.stringify(historyResult.result.result.value, null, 2));
    }

    // 9. 探索 recallMsg 参数
    console.log('\n【9. recallMsg 完整参数】\n');
    
    const recallMsgTest = await send(`
        (function() {
            return {
                api: 'recallMsg',
                params: {
                    msg: 'object - 要撤回的消息对象 (必须)',
                    msgProperties: {
                        idClient: '消息客户端ID',
                        idServer: '消息服务端ID',
                        scene: '场景',
                        to: '目标'
                    },
                    ps: 'string - 附言 (可选)',
                    done: 'callback(err, result)'
                },
                restrictions: [
                    '群消息: 管理员可撤回他人消息',
                    '私聊消息: 只能撤回自己发送的',
                    '时间限制: 2分钟内'
                ],
                example: "nim.recallMsg({ msg: msgObj, done: cb })"
            };
        })()
    `);
    
    if (recallMsgTest.result?.result?.value) {
        console.log(JSON.stringify(recallMsgTest.result.result.value, null, 2));
    }

    // 10. 探索 addTeamManagers / removeTeamManagers 参数
    console.log('\n【10. 管理员操作 API】\n');
    
    const managerTest = await send(`
        (function() {
            return {
                addTeamManagers: {
                    params: {
                        teamId: 'string - 群ID (必须)',
                        accounts: 'array - 账号列表 (必须)',
                        done: 'callback(err, result)'
                    },
                    example: "nim.addTeamManagers({ teamId: 'xxx', accounts: ['a', 'b'], done: cb })"
                },
                removeTeamManagers: {
                    params: {
                        teamId: 'string - 群ID (必须)',
                        accounts: 'array - 账号列表 (必须)',
                        done: 'callback(err, result)'
                    },
                    example: "nim.removeTeamManagers({ teamId: 'xxx', accounts: ['a'], done: cb })"
                },
                note: '只有群主可以操作'
            };
        })()
    `);
    
    if (managerTest.result?.result?.value) {
        console.log(JSON.stringify(managerTest.result.result.value, null, 2));
    }

    // 11. 探索 updateNickInTeam 参数
    console.log('\n【11. updateNickInTeam 参数】\n');
    
    const nickTest = await send(`
        (function() {
            return {
                api: 'updateNickInTeam',
                params: {
                    teamId: 'string - 群ID (必须)',
                    account: 'string - 成员账号 (可选, 不填则修改自己的)',
                    nickInTeam: 'string - 新昵称 (必须)',
                    done: 'callback(err, result)'
                },
                permissions: {
                    self: '可以修改自己的昵称',
                    others: '管理员可修改普通成员的昵称',
                    owner: '群主可修改任何人的昵称'
                },
                example: "nim.updateNickInTeam({ teamId: 'xxx', account: 'yyy', nickInTeam: '新昵称', done: cb })"
            };
        })()
    `);
    
    if (nickTest.result?.result?.value) {
        console.log(JSON.stringify(nickTest.result.result.value, null, 2));
    }

    // 12. 探索 removeTeamMembers 参数
    console.log('\n【12. removeTeamMembers 参数】\n');
    
    const removeTest = await send(`
        (function() {
            return {
                api: 'removeTeamMembers',
                params: {
                    teamId: 'string - 群ID (必须)',
                    accounts: 'array - 要移除的账号列表 (必须)',
                    done: 'callback(err, result)'
                },
                permissions: {
                    manager: '管理员可踢普通成员',
                    owner: '群主可踢任何人(除自己外)'
                },
                example: "nim.removeTeamMembers({ teamId: 'xxx', accounts: ['a', 'b'], done: cb })"
            };
        })()
    `);
    
    if (removeTest.result?.result?.value) {
        console.log(JSON.stringify(removeTest.result.result.value, null, 2));
    }

    // 13. 探索 sendFile/previewFile 参数
    console.log('\n【13. sendFile / previewFile 参数】\n');
    
    const fileTest = await send(`
        (function() {
            return {
                previewFile: {
                    params: {
                        type: "'image' | 'audio' | 'video' | 'file' (必须)",
                        blob: 'Blob - 文件Blob对象 (必须)',
                        uploadprogress: 'callback - 上传进度回调 (可选)',
                        done: 'callback(err, { fileObj })'
                    },
                    returnFields: {
                        fileObj: {
                            name: '文件名',
                            size: '文件大小',
                            md5: '文件MD5',
                            url: '上传后的URL',
                            ext: '文件扩展名'
                        }
                    }
                },
                sendFile: {
                    params: {
                        scene: "'p2p' 或 'team' (必须)",
                        to: 'string - 目标ID (必须)',
                        type: "'image' | 'audio' | 'video' | 'file' (必须)",
                        file: 'object - previewFile返回的fileObj (必须)',
                        blob: 'Blob - 或直接传Blob (可选)',
                        done: 'callback(err, msg)'
                    }
                },
                recommendedFlow: [
                    '1. 调用 previewFile 上传文件到服务器',
                    '2. 获取 fileObj 包含 URL',
                    '3. 调用 sendFile 发送文件消息'
                ]
            };
        })()
    `);
    
    if (fileTest.result?.result?.value) {
        console.log(JSON.stringify(fileTest.result.result.value, null, 2));
    }

    ws.close();
    console.log('\n' + '='.repeat(80));
    console.log('参数提取完成！');
    console.log('='.repeat(80));
}

main().catch(console.error);
