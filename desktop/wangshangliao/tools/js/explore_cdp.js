// WangShangLiao Field Explorer using CDP
// Run with: node explore_cdp.js

const http = require('http');
const WebSocket = require('ws');

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
        }, 30000);
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
    console.log('=== WangShangLiao Deep Field Explorer ===');
    console.log('Time:', new Date().toISOString());
    
    // Get targets
    const targets = await getTargets();
    const target = targets.find(t => t.type === 'page');
    if (!target) {
        console.error('No page target found');
        process.exit(1);
    }
    
    console.log('Target:', target.title);
    console.log('URL:', target.url);
    
    // Connect WebSocket
    const ws = await connectWS(target.webSocketDebuggerUrl);
    console.log('WebSocket connected!');
    
    const allResults = {};
    
    // ============================================
    // PART 1: NIM SDK Methods
    // ============================================
    console.log('\n=== PART 1: NIM SDK Methods ===');
    
    const nimData = await evaluate(ws, `
        (function() {
            var result = {
                available: false,
                version: null,
                allMethods: [],
                categorized: {}
            };
            
            if (!window.nim) return JSON.stringify(result);
            
            result.available = true;
            result.version = window.nim.version || 'unknown';
            
            var methods = Object.keys(window.nim).filter(function(k) {
                return typeof window.nim[k] === 'function';
            }).sort();
            
            result.allMethods = methods;
            
            var categories = {
                team: [], member: [], group: [], msg: [], session: [],
                friend: [], user: [], file: [], system: [], other: []
            };
            
            methods.forEach(function(m) {
                var lower = m.toLowerCase();
                if (lower.includes('team')) categories.team.push(m);
                else if (lower.includes('member')) categories.member.push(m);
                else if (lower.includes('group')) categories.group.push(m);
                else if (lower.includes('msg') || lower.includes('message')) categories.msg.push(m);
                else if (lower.includes('session')) categories.session.push(m);
                else if (lower.includes('friend')) categories.friend.push(m);
                else if (lower.includes('user') || lower.includes('account')) categories.user.push(m);
                else if (lower.includes('file') || lower.includes('image') || lower.includes('upload')) categories.file.push(m);
                else if (lower.includes('system') || lower.includes('sys')) categories.system.push(m);
                else categories.other.push(m);
            });
            
            result.categorized = categories;
            return JSON.stringify(result);
        })();
    `);
    
    allResults.nim = nimData;
    console.log('NIM SDK Available:', nimData?.available);
    console.log('Version:', nimData?.version);
    console.log('Total Methods:', nimData?.allMethods?.length);
    
    if (nimData?.categorized) {
        for (const cat of ['team', 'member', 'group', 'msg', 'session']) {
            const methods = nimData.categorized[cat];
            if (methods?.length) {
                console.log(`\n  [${cat}] (${methods.length} methods):`, methods.join(', '));
            }
        }
    }
    
    // ============================================
    // PART 2: Vue Store Structure
    // ============================================
    console.log('\n=== PART 2: Vue Store Structure ===');
    
    const vueData = await evaluate(ws, `
        (function() {
            var result = {
                hasVue: false,
                hasStore: false,
                storeModules: [],
                moduleDetails: {}
            };
            
            var app = document.querySelector('#app');
            if (!app || !app.__vue__) return JSON.stringify(result);
            
            result.hasVue = true;
            var vue = app.__vue__;
            
            if (!vue.$store) return JSON.stringify(result);
            result.hasStore = true;
            
            var state = vue.$store.state || {};
            result.storeModules = Object.keys(state);
            
            result.storeModules.forEach(function(mod) {
                var modState = state[mod];
                if (modState && typeof modState === 'object') {
                    result.moduleDetails[mod] = {
                        keys: Object.keys(modState),
                        types: {}
                    };
                    Object.keys(modState).forEach(function(k) {
                        var val = modState[k];
                        var type = Array.isArray(val) ? 'array[' + val.length + ']' : typeof val;
                        if (type === 'object' && val !== null) {
                            type = 'object{' + Object.keys(val).slice(0,5).join(',') + (Object.keys(val).length > 5 ? '...' : '') + '}';
                        }
                        result.moduleDetails[mod].types[k] = type;
                    });
                }
            });
            
            return JSON.stringify(result);
        })();
    `);
    
    allResults.vue = vueData;
    console.log('Vue Available:', vueData?.hasVue);
    console.log('Store Available:', vueData?.hasStore);
    
    if (vueData?.storeModules) {
        console.log('\nStore Modules:', vueData.storeModules.length);
        for (const mod of vueData.storeModules) {
            const details = vueData.moduleDetails[mod];
            if (details) {
                console.log(`\n  [${mod}]:`);
                for (const key of details.keys) {
                    console.log(`    - ${key}: ${details.types[key]}`);
                }
            }
        }
    }
    
    // ============================================
    // PART 3: Current Session Details
    // ============================================
    console.log('\n=== PART 3: Current Session Details ===');
    
    const sessionData = await evaluate(ws, `
        (function() {
            var result = {
                hasSession: false,
                sessionFields: [],
                sessionData: null
            };
            
            var app = document.querySelector('#app');
            if (!app || !app.__vue__ || !app.__vue__.$store) return JSON.stringify(result);
            
            var store = app.__vue__.$store.state;
            
            if (store.sessionStore && store.sessionStore.currentSession) {
                result.hasSession = true;
                var cs = store.sessionStore.currentSession;
                result.sessionFields = Object.keys(cs);
                result.sessionData = {};
                
                Object.keys(cs).forEach(function(k) {
                    var val = cs[k];
                    if (typeof val === 'string' || typeof val === 'number' || typeof val === 'boolean') {
                        result.sessionData[k] = val;
                    } else if (Array.isArray(val)) {
                        result.sessionData[k] = '[array:' + val.length + ']';
                    } else if (val && typeof val === 'object') {
                        result.sessionData[k] = '{object:' + Object.keys(val).join(',') + '}';
                    } else {
                        result.sessionData[k] = String(val);
                    }
                });
            }
            
            return JSON.stringify(result);
        })();
    `);
    
    allResults.session = sessionData;
    console.log('Has Current Session:', sessionData?.hasSession);
    
    if (sessionData?.sessionData) {
        console.log('\nSession Fields:');
        for (const [key, val] of Object.entries(sessionData.sessionData)) {
            console.log(`  ${key} = ${val}`);
        }
    }
    
    // ============================================
    // PART 4: Group/Team Details
    // ============================================
    console.log('\n=== PART 4: Group/Team Details ===');
    
    const groupData = await evaluate(ws, `
        (function() {
            var result = {
                groups: [],
                currentTeamId: null
            };
            
            var app = document.querySelector('#app');
            if (!app || !app.__vue__ || !app.__vue__.$store) return JSON.stringify(result);
            
            var store = app.__vue__.$store.state;
            
            if (store.appStore && store.appStore.groupList) {
                var gl = store.appStore.groupList;
                
                if (gl.owner) {
                    gl.owner.forEach(function(g) {
                        result.groups.push({
                            type: 'owner',
                            groupId: g.groupId,
                            groupAccount: g.groupAccount,
                            groupName: g.groupName,
                            nimGroupId: g.nimGroupId,
                            memberCount: g.groupMemberNum || g.memberCount,
                            allFields: Object.keys(g)
                        });
                    });
                }
                
                if (gl.member) {
                    gl.member.forEach(function(g) {
                        result.groups.push({
                            type: 'member',
                            groupId: g.groupId,
                            groupAccount: g.groupAccount,
                            groupName: g.groupName,
                            nimGroupId: g.nimGroupId,
                            memberCount: g.groupMemberNum || g.memberCount,
                            allFields: Object.keys(g)
                        });
                    });
                }
            }
            
            if (store.sessionStore && store.sessionStore.currentSession) {
                result.currentTeamId = store.sessionStore.currentSession.to || store.sessionStore.currentSession.teamId;
            }
            
            return JSON.stringify(result);
        })();
    `);
    
    allResults.groups = groupData;
    console.log('Total Groups:', groupData?.groups?.length);
    console.log('Current Team ID:', groupData?.currentTeamId);
    
    if (groupData?.groups?.length) {
        console.log('\nGroups:');
        for (const g of groupData.groups) {
            console.log(`  [${g.type}] ${g.groupName}`);
            console.log(`    GroupID: ${g.groupId}`);
            console.log(`    Account: ${g.groupAccount}`);
            console.log(`    NimID: ${g.nimGroupId}`);
            console.log(`    Members: ${g.memberCount}`);
            console.log(`    Fields: ${g.allFields?.join(', ')}`);
        }
    }
    
    // ============================================
    // PART 5: Message Structure
    // ============================================
    console.log('\n=== PART 5: Message Structure ===');
    
    const msgData = await evaluate(ws, `
        (function() {
            var result = {
                msgFields: [],
                sampleMsg: null,
                msgTypes: [],
                recentMessages: []
            };
            
            var app = document.querySelector('#app');
            if (!app || !app.__vue__ || !app.__vue__.$store) return JSON.stringify(result);
            
            var store = app.__vue__.$store.state;
            var messages = null;
            
            if (store.messageStore && store.messageStore.messages) {
                var msgStore = store.messageStore.messages;
                var keys = Object.keys(msgStore);
                if (keys.length > 0) {
                    var firstKey = keys[0];
                    var msgArray = msgStore[firstKey];
                    if (Array.isArray(msgArray) && msgArray.length > 0) {
                        messages = msgArray;
                    }
                }
            }
            
            if (!messages && store.sessionStore) {
                if (store.sessionStore.currentMessages && Array.isArray(store.sessionStore.currentMessages)) {
                    messages = store.sessionStore.currentMessages;
                }
            }
            
            if (messages && messages.length > 0) {
                var sample = messages[messages.length - 1];
                result.msgFields = Object.keys(sample);
                
                result.sampleMsg = {};
                Object.keys(sample).forEach(function(k) {
                    var val = sample[k];
                    if (typeof val === 'string') {
                        result.sampleMsg[k] = val.length > 50 ? val.substring(0, 50) + '...' : val;
                    } else if (typeof val === 'number' || typeof val === 'boolean') {
                        result.sampleMsg[k] = val;
                    } else if (Array.isArray(val)) {
                        result.sampleMsg[k] = '[array:' + val.length + ']';
                    } else if (val && typeof val === 'object') {
                        result.sampleMsg[k] = '{' + Object.keys(val).slice(0,3).join(',') + '}';
                    }
                });
                
                var types = {};
                messages.forEach(function(m) {
                    if (m.type) types[m.type] = true;
                });
                result.msgTypes = Object.keys(types);
                
                // Get last 5 messages (without content)
                result.recentMessages = messages.slice(-5).map(function(m) {
                    return {
                        type: m.type,
                        from: m.from,
                        fromNick: m.fromNick,
                        time: m.time,
                        idClient: m.idClient
                    };
                });
            }
            
            return JSON.stringify(result);
        })();
    `);
    
    allResults.message = msgData;
    console.log('Message Fields Found:', msgData?.msgFields?.length);
    
    if (msgData?.msgFields?.length) {
        console.log('\nMessage Fields:', msgData.msgFields.join(', '));
    }
    
    if (msgData?.msgTypes?.length) {
        console.log('\nMessage Types:', msgData.msgTypes.join(', '));
    }
    
    if (msgData?.sampleMsg) {
        console.log('\nSample Message Structure:');
        for (const [key, val] of Object.entries(msgData.sampleMsg)) {
            console.log(`  ${key} = ${val}`);
        }
    }
    
    // ============================================
    // PART 6: Team Members (Critical for OnlyMemberBet)
    // ============================================
    console.log('\n=== PART 6: Team Members (for OnlyMemberBet) ===');
    
    const memberData = await evaluate(ws, `
        (async function() {
            var result = {
                success: false,
                teamId: null,
                memberCount: 0,
                memberFields: [],
                sampleMembers: [],
                memberTypes: [],
                allMemberIds: []
            };
            
            var app = document.querySelector('#app');
            if (app && app.__vue__ && app.__vue__.$store) {
                var store = app.__vue__.$store.state;
                if (store.sessionStore && store.sessionStore.currentSession) {
                    result.teamId = store.sessionStore.currentSession.to;
                }
                if (!result.teamId && store.appStore && store.appStore.groupList) {
                    var gl = store.appStore.groupList;
                    var first = (gl.owner && gl.owner[0]) || (gl.member && gl.member[0]);
                    if (first) result.teamId = first.nimGroupId || first.groupId;
                }
            }
            
            if (!result.teamId || !window.nim) return JSON.stringify(result);
            
            if (typeof window.nim.getTeamMembers === 'function') {
                var members = await new Promise(function(resolve) {
                    window.nim.getTeamMembers({
                        teamId: result.teamId,
                        done: function(err, obj) {
                            if (err) {
                                console.log('getTeamMembers error:', err);
                                resolve([]);
                            } else {
                                console.log('getTeamMembers success');
                                resolve(obj.members || obj || []);
                            }
                        }
                    });
                    setTimeout(function() { resolve([]); }, 10000);
                });
                
                if (members.length > 0) {
                    result.success = true;
                    result.memberCount = members.length;
                    result.memberFields = Object.keys(members[0]);
                    
                    // Get all member IDs (for OnlyMemberBet feature)
                    result.allMemberIds = members.map(function(m) {
                        return {
                            id: m.id || m.account,
                            account: m.account,
                            nick: m.nick || m.nickInTeam,
                            type: m.type
                        };
                    });
                    
                    // Get first 3 as samples
                    result.sampleMembers = members.slice(0, 3).map(function(m) {
                        var safe = {};
                        Object.keys(m).forEach(function(k) {
                            var val = m[k];
                            if (typeof val === 'string') {
                                safe[k] = val.length > 50 ? val.substring(0,50) + '...' : val;
                            } else if (typeof val === 'number' || typeof val === 'boolean') {
                                safe[k] = val;
                            } else if (val && typeof val === 'object') {
                                safe[k] = '{object}';
                            }
                        });
                        return safe;
                    });
                    
                    var types = {};
                    members.forEach(function(m) {
                        if (m.type !== undefined) types[m.type] = true;
                    });
                    result.memberTypes = Object.keys(types);
                }
            }
            
            return JSON.stringify(result);
        })();
    `, true);
    
    allResults.members = memberData;
    console.log('Team ID:', memberData?.teamId);
    console.log('Success:', memberData?.success);
    console.log('Member Count:', memberData?.memberCount);
    
    if (memberData?.memberFields?.length) {
        console.log('\nMember Fields:', memberData.memberFields.join(', '));
    }
    
    if (memberData?.memberTypes?.length) {
        console.log('\nMember Types:', memberData.memberTypes.join(', '));
    }
    
    if (memberData?.sampleMembers?.length) {
        console.log('\nSample Members:');
        for (const m of memberData.sampleMembers) {
            console.log('  ---');
            for (const [key, val] of Object.entries(m)) {
                console.log(`    ${key} = ${val}`);
            }
        }
    }
    
    if (memberData?.allMemberIds?.length) {
        console.log('\n=== ALL MEMBER IDs (for OnlyMemberBet) ===');
        console.log(`Total Members: ${memberData.allMemberIds.length}`);
        for (const m of memberData.allMemberIds) {
            console.log(`  - ${m.nick || m.account} (${m.account}) type:${m.type}`);
        }
    }
    
    // ============================================
    // PART 7: User/Contact Fields
    // ============================================
    console.log('\n=== PART 7: User/Contact Fields ===');
    
    const userData = await evaluate(ws, `
        (function() {
            var result = {
                currentUser: null,
                userFields: [],
                friendFields: []
            };
            
            var app = document.querySelector('#app');
            if (!app || !app.__vue__ || !app.__vue__.$store) return JSON.stringify(result);
            
            var store = app.__vue__.$store.state;
            
            if (store.appStore && store.appStore.userInfo) {
                var user = store.appStore.userInfo;
                result.userFields = Object.keys(user);
                result.currentUser = {};
                Object.keys(user).forEach(function(k) {
                    var val = user[k];
                    if (typeof val === 'string') {
                        result.currentUser[k] = val.length > 30 ? val.substring(0,30) + '...' : val;
                    } else if (typeof val === 'number' || typeof val === 'boolean') {
                        result.currentUser[k] = val;
                    }
                });
            }
            
            if (store.friendStore && store.friendStore.friends) {
                var friends = store.friendStore.friends;
                if (Array.isArray(friends) && friends.length > 0) {
                    result.friendFields = Object.keys(friends[0]);
                } else if (typeof friends === 'object') {
                    var keys = Object.keys(friends);
                    if (keys.length > 0) {
                        var first = friends[keys[0]];
                        if (first) result.friendFields = Object.keys(first);
                    }
                }
            }
            
            return JSON.stringify(result);
        })();
    `);
    
    allResults.user = userData;
    
    if (userData?.userFields?.length) {
        console.log('\nUser Fields:', userData.userFields.join(', '));
    }
    
    if (userData?.currentUser) {
        console.log('\nCurrent User:');
        for (const [key, val] of Object.entries(userData.currentUser)) {
            console.log(`  ${key} = ${val}`);
        }
    }
    
    // ============================================
    // PART 8: Team Info from NIM SDK
    // ============================================
    console.log('\n=== PART 8: Team Info from NIM SDK ===');
    
    const teamInfoData = await evaluate(ws, `
        (async function() {
            var result = {
                success: false,
                teamId: null,
                teamInfo: null,
                teamFields: []
            };
            
            var app = document.querySelector('#app');
            if (app && app.__vue__ && app.__vue__.$store) {
                var store = app.__vue__.$store.state;
                if (store.sessionStore && store.sessionStore.currentSession) {
                    result.teamId = store.sessionStore.currentSession.to;
                }
            }
            
            if (!result.teamId || !window.nim) return JSON.stringify(result);
            
            if (typeof window.nim.getTeam === 'function') {
                var team = await new Promise(function(resolve) {
                    window.nim.getTeam({
                        teamId: result.teamId,
                        done: function(err, obj) {
                            if (err) resolve(null);
                            else resolve(obj);
                        }
                    });
                    setTimeout(function() { resolve(null); }, 5000);
                });
                
                if (team) {
                    result.success = true;
                    result.teamFields = Object.keys(team);
                    result.teamInfo = {};
                    
                    Object.keys(team).forEach(function(k) {
                        var val = team[k];
                        if (typeof val === 'string') {
                            result.teamInfo[k] = val.length > 100 ? val.substring(0,100) + '...' : val;
                        } else if (typeof val === 'number' || typeof val === 'boolean') {
                            result.teamInfo[k] = val;
                        } else if (Array.isArray(val)) {
                            result.teamInfo[k] = '[array:' + val.length + ']';
                        } else if (val && typeof val === 'object') {
                            result.teamInfo[k] = '{' + Object.keys(val).slice(0,5).join(',') + '}';
                        }
                    });
                }
            }
            
            return JSON.stringify(result);
        })();
    `, true);
    
    allResults.teamInfo = teamInfoData;
    console.log('Team ID:', teamInfoData?.teamId);
    console.log('Success:', teamInfoData?.success);
    
    if (teamInfoData?.teamFields?.length) {
        console.log('\nTeam Fields:', teamInfoData.teamFields.join(', '));
    }
    
    if (teamInfoData?.teamInfo) {
        console.log('\nTeam Info:');
        for (const [key, val] of Object.entries(teamInfoData.teamInfo)) {
            console.log(`  ${key} = ${val}`);
        }
    }
    
    // ============================================
    // Save Results
    // ============================================
    const fs = require('fs');
    const outputFile = `wangshangliao_deep_${new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19)}.json`;
    fs.writeFileSync(outputFile, JSON.stringify(allResults, null, 2));
    console.log('\n=== Results saved to:', outputFile, '===');
    
    ws.close();
    console.log('\n=== Deep Exploration Complete ===');
}

main().catch(console.error);

