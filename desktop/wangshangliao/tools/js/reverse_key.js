// Reverse engineer the encryption key using known plaintext-ciphertext pair
const crypto = require('crypto');

console.log('=== Reverse Engineer Encryption Key ===\n');

// Known data
const knownPairs = [
    {
        plaintext: '法拉利客服',
        ciphertext: 'nDt2pNsxc6t+8U74M/qn3w=='  // from teamMember nicknameCiphertext
    }
];

const unknownCiphertexts = [
    { account: '1391351554', ciphertext: 'acMVOxzdhuOhtn+lyr28+g==' },
    { account: '1393372631', ciphertext: 'HLF4vIVpTTX9OuKnY1gW6g==' },
    { account: '2101370650', ciphertext: 'SNtGTu6r6JHF0GKHFz2fCw==' },
    { account: '1598362464', ciphertext: 'uLU+SdEvJnP9tb07iJh8vw==' },
];

// Helper functions
function pad(text, blockSize = 16) {
    const padLen = blockSize - (Buffer.from(text).length % blockSize);
    return Buffer.concat([Buffer.from(text), Buffer.alloc(padLen, padLen)]);
}

function tryDecrypt(ciphertextBase64, key, algorithm, iv) {
    try {
        const decipher = crypto.createDecipheriv(algorithm, key, iv);
        decipher.setAutoPadding(true);
        let decrypted = decipher.update(Buffer.from(ciphertextBase64, 'base64'));
        decrypted = Buffer.concat([decrypted, decipher.final()]);
        return decrypted.toString('utf8');
    } catch (e) {
        return null;
    }
}

function tryEncrypt(plaintext, key, algorithm, iv) {
    try {
        const cipher = crypto.createCipheriv(algorithm, key, iv);
        cipher.setAutoPadding(true);
        let encrypted = cipher.update(Buffer.from(plaintext), 'utf8');
        encrypted = Buffer.concat([encrypted, cipher.final()]);
        return encrypted.toString('base64');
    } catch (e) {
        return null;
    }
}

// Analyze known pair
console.log('Known pair:');
const known = knownPairs[0];
const plaintextBytes = Buffer.from(known.plaintext);
const ciphertextBytes = Buffer.from(known.ciphertext, 'base64');

console.log(`  Plaintext: ${known.plaintext}`);
console.log(`  Plaintext (hex): ${plaintextBytes.toString('hex')}`);
console.log(`  Plaintext length: ${plaintextBytes.length} bytes`);
console.log(`  Ciphertext (base64): ${known.ciphertext}`);
console.log(`  Ciphertext (hex): ${ciphertextBytes.toString('hex')}`);
console.log(`  Ciphertext length: ${ciphertextBytes.length} bytes`);
console.log('');

// Try various key derivation methods with more variations
const appKey = 'b03cfcd909dbf05c25163cc8c7e7b6cf';
const token = 'aY5RmNXSVp2ONHuxakKUu-5QYhusco9hLFVWzxCI4DM';
const teamId = '40821608989';
const groupId = '1176721';
const account = '1948408648'; // The known account

console.log('Trying key derivation methods...\n');

// Generate candidate keys
const candidateKeys = [];

// Basic derivations
candidateKeys.push({ name: 'appKey (hex)', key: Buffer.from(appKey, 'hex') });
candidateKeys.push({ name: 'MD5(appKey)', key: crypto.createHash('md5').update(appKey).digest() });
candidateKeys.push({ name: 'MD5(token)', key: crypto.createHash('md5').update(token).digest() });
candidateKeys.push({ name: 'MD5(teamId)', key: crypto.createHash('md5').update(teamId).digest() });
candidateKeys.push({ name: 'MD5(groupId)', key: crypto.createHash('md5').update(groupId).digest() });
candidateKeys.push({ name: 'MD5(account)', key: crypto.createHash('md5').update(account).digest() });

// Combined derivations
candidateKeys.push({ name: 'MD5(appKey+teamId)', key: crypto.createHash('md5').update(appKey + teamId).digest() });
candidateKeys.push({ name: 'MD5(teamId+appKey)', key: crypto.createHash('md5').update(teamId + appKey).digest() });
candidateKeys.push({ name: 'MD5(appKey+groupId)', key: crypto.createHash('md5').update(appKey + groupId).digest() });
candidateKeys.push({ name: 'MD5(groupId+appKey)', key: crypto.createHash('md5').update(groupId + appKey).digest() });
candidateKeys.push({ name: 'MD5(appKey+account)', key: crypto.createHash('md5').update(appKey + account).digest() });

// Double hashing
candidateKeys.push({ name: 'MD5(MD5(appKey))', key: crypto.createHash('md5').update(crypto.createHash('md5').update(appKey).digest()).digest() });
candidateKeys.push({ name: 'MD5(appKey-hex)', key: crypto.createHash('md5').update(Buffer.from(appKey, 'hex')).digest() });

// With separators
[':', '-', '_', '|', '', '.'].forEach(sep => {
    candidateKeys.push({ name: `MD5(appKey${sep}teamId)`, key: crypto.createHash('md5').update(appKey + sep + teamId).digest() });
    candidateKeys.push({ name: `MD5(appKey${sep}groupId)`, key: crypto.createHash('md5').update(appKey + sep + groupId).digest() });
});

