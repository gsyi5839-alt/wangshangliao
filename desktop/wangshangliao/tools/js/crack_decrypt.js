// Advanced decryption attempt with known plaintext
const crypto = require('crypto');

// Known data
const appKey = 'b03cfcd909dbf05c25163cc8c7e7b6cf';
const groupId = '1176721';
const teamId = '40821608989';

// Known plaintext-ciphertext pair (验证用)
const knownPlaintext = '法拉利客服';
const knownCiphertext = 'nDt2pNsxc6t+8U74M/qn3w==';
const knownAccount = '1948408648';

// More samples to test
const samples = [
    { account: '1393372631', ciphertext: 'HLF4vIVpTTX9OuKnY1gW6g==' },
    { account: '2101370650', ciphertext: 'SNtGTu6r6JHF0GKHFz2fCw==' },
    { account: '1598362464', ciphertext: 'uLU+SdEvJnP9tb07iJh8vw==' },
];

console.log('=== Advanced Decryption Research ===\n');
console.log('Known plaintext:', knownPlaintext);
console.log('Known plaintext UTF-8 bytes:', Buffer.from(knownPlaintext).toString('hex'));
console.log('Known ciphertext:', knownCiphertext);
console.log('Known ciphertext bytes:', Buffer.from(knownCiphertext, 'base64').toString('hex'));
console.log('');

// Try many more key derivation methods
function tryDecrypt(key, ciphertext, mode, iv) {
    try {
        const decipher = crypto.createDecipheriv(mode, key, iv || '');
        decipher.setAutoPadding(true);
        let dec = decipher.update(Buffer.from(ciphertext, 'base64'));
        dec = Buffer.concat([dec, decipher.final()]);
        return dec.toString('utf8');
    } catch (e) {
        return null;
    }
}

// Test a key derivation
function testKey(name, keyBuffer) {
    if (keyBuffer.length !== 16) return;
    
    // Test ECB
    let result = tryDecrypt(keyBuffer, knownCiphertext, 'aes-128-ecb', '');
    if (result === knownPlaintext) {
        console.log(`*** SUCCESS ECB: ${name} ***`);
        console.log(`Key (hex): ${keyBuffer.toString('hex')}`);
        return true;
    }
    
    // Test CBC with various IVs
    const ivs = [
        { name: 'zero', iv: Buffer.alloc(16, 0) },
        { name: 'appKey-hex', iv: Buffer.from(appKey, 'hex') },
        { name: 'appKey-utf8', iv: Buffer.from(appKey.slice(0, 16)) },
        { name: 'groupId-padded', iv: Buffer.from(groupId.padStart(16, '0')) },
    ];
    
    for (const ivInfo of ivs) {
        result = tryDecrypt(keyBuffer, knownCiphertext, 'aes-128-cbc', ivInfo.iv);
        if (result === knownPlaintext) {
            console.log(`*** SUCCESS CBC: ${name}, IV: ${ivInfo.name} ***`);
            console.log(`Key (hex): ${keyBuffer.toString('hex')}`);
            console.log(`IV (hex): ${ivInfo.iv.toString('hex')}`);
            return true;
        }
    }
    
    return false;
}

console.log('Testing key derivations...\n');

// Basic derivations
testKey('appKey-hex', Buffer.from(appKey, 'hex'));
testKey('MD5(appKey)', crypto.createHash('md5').update(appKey).digest());
testKey('MD5(groupId)', crypto.createHash('md5').update(groupId).digest());
testKey('MD5(teamId)', crypto.createHash('md5').update(teamId).digest());

// Combined derivations
testKey('MD5(appKey+groupId)', crypto.createHash('md5').update(appKey + groupId).digest());
testKey('MD5(groupId+appKey)', crypto.createHash('md5').update(groupId + appKey).digest());
testKey('MD5(appKey+teamId)', crypto.createHash('md5').update(appKey + teamId).digest());
testKey('MD5(teamId+appKey)', crypto.createHash('md5').update(teamId + appKey).digest());

// With separators
testKey('MD5(appKey:groupId)', crypto.createHash('md5').update(appKey + ':' + groupId).digest());
testKey('MD5(groupId:appKey)', crypto.createHash('md5').update(groupId + ':' + appKey).digest());

// Account-based
testKey('MD5(account+appKey)', crypto.createHash('md5').update(knownAccount + appKey).digest());
testKey('MD5(appKey+account)', crypto.createHash('md5').update(appKey + knownAccount).digest());

// GroupId variations
testKey('MD5(groupId+groupId)', crypto.createHash('md5').update(groupId + groupId).digest());
testKey('groupId-repeated', Buffer.from((groupId + groupId + groupId).slice(0, 16)));

// Numeric key from groupId
const groupIdNum = parseInt(groupId);
const groupIdHex = groupIdNum.toString(16).padStart(32, '0');
testKey('groupId-as-hex', Buffer.from(groupIdHex, 'hex'));

