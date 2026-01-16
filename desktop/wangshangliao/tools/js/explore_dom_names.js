// Extract displayed nicknames from DOM and map to accounts
const WebSocket = require('ws');

const CDP_URL = 'ws://127.0.0.1:9222/devtools/page/8322EEB7A02952E8C4C59B59B616C299';

async function explore() {
    console.log('Connecting to CDP...');
    const ws = new WebSocket(CDP_URL);
    
    await new Promise((resolve, reject) => {
        ws.on('open', resolve);
        ws.on('error', reject);
    });
    
    console.log('Connected!');
    let msgId = 1;
    
    function send(method, params = {}) {
        return new Promise((resolve) => {
            const id = msgId++;
            const handler = (data) => {
                const msg = JSON.parse(data);
                if (msg.id === id) {
                    ws.off('message', handler);
                    resolve(msg.result);
                }
            };
            ws.on('message', handler);
            ws.send(JSON.stringify({ id, method, params }));
        });
    }
    
    async function evaluate(expression) {
        const result = await send('Runtime.evaluate', {
            expression,
            returnByValue: true,
            awaitPromise: true
        });
        return result?.result?.value;
    }
    
    // Get document structure
    console.log('\n=== Full Document Body Classes ===');
    const bodyScript = `
(function() {
    var result = { classes: [], structure: [] };
    
    // Get all unique class names in the document
    var allElements = document.querySelectorAll('*');
    var classSet = new Set();
    
    allElements.forEach(function(el) {
        if (el.className && typeof el.className === 'string') {
            el.className.split(' ').forEach(function(c) {
                if (c.trim()) classSet.add(c.trim());
            });
        }
    });
    
    result.classes = Array.from(classSet).filter(function(c) {
        return c.toLowerCase().indexOf('member') !== -1 ||
               c.toLowerCase().indexOf('nick') !== -1 ||
               c.toLowerCase().indexOf('user') !== -1 ||
               c.toLowerCase().indexOf('group') !== -1 ||
               c.toLowerCase().indexOf('team') !== -1 ||
               c.toLowerCase().indexOf('contact') !== -1 ||
               c.toLowerCase().indexOf('sidebar') !== -1;
    });
    
    return JSON.stringify(result, null, 2);
})()`;

    const body = await evaluate(bodyScript);
    console.log('Relevant Classes:', body);
    
    // Look for sidebar with member list
    console.log('\n=== Looking for Member Sidebar ===');
    const sidebarScript = `
(function() {
    var result = { found: [], memberNames: [] };
    
    // Common sidebar selectors
    var selectors = [
        '.sidebar',
        '.right-sidebar', 
        '.member-sidebar',
        '[class*="sidebar"]',
        '[class*="Sidebar"]',
        '[class*="member"]',
        '[class*="Member"]',
        '.group-info',
        '[class*="groupInfo"]'
    ];
    
    selectors.forEach(function(sel) {
        try {
            var elements = document.querySelectorAll(sel);
            if (elements.length > 0) {
                result.found.push({
                    selector: sel,
                    count: elements.length,
                    firstClass: elements[0].className.substring(0, 100),
                    innerHTML: elements[0].innerHTML.substring(0, 300)
                });
            }
        } catch(e) {}
    });
    
    return JSON.stringify(result, null, 2);
})()`;

    const sidebar = await evaluate(sidebarScript);
    console.log('Sidebar Search:', sidebar);
    
    // Get innerHTML of the entire page and search for member list pattern
    console.log('\n=== Searching for 群成员 in HTML ===');
    const searchHtmlScript = `
(function() {
    var html = document.body.innerHTML;
    var result = { found: false, context: [] };
    
    // Look for 群成员 text
    var patterns = ['群成员', '成员列表', 'member', 'Members'];
    
    patterns.forEach(function(pattern) {
        var idx = html.indexOf(pattern);
        if (idx !== -1) {
            result.found = true;
            result.context.push({
                pattern: pattern,
                context: html.substring(Math.max(0, idx - 50), Math.min(html.length, idx + 200))
            });
        }
    });
    
    return JSON.stringify(result, null, 2);
})()`;

    const searchHtml = await evaluate(searchHtmlScript);
    console.log('HTML Search:', searchHtml);
    
    // Look for the member list container directly
    console.log('\n=== Looking for Container with 186/200 Members ===');
    const memberCountScript = `
(function() {
    var result = { found: [], elements: [] };
    
    // Search for text containing member count (186/200)
    var allElements = document.querySelectorAll('*');
    
    for (var i = 0; i < allElements.length; i++) {
        var el = allElements[i];
        try {
            var text = el.textContent || '';
            
            // Look for member count pattern
            if (text.match(/\\d+\\/\\d+/) || text.indexOf('186') !== -1 || text.indexOf('群成员') !== -1) {
                var directText = '';
                for (var j = 0; j < el.childNodes.length; j++) {
                    if (el.childNodes[j].nodeType === 3) {
                        directText += el.childNodes[j].textContent;
                    }
                }
                
                if (directText.trim()) {
                    result.elements.push({
                        tag: el.tagName,
                        className: el.className.substring(0, 80),
                        directText: directText.trim().substring(0, 50),
                        parentClass: el.parentElement ? el.parentElement.className.substring(0, 50) : ''
                    });
                }
            }
        } catch(e) {}
    }
    
    return JSON.stringify(result.elements.slice(0, 10), null, 2);
})()`;

    const memberCount = await evaluate(memberCountScript);
    console.log('Member Count Elements:', memberCount);
    
    // Now look for elements near the member count
    console.log('\n=== Find Parent Container with Member Names ===');
    const parentContainerScript = `
(function() {
    var result = { found: false, names: [] };
    
    // Find element containing "群成员"
    var allElements = document.querySelectorAll('*');
    var memberHeader = null;
    
    for (var i = 0; i < allElements.length; i++) {
        var el = allElements[i];
        try {
            // Check direct text content
            var directText = '';
            for (var j = 0; j < el.childNodes.length; j++) {
                if (el.childNodes[j].nodeType === 3) {
                    directText += el.childNodes[j].textContent;
                }
            }
            
            if (directText.indexOf('群成员') !== -1) {
                memberHeader = el;
                result.headerFound = true;
                result.headerClass = el.className;
                break;
            }
        } catch(e) {}
    }
    
    if (!memberHeader) {
        return JSON.stringify({error: 'Header not found'});
    }
    
    // Go up to find the container
    var container = memberHeader.parentElement;
    for (var k = 0; k < 5 && container; k++) {
        // Check if this container has many children (member list)
        var children = container.querySelectorAll('*');
        if (children.length > 50) {
            result.containerClass = container.className;
            result.containerChildren = children.length;
            
            // Get text content of all spans/divs inside
            var textElements = container.querySelectorAll('span, div');
            var names = [];
            
            textElements.forEach(function(te) {
                var t = te.textContent.trim();
                // Filter for likely names (short, no special chars, not numbers)
                if (t.length > 0 && t.length < 20 && 
                    !t.match(/^[0-9\\/\\(\\)]+$/) && 
                    t.indexOf('群成员') === -1 &&
                    t.indexOf('搜索') === -1) {
                    names.push(t);
                }
            });
            
            // Dedupe
            result.names = [...new Set(names)].slice(0, 30);
            result.found = true;
            break;
        }
        container = container.parentElement;
    }
    
    return JSON.stringify(result, null, 2);
})()`;

    const parentContainer = await evaluate(parentContainerScript);
    console.log('Parent Container:', parentContainer);
    
    // Try to get member items with both name and account
    console.log('\n=== Extract Member Items with Account Mapping ===');
    const memberItemsScript = `
(async function() {
    var result = { members: [], error: null };
    
    // Get team ID from URL
    var url = window.location.href;
    var match = url.match(/team-([0-9]+)/);
    if (!match || !window.nim) {
        return JSON.stringify({error: 'No team or nim'});
    }
    
    var teamId = match[1];
    
    // Get all member accounts from NIM
    var nimMembers = await new Promise(function(resolve, reject) {
        window.nim.getTeamMembers({
            teamId: teamId,
            done: function(err, obj) {
                if (err) reject(err);
                else resolve(obj);
            }
        });
        setTimeout(reject, 10000);
    });
    
    var members = nimMembers.members || nimMembers || [];
    
    // Find all displayed member elements in DOM
    var memberDivs = document.querySelectorAll('[class*="member-item"], [class*="memberItem"], [class*="user-item"]');
    
    result.nimMemberCount = members.length;
    result.domMemberCount = memberDivs.length;
    
    // Extract names from DOM
    var domNames = [];
    var allTextNodes = document.body.querySelectorAll('*');
    
    // Look for elements that might be member names (near member list area)
    var sidebar = document.querySelector('[class*="sidebar"]') || document.querySelector('[class*="right"]');
    if (sidebar) {
        var spans = sidebar.querySelectorAll('span, div');
        spans.forEach(function(s) {
            var t = s.textContent.trim();
            // Filter: short text, not numbers, not empty
            if (t.length > 1 && t.length < 15 && 
                !t.match(/^[0-9\\/\\(\\)\\s]+$/) &&
                t !== '群成员' && t !== '搜索') {
                domNames.push(t);
            }
        });
    }
    
    result.possibleNames = [...new Set(domNames)].slice(0, 50);
    
    return JSON.stringify(result, null, 2);
})()`;

    const memberItems = await evaluate(memberItemsScript);
    console.log('Member Items:', memberItems);
    
    // Final: Get Vue component data for member list
    console.log('\n=== Deep Vue Component Search ===');
    const deepVueScript = `
(function() {
    var result = { components: [] };
    
    // Walk DOM and find Vue components
    function findVueComponents(el, depth) {
        if (depth > 10 || !el) return;
        
        if (el.__vue__) {
            var v = el.__vue__;
            var componentName = v.$options.name || v.$options._componentTag || 'unknown';
            
            // Check if this might be member-related
            if (componentName.toLowerCase().indexOf('member') !== -1 ||
                componentName.toLowerCase().indexOf('user') !== -1 ||
                componentName.toLowerCase().indexOf('contact') !== -1 ||
                componentName.toLowerCase().indexOf('sidebar') !== -1 ||
                componentName.toLowerCase().indexOf('group') !== -1) {
                
                result.components.push({
                    name: componentName,
                    dataKeys: v.$data ? Object.keys(v.$data) : [],
                    propsKeys: v.$props ? Object.keys(v.$props) : [],
                    computedKeys: v._computedWatchers ? Object.keys(v._computedWatchers) : []
                });
            }
        }
        
        // Recurse into children
        var children = el.children || [];
        for (var i = 0; i < children.length; i++) {
            findVueComponents(children[i], depth + 1);
        }
    }
    
    findVueComponents(document.body, 0);
    
    return JSON.stringify(result, null, 2);
})()`;

    const deepVue = await evaluate(deepVueScript);
    console.log('Deep Vue:', deepVue);
    
    ws.close();
    console.log('\nExploration complete!');
}

explore().catch(console.error);

