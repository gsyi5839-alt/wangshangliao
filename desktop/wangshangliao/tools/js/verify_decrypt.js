// Verify the decryption key found in source code
const crypto = require('crypto');

console.log('=== Verify Decryption Key ===\n');

// Key and IV from source code
const keyString = 'd6ba6647b7c43b79d0e42ceb2790e342';
const ivString = 'kgWRyiiODMjSCh0m';

const key = Buffer.from(keyString, 'utf8');
const iv = Buffer.from(ivString, 'utf8');

console.log('Key (UTF-8):', keyString);
console.log('Key length:', key.length, 'bytes');
console.log('Key (hex):', key.toString('hex'));
console.log('IV (UTF-8):', ivString);
console.log('IV length:', iv.length, 'bytes');
console.log('IV (hex):', iv.toString('hex'));
console.log('');

function decrypt(ciphertextBase64) {
    try {
        const decipher = crypto.createDecipheriv('aes-256-cbc', key, iv);
        decipher.setAutoPadding(true);
        let decrypted = decipher.update(Buffer.from(ciphertextBase64, 'base64'));
        decrypted = Buffer.concat([decrypted, decipher.final()]);
        return decrypted.toString('utf8');
    } catch (e) {
        return `Error: ${e.message}`;
    }
}

// Test with known pair
console.log('=== Test Known Pair ===\n');
const knownCiphertext = 'nDt2pNsxc6t+8U74M/qn3w=='; // 法拉利客服
const expectedPlaintext = '法拉利客服';

const result = decrypt(knownCiphertext);
console.log('Ciphertext:', knownCiphertext);
console.log('Expected:', expectedPlaintext);
console.log('Decrypted:', result);
console.log('Match:', result === expectedPlaintext ? '✓ SUCCESS!' : '✗ FAILED');
console.log('');

// Test with unknown ciphertexts
console.log('=== Decrypt Unknown Ciphertexts ===\n');

const unknownCiphertexts = [
    { account: '1391351554', ciphertext: 'acMVOxzdhuOhtn+lyr28+g==' },
    { account: '1393372631', ciphertext: 'HLF4vIVpTTX9OuKnY1gW6g==' },
    { account: '2101370650', ciphertext: 'SNtGTu6r6JHF0GKHFz2fCw==' },
    { account: '1598362464', ciphertext: 'uLU+SdEvJnP9tb07iJh8vw==' },
    { account: '1628907626', ciphertext: '71a029a8d4e99f0be04834dbec9260b0' }, // This might be MD5, not ciphertext
];

unknownCiphertexts.forEach(u => {
    const dec = decrypt(u.ciphertext);
    console.log(`${u.account}: ${u.ciphertext}`);
    console.log(`  -> ${dec}`);
    console.log('');
});

// If the key is 32 characters but needs to be treated as hex (16 bytes)
console.log('=== Try Key as Hex (if needed) ===\n');

const keyHex = Buffer.from(keyString, 'hex');
console.log('Key as hex:', keyHex.toString('hex'));
console.log('Key hex length:', keyHex.length, 'bytes');

if (keyHex.length === 16) {
    function decryptWithHexKey(ciphertextBase64) {
        try {
            const decipher = crypto.createDecipheriv('aes-128-cbc', keyHex, iv);
            decipher.setAutoPadding(true);
            let decrypted = decipher.update(Buffer.from(ciphertextBase64, 'base64'));
            decrypted = Buffer.concat([decrypted, decipher.final()]);
            return decrypted.toString('utf8');
        } catch (e) {
            return `Error: ${e.message}`;
        }
    }
    
    const resultHex = decryptWithHexKey(knownCiphertext);
    console.log('Decrypted with hex key:', resultHex);
    console.log('Match:', resultHex === expectedPlaintext ? '✓ SUCCESS!' : '✗ FAILED');
}

console.log('\n=== Done ===');