// SHA derivations
candidateKeys.push({ name: 'SHA1(appKey)[0:16]', key: crypto.createHash('sha1').update(appKey).digest().slice(0, 16) });
candidateKeys.push({ name: 'SHA256(appKey)[0:16]', key: crypto.createHash('sha256').update(appKey).digest().slice(0, 16) });

// Static common keys
const staticStrings = [
    'wangshangliao!@',
    'wsl_encrypt_key',
    '1234567890123456',
    'nim_encrypt_key!',
    'nim!@#$%^&*()123',
    'nimencryptkey123',
    'wangshangliao123',
    'nimnicknamekey12',
];
staticStrings.forEach(s => {
    candidateKeys.push({ name: `static: ${s}`, key: Buffer.from(s) });
    candidateKeys.push({ name: `MD5(${s})`, key: crypto.createHash('md5').update(s).digest() });
});

// Test each key
console.log('Testing AES-128-ECB and CBC modes...\n');
let found = false;

for (const candidate of candidateKeys) {
    if (candidate.key.length !== 16) continue;
    
    // ECB
    let result = tryDecrypt(known.ciphertext, candidate.key, 'aes-128-ecb', '');
    if (result === known.plaintext) {
        console.log(`*** SUCCESS ECB: ${candidate.name} ***`);
        console.log(`Key (hex): ${candidate.key.toString('hex')}`);
        found = true;
        
        // Test on unknown ciphertexts
        console.log('\nDecrypting unknown ciphertexts:');
        unknownCiphertexts.forEach(u => {
            const dec = tryDecrypt(u.ciphertext, candidate.key, 'aes-128-ecb', '');
            console.log(`  ${u.account}: ${dec || 'FAILED'}`);
        });
        break;
    }
    
    // CBC with zero IV
    result = tryDecrypt(known.ciphertext, candidate.key, 'aes-128-cbc', Buffer.alloc(16, 0));
    if (result === known.plaintext) {
        console.log(`*** SUCCESS CBC (zero IV): ${candidate.name} ***`);
        console.log(`Key (hex): ${candidate.key.toString('hex')}`);
        found = true;
        
        console.log('\nDecrypting unknown ciphertexts:');
        unknownCiphertexts.forEach(u => {
            const dec = tryDecrypt(u.ciphertext, candidate.key, 'aes-128-cbc', Buffer.alloc(16, 0));
            console.log(`  ${u.account}: ${dec || 'FAILED'}`);
        });
        break;
    }
    
    // CBC with appKey as IV
    result = tryDecrypt(known.ciphertext, candidate.key, 'aes-128-cbc', Buffer.from(appKey, 'hex'));
    if (result === known.plaintext) {
        console.log(`*** SUCCESS CBC (appKey IV): ${candidate.name} ***`);
        console.log(`Key (hex): ${candidate.key.toString('hex')}`);
        found = true;
        break;
    }
}

if (!found) {
    console.log('No key found with standard derivations.\n');
    
    // Try brute force on simple patterns
    console.log('Trying simple patterns...\n');
    
    // Pattern: repeating character
    for (let c = 0; c < 256; c++) {
        const key = Buffer.alloc(16, c);
        let result = tryDecrypt(known.ciphertext, key, 'aes-128-ecb', '');
        if (result === known.plaintext) {
            console.log(`*** SUCCESS: repeating char 0x${c.toString(16).padStart(2, '0')} ***`);
            break;
        }
    }
    
    // Pattern: incrementing from a base
    for (let base = 0; base < 256; base++) {
        const key = Buffer.alloc(16);
        for (let i = 0; i < 16; i++) key[i] = (base + i) % 256;
        let result = tryDecrypt(known.ciphertext, key, 'aes-128-ecb', '');
        if (result === known.plaintext) {
            console.log(`*** SUCCESS: incrementing from 0x${base.toString(16).padStart(2, '0')} ***`);
            break;
        }
    }
}

// Try XOR analysis (in case it's XOR, not AES)
console.log('\n=== XOR Analysis ===\n');

// If it were simple XOR with 16-byte key
const paddedPlaintext = pad(known.plaintext);
console.log('Padded plaintext length:', paddedPlaintext.length);
console.log('Ciphertext length:', ciphertextBytes.length);

if (paddedPlaintext.length === ciphertextBytes.length) {
    const xorKey = Buffer.alloc(ciphertextBytes.length);
    for (let i = 0; i < ciphertextBytes.length; i++) {
        xorKey[i] = paddedPlaintext[i] ^ ciphertextBytes[i];
    }
    console.log('Derived XOR key:', xorKey.toString('hex'));
    
    // Test this XOR key on unknown ciphertext
    console.log('\nTesting XOR key on unknown ciphertexts:');
    unknownCiphertexts.forEach(u => {
        const ct = Buffer.from(u.ciphertext, 'base64');
        const decrypted = Buffer.alloc(ct.length);
        for (let i = 0; i < ct.length; i++) {
            decrypted[i] = ct[i] ^ xorKey[i % xorKey.length];
        }
        // Remove PKCS7 padding
        const padLen = decrypted[decrypted.length - 1];
        const unpadded = padLen <= 16 && padLen > 0 ? 
            decrypted.slice(0, decrypted.length - padLen) : decrypted;
        const str = unpadded.toString('utf8');
        // Check if result looks like Chinese text
        const isChinese = /[\u4e00-\u9fa5]/.test(str);
        console.log(`  ${u.account}: ${str} ${isChinese ? '(looks like Chinese)' : ''}`);
    });
}

console.log('\n=== Done ===');

