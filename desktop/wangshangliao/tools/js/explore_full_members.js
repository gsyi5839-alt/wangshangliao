// WangShangLiao Full Members & Message Explorer
// Run with: node explore_full_members.js

const http = require('http');
const WebSocket = require('ws');
const fs = require('fs');

const DEBUG_PORT = 9222;

async function getTargets() {
    return new Promise((resolve, reject) => {
        http.get(`http://localhost:${DEBUG_PORT}/json`, (res) => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', () => resolve(JSON.parse(data)));
        }).on('error', reject);
    });
}

async function connectWS(wsUrl) {
    return new Promise((resolve, reject) => {
        const ws = new WebSocket(wsUrl);
        ws.on('open', () => resolve(ws));
        ws.on('error', reject);
    });
}

async function sendCDP(ws, method, params = {}) {
    return new Promise((resolve, reject) => {
        const id = Math.floor(Math.random() * 100000);
        
        const handler = (data) => {
            const msg = JSON.parse(data);
            if (msg.id === id) {
                ws.removeListener('message', handler);
                resolve(msg);
            }
        };
        
        ws.on('message', handler);
        ws.send(JSON.stringify({ id, method, params }));
        
        setTimeout(() => {
            ws.removeListener('message', handler);
            reject(new Error('Timeout'));
        }, 60000);
    });
}

async function evaluate(ws, expression, awaitPromise = false) {
    const result = await sendCDP(ws, 'Runtime.evaluate', {
        expression,
        returnByValue: true,
        awaitPromise
    });
    
    if (result.result && result.result.result) {
        const val = result.result.result.value;
        if (typeof val === 'string') {
            try {
                return JSON.parse(val);
            } catch {
                return val;
            }
        }
        return val;
    }
    return null;
}

