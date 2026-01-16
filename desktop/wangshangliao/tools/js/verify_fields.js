// Verify OnlyMemberBet field format matching
const http = require('http');
const WebSocket = require('ws');

async function test() {
    // Get target
    const resp = await new Promise(r => http.get('http://localhost:9222/json', res => {
        let d = ''; res.on('data', c => d += c); res.on('end', () => r(JSON.parse(d)));
    }));
    const target = resp.find(t => t.type === 'page');
    if (!target) { console.log('No target'); return; }
    
    // Connect
    const ws = new WebSocket(target.webSocketDebuggerUrl);
    await new Promise(r => ws.on('open', r));
    
    // Execute test
    const id = 12345;
    const script = `
        (async function() {
            var result = { 
                memberSample: null, 
                msgSample: null, 
                formatMatch: false,
                testAccount: null,
                isMember: false
            };
            
            // Get members
            if (window.nim && window.nim.getTeamMembers) {
                var members = await new Promise(r => {
                    window.nim.getTeamMembers({
                        teamId: '21654357327',
                        done: (e, o) => r(o && o.members ? o.members : [])
                    });
                    setTimeout(() => r([]), 5000);
                });
                if (members.length > 0) {
                    result.memberSample = {
                        account: members[0].account,
                        accountType: typeof members[0].account,
                        accountLength: members[0].account.length
                    };
                    // Pick a random member to test
                    result.testAccount = members[Math.floor(Math.random() * members.length)].account;
                }
            }
            
            // Get recent message
            if (window.nim && window.nim.getHistoryMsgs) {
                var msgs = await new Promise(r => {
                    window.nim.getHistoryMsgs({
                        scene: 'team',
                        to: '21654357327',
                        limit: 10,
                        done: (e, o) => r(o && o.msgs ? o.msgs : [])
                    });
                    setTimeout(() => r([]), 5000);
                });
                // Find a message from a member (not system)
                var userMsg = msgs.find(m => m.from && m.from.length > 5);
                if (userMsg) {
                    result.msgSample = {
                        from: userMsg.from,
                        fromType: typeof userMsg.from,
                        fromLength: userMsg.from.length,
                        fromNick: userMsg.fromNick
                    };
                }
            }
            
            // Check format match
            if (result.memberSample && result.msgSample) {
                result.formatMatch = 
                    result.memberSample.accountType === result.msgSample.fromType &&
                    result.memberSample.accountType === 'string';
            }
            
            // Test if message sender is in members list
            if (result.msgSample && window.nim) {
                var allMembers = await new Promise(r => {
                    window.nim.getTeamMembers({
                        teamId: '21654357327',
                        done: (e, o) => r(o && o.members ? o.members : [])
                    });
                    setTimeout(() => r([]), 5000);
                });
                var accounts = allMembers.map(m => m.account);
                result.isMember = accounts.includes(result.msgSample.from);
                result.totalMembers = accounts.length;
            }
            
            return JSON.stringify(result, null, 2);
        })();
    `;
    
    ws.send(JSON.stringify({
        id,
        method: 'Runtime.evaluate',
        params: {
            expression: script,
            returnByValue: true,
            awaitPromise: true
        }
    }));
    
    // Wait for response
    const response = await new Promise(r => ws.on('message', d => {
        const msg = JSON.parse(d);
        if (msg.id === id) r(msg);
    }));
    
    console.log('=== OnlyMemberBet Field Format Verification ===\n');
    const result = JSON.parse(response.result.result.value);
    
    console.log('📊 Member Sample:');
    console.log('   Account:', result.memberSample?.account);
    console.log('   Type:', result.memberSample?.accountType);
    console.log('   Length:', result.memberSample?.accountLength);
    
    console.log('\n📨 Message Sample:');
    console.log('   From:', result.msgSample?.from);
    console.log('   Type:', result.msgSample?.fromType);
    console.log('   Length:', result.msgSample?.fromLength);
    console.log('   Nickname:', result.msgSample?.fromNick);
    
    console.log('\n✅ Format Match:', result.formatMatch ? 'YES' : 'NO');
    console.log('✅ Sender is Member:', result.isMember ? 'YES' : 'NO');
    console.log('📈 Total Members:', result.totalMembers);
    
    if (result.formatMatch && result.isMember) {
        console.log('\n🎉 OnlyMemberBet FULLY COMPATIBLE!');
    }
    
    ws.close();
}

test().catch(console.error);