// SHA derivations
testKey('SHA256(appKey)[0:16]', crypto.createHash('sha256').update(appKey).digest().slice(0, 16));
testKey('SHA256(groupId)[0:16]', crypto.createHash('sha256').update(groupId).digest().slice(0, 16));
testKey('SHA256(appKey+groupId)[0:16]', crypto.createHash('sha256').update(appKey + groupId).digest().slice(0, 16));

// PBKDF2 derivations
const pbkdf2Keys = [
    { name: 'PBKDF2(appKey,groupId)', key: crypto.pbkdf2Sync(appKey, groupId, 1000, 16, 'sha256') },
    { name: 'PBKDF2(groupId,appKey)', key: crypto.pbkdf2Sync(groupId, appKey, 1000, 16, 'sha256') },
    { name: 'PBKDF2(appKey,groupId,1)', key: crypto.pbkdf2Sync(appKey, groupId, 1, 16, 'sha256') },
];
pbkdf2Keys.forEach(k => testKey(k.name, k.key));

// Common static keys (some apps use hardcoded keys)
const staticKeys = [
    'wangshangliao!@#',
    'wsl_encrypt_key!',
    '1234567890123456',
    '0123456789abcdef',
    'abcdef0123456789',
];
staticKeys.forEach(k => testKey(`static: ${k}`, Buffer.from(k)));

// Try MD5 of common strings
const commonStrings = [
    'wangshangliao',
    'wsl',
    'nickname',
    'encrypt',
    'aes_key',
    '旺商聊',
];
commonStrings.forEach(s => testKey(`MD5(${s})`, crypto.createHash('md5').update(s).digest()));

console.log('\n=== Trying Brute Force on groupId variations ===\n');

// Try variations of groupId as key
for (let i = 0; i < 16; i++) {
    // Shift groupId left
    const shifted = groupId.padStart(16, '0').slice(-16 + i) + '0'.repeat(i);
    testKey(`groupId-shift-${i}`, Buffer.from(shifted.slice(0, 16)));
}

// Try groupId with different encodings
const groupIdBytes = Buffer.alloc(16);
groupIdBytes.writeInt32LE(groupIdNum, 0);
testKey('groupId-int32LE', groupIdBytes);

groupIdBytes.fill(0);
groupIdBytes.writeInt32BE(groupIdNum, 0);
testKey('groupId-int32BE', groupIdBytes);

console.log('\n=== Analyzing ciphertext patterns ===\n');

// Check if multiple ciphertexts share patterns (might indicate ECB mode)
const ciphertexts = [
    { name: 'known', hex: Buffer.from(knownCiphertext, 'base64').toString('hex') },
    ...samples.map(s => ({ name: s.account, hex: Buffer.from(s.ciphertext, 'base64').toString('hex') }))
];

console.log('Ciphertext analysis:');
ciphertexts.forEach(c => {
    console.log(`  ${c.name}: ${c.hex} (${c.hex.length / 2} bytes)`);
});

// All ciphertexts are 16 bytes = single AES block
// This suggests either:
// 1. ECB mode (same key)
// 2. CBC with predictable IV

console.log('\n=== Try XOR-based decryption ===\n');

// Maybe it's a simple XOR cipher, not AES
const knownPlaintextBytes = Buffer.from(knownPlaintext);
const knownCiphertextBytes = Buffer.from(knownCiphertext, 'base64');

// XOR to find potential key
if (knownPlaintextBytes.length <= knownCiphertextBytes.length) {
    const xorKey = Buffer.alloc(knownCiphertextBytes.length);
    for (let i = 0; i < knownCiphertextBytes.length; i++) {
        if (i < knownPlaintextBytes.length) {
            xorKey[i] = knownPlaintextBytes[i] ^ knownCiphertextBytes[i];
        } else {
            // Padding byte
            const padLen = knownCiphertextBytes.length - knownPlaintextBytes.length;
            xorKey[i] = padLen ^ knownCiphertextBytes[i];
        }
    }
    console.log('XOR key (if simple XOR):', xorKey.toString('hex'));
    
    // Try this key on another sample
    const sample1Cipher = Buffer.from(samples[0].ciphertext, 'base64');
    const decrypted1 = Buffer.alloc(sample1Cipher.length);
    for (let i = 0; i < sample1Cipher.length; i++) {
        decrypted1[i] = sample1Cipher[i] ^ xorKey[i % xorKey.length];
    }
    console.log('Sample 1 decrypted with XOR key:', decrypted1.toString('utf8'));
}

console.log('\n=== Check if key might be derived from NIM token ===\n');

const token = 'aY5RmNXSVp2ONHuxakKUu-5QYhusco9hLFVWzxCI4DM';
testKey('MD5(token)', crypto.createHash('md5').update(token).digest());
testKey('token[0:16]', Buffer.from(token.slice(0, 16)));
testKey('MD5(token+groupId)', crypto.createHash('md5').update(token + groupId).digest());

console.log('\nDone. If no SUCCESS message, need to investigate further.');