async function main() {
    console.log('=== WangShangLiao Full Members Explorer ===');
    console.log('Time:', new Date().toISOString());
    
    const targets = await getTargets();
    const target = targets.find(t => t.type === 'page');
    if (!target) {
        console.error('No page target found');
        process.exit(1);
    }
    
    console.log('Target:', target.title);
    
    // Get team ID from URL
    const urlMatch = target.url.match(/sessionId=team-(\d+)/);
    const teamId = urlMatch ? urlMatch[1] : null;
    console.log('Team ID from URL:', teamId);
    
    if (!teamId) {
        console.error('Cannot find team ID in URL');
        process.exit(1);
    }
    
    const ws = await connectWS(target.webSocketDebuggerUrl);
    console.log('WebSocket connected!');
    
    const allResults = {
        teamId: teamId,
        timestamp: new Date().toISOString()
    };
    
    // ============================================
    // PART 1: Get ALL Team Members
    // ============================================
    console.log('\n=== PART 1: Get ALL Team Members ===');
    
    const membersData = await evaluate(ws, `
        (async function() {
            var result = {
                success: false,
                error: null,
                totalCount: 0,
                members: []
            };
            
            if (!window.nim) {
                result.error = 'nim not found';
                return JSON.stringify(result);
            }
            
            try {
                var data = await new Promise(function(resolve, reject) {
                    window.nim.getTeamMembers({
                        teamId: '${teamId}',
                        done: function(err, obj) {
                            if (err) reject(err);
                            else resolve(obj);
                        }
                    });
                    setTimeout(function() { reject(new Error('Timeout')); }, 30000);
                });
                
                result.success = true;
                var members = data.members || data || [];
                result.totalCount = members.length;
                
                // Get all member details
                result.members = members.map(function(m) {
                    return {
                        account: m.account,
                        nickInTeam: m.nickInTeam,
                        type: m.type,
                        joinTime: m.joinTime,
                        updateTime: m.updateTime,
                        active: m.active,
                        valid: m.valid,
                        mute: m.mute,
                        invitorAccid: m.invitorAccid,
                        custom: m.custom
                    };
                });
            } catch (e) {
                result.error = e.message || String(e);
            }
            
            return JSON.stringify(result);
        })();
    `, true);
    
    allResults.members = membersData;
    console.log('Success:', membersData?.success);
    console.log('Total Members:', membersData?.totalCount);
    
    if (membersData?.success && membersData.members) {
        // Show member type distribution
        const typeCount = {};
        membersData.members.forEach(m => {
            typeCount[m.type] = (typeCount[m.type] || 0) + 1;
        });
        console.log('\nMember Type Distribution:', typeCount);
        
        // Show first 10 members
        console.log('\nFirst 10 members:');
        membersData.members.slice(0, 10).forEach((m, i) => {
            console.log(`  ${i + 1}. ${m.nickInTeam || m.account} (${m.account}) - ${m.type}`);
        });
    }
    
    // ============================================
    // PART 2: Get Team Info
    // ============================================
    console.log('\n=== PART 2: Get Team Info ===');
    
    const teamInfo = await evaluate(ws, `
        (async function() {
            var result = {
                success: false,
                error: null,
                team: null
            };
            
            if (!window.nim) {
                result.error = 'nim not found';
                return JSON.stringify(result);
            }
            
            try {
                var team = await new Promise(function(resolve, reject) {
                    window.nim.getTeam({
                        teamId: '${teamId}',
                        done: function(err, obj) {
                            if (err) reject(err);
                            else resolve(obj);
                        }
                    });
                    setTimeout(function() { reject(new Error('Timeout')); }, 10000);
                });
                
                result.success = true;
                result.team = {
                    teamId: team.teamId,
                    name: team.name,
                    avatar: team.avatar,
                    intro: team.intro,
                    announcement: team.announcement,
                    joinMode: team.joinMode,
                    beInviteMode: team.beInviteMode,
                    inviteMode: team.inviteMode,
                    updateTeamMode: team.updateTeamMode,
                    updateCustomMode: team.updateCustomMode,
                    memberNum: team.memberNum,
                    memberUpdateTime: team.memberUpdateTime,
                    createTime: team.createTime,
                    updateTime: team.updateTime,
                    owner: team.owner,
                    type: team.type,
                    level: team.level,
                    valid: team.valid,
                    serverCustom: team.serverCustom,
                    custom: team.custom,
                    allFields: Object.keys(team)
                };
            } catch (e) {
                result.error = e.message || String(e);
            }
            
            return JSON.stringify(result);
        })();
    `, true);
    
    allResults.teamInfo = teamInfo;
    console.log('Team Success:', teamInfo?.success);
    
    if (teamInfo?.team) {
        console.log('\nTeam Details:');
        console.log('  Name:', teamInfo.team.name);
        console.log('  Owner:', teamInfo.team.owner);
        console.log('  Member Count:', teamInfo.team.memberNum);
        console.log('  Type:', teamInfo.team.type);
        console.log('  JoinMode:', teamInfo.team.joinMode);
        console.log('  All Fields:', teamInfo.team.allFields?.join(', '));
    }
    
    // ============================================
    // PART 3: Get Recent Messages
    // ============================================
    console.log('\n=== PART 3: Get Recent Messages ===');
    
    const messagesData = await evaluate(ws, `
        (async function() {
            var result = {
                success: false,
                error: null,
                messageCount: 0,
                messageFields: [],
                sampleMessages: []
            };
            
            if (!window.nim) {
                result.error = 'nim not found';
                return JSON.stringify(result);
            }
            
            try {
                var msgs = await new Promise(function(resolve, reject) {
                    window.nim.getHistoryMsgs({
                        scene: 'team',
                        to: '${teamId}',
                        limit: 50,
                        done: function(err, obj) {
                            if (err) reject(err);
                            else resolve(obj);
                        }
                    });
                    setTimeout(function() { reject(new Error('Timeout')); }, 10000);
                });
                
                result.success = true;
                var messages = msgs.msgs || msgs || [];
                result.messageCount = messages.length;
                
                if (messages.length > 0) {
                    result.messageFields = Object.keys(messages[0]);
                    
                    // Get sample messages (last 10, without full content)
                    result.sampleMessages = messages.slice(-10).map(function(m) {
                        return {
                            type: m.type,
                            from: m.from,
                            fromNick: m.fromNick,
                            to: m.to,
                            time: m.time,
                            idClient: m.idClient,
                            idServer: m.idServer,
                            sessionId: m.sessionId,
                            scene: m.scene,
                            flow: m.flow,
                            status: m.status,
                            text: m.text ? (m.text.length > 100 ? m.text.substring(0, 100) + '...' : m.text) : null,
                            content: m.content ? '{content object}' : null,
                            file: m.file ? '{file object}' : null,
                            geo: m.geo ? '{geo object}' : null,
                            custom: m.custom,
                            pushContent: m.pushContent,
                            pushPayload: m.pushPayload,
                            isHistoryable: m.isHistoryable,
                            isRoamingable: m.isRoamingable,
                            isSyncable: m.isSyncable,
                            isPushable: m.isPushable,
                            isOfflinable: m.isOfflinable,
                            isUnreadable: m.isUnreadable,
                            needPushNick: m.needPushNick,
                            isLocal: m.isLocal,
                            localCustom: m.localCustom,
                            allFields: Object.keys(m)
                        };
                    });
                }
            } catch (e) {
                result.error = e.message || String(e);
            }
            
            return JSON.stringify(result);
        })();
    `, true);
    
    allResults.messages = messagesData;
    console.log('Messages Success:', messagesData?.success);
    console.log('Message Count:', messagesData?.messageCount);
    
    if (messagesData?.messageFields?.length) {
        console.log('\nMessage Fields:', messagesData.messageFields.join(', '));
    }
    
    if (messagesData?.sampleMessages?.length) {
        console.log('\nSample Messages:');
        messagesData.sampleMessages.forEach((m, i) => {
            console.log(`  ${i + 1}. [${m.type}] ${m.fromNick || m.from}: ${m.text || '(non-text)'}`);
        });
    }
    
    // ============================================
    // PART 4: Get Current User Info
    // ============================================
    console.log('\n=== PART 4: Current User Info ===');
    
    const userInfo = await evaluate(ws, `
        (async function() {
            var result = {
                success: false,
                error: null,
                user: null
            };
            
            if (!window.nim) {
                result.error = 'nim not found';
                return JSON.stringify(result);
            }
            
            try {
                var user = await new Promise(function(resolve, reject) {
                    window.nim.getMyInfo({
                        done: function(err, obj) {
                            if (err) reject(err);
                            else resolve(obj);
                        }
                    });
                    setTimeout(function() { reject(new Error('Timeout')); }, 10000);
                });
                
                result.success = true;
                result.user = {
                    account: user.account,
                    nick: user.nick,
                    avatar: user.avatar,
                    sign: user.sign,
                    email: user.email,
                    birth: user.birth,
                    tel: user.tel,
                    gender: user.gender,
                    custom: user.custom,
                    createTime: user.createTime,
                    updateTime: user.updateTime,
                    allFields: Object.keys(user)
                };
            } catch (e) {
                result.error = e.message || String(e);
            }
            
            return JSON.stringify(result);
        })();
    `, true);
    
    allResults.userInfo = userInfo;
    console.log('User Success:', userInfo?.success);
    
    if (userInfo?.user) {
        console.log('\nCurrent User:');
        console.log('  Account:', userInfo.user.account);
        console.log('  Nick:', userInfo.user.nick);
        console.log('  All Fields:', userInfo.user.allFields?.join(', '));
    }
    
    // ============================================
    // PART 5: Get All Teams/Groups
    // ============================================
    console.log('\n=== PART 5: All Teams/Groups ===');
    
    const teamsData = await evaluate(ws, `
        (async function() {
            var result = {
                success: false,
                error: null,
                teams: []
            };
            
            if (!window.nim) {
                result.error = 'nim not found';
                return JSON.stringify(result);
            }
            
            try {
                var teams = await new Promise(function(resolve, reject) {
                    window.nim.getTeams({
                        done: function(err, obj) {
                            if (err) reject(err);
                            else resolve(obj);
                        }
                    });
                    setTimeout(function() { reject(new Error('Timeout')); }, 10000);
                });
                
                result.success = true;
                var teamList = teams.teams || teams || [];
                
                result.teams = teamList.map(function(t) {
                    return {
                        teamId: t.teamId,
                        name: t.name,
                        memberNum: t.memberNum,
                        owner: t.owner,
                        type: t.type,
                        valid: t.valid
                    };
                });
            } catch (e) {
                result.error = e.message || String(e);
            }
            
            return JSON.stringify(result);
        })();
    `, true);
    
    allResults.teams = teamsData;
    console.log('Teams Success:', teamsData?.success);
    
    if (teamsData?.teams?.length) {
        console.log('\nAll Teams:');
        teamsData.teams.forEach((t, i) => {
            console.log(`  ${i + 1}. ${t.name} (${t.teamId}) - ${t.memberNum} members`);
        });
    }
    
    // ============================================
    // PART 6: Export member accounts for OnlyMemberBet
    // ============================================
    console.log('\n=== PART 6: Member Accounts for OnlyMemberBet ===');
    
    if (membersData?.success && membersData.members) {
        const memberAccounts = membersData.members.map(m => m.account);
        allResults.memberAccounts = memberAccounts;
        
        console.log(`\nTotal ${memberAccounts.length} member accounts ready for OnlyMemberBet validation`);
        console.log('First 20 accounts:', memberAccounts.slice(0, 20).join(', '));
    }
    
    // ============================================
    // Save Results
    // ============================================
    const outputFile = `wangshangliao_full_data_${new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19)}.json`;
    fs.writeFileSync(outputFile, JSON.stringify(allResults, null, 2));
    console.log('\n=== Results saved to:', outputFile, '===');
    
    // Also save member accounts to a separate file for easy import
    if (allResults.memberAccounts) {
        const accountsFile = `member_accounts_${teamId}.json`;
        fs.writeFileSync(accountsFile, JSON.stringify({
            teamId: teamId,
            timestamp: allResults.timestamp,
            totalCount: allResults.memberAccounts.length,
            accounts: allResults.memberAccounts
        }, null, 2));
        console.log('Member accounts saved to:', accountsFile);
    }
    
    ws.close();
    console.log('\n=== Full Exploration Complete ===');
}

main().catch(console.error);

