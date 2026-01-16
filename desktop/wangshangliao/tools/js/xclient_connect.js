/**
 * xclient 完整连接测试
 * 使用提取的KEYS进行初始化
 */

const net = require('net');

const XCLIENT_PORT = 21303;

// 从旺商聊提取的KEYS
const KEYS = {
    development: "AgAAAAAAAAB7w0xkTV3nnZ3HEzpDESHyFwCvFX-b3YhdCUwPtb2PAXZgWsH3Y7clf7E0x0mTwFnvkcDX4xRrc_R83VsxxKjnrPSE6gY43TeB9QGj4XjKTgN7wgUDlRWi6z_WnHOJ1w8.BgAAAAAAAACNs15mtTPLWrfGKfiDgqzGVxkMi-UbWEYxU0c6JKqO-BkT8LiCBX36YFbio4zqa5g3lexRzffG-it8h-8wuLbgkRogdcHZRNjxzXVwJYdAJRKuRS9LVwePvL743KzvLQQ.RlBIxOkAOFiT5Ri-QE79e9GsPiZWoJCL5HEkBLtd90gR0Fl8T43QqycK_BlhjvqiTdUVncVXBK_jEIWbI6R3WqqFG8s1eQm_NjktbMZ-cGZ8lG4oiFT1kmEL7QGZFlAL9Lw2GloykmDwYg9QX8q0Bc0Kjp8oxzUmO8QGWJDoNGGWGU9Fojjx8-Iugwg1pSTnpauHfS1Kg23cQ7J_rnvgBQJ_k0WcuPYVxhtj0mXjVz3s-C-iVIcvGrQ9aXcImgZsiMRDORHYiAIRTgOg3wJEFwIHqIoN-qRBT2KGU0d5Lz4YGkV14p21IGrzcfjk1T37fPv0_mGfia0GM91-gPfaNo3PJB4-AEeNlPmLtkWZ5y_tcMe1lp9s1ae-iw0fjDsQYM6qbcToYwvJIzyILncG7_LmNUATJp68Ms-BIgWic8gpP_cJscQqTttypZ2gmLdjbKBmwhbGjd7uzGKs4RHmfsw7APrZKg",
    staging: "AQAAAAAAAABVZpRGB3OJe9Dyoq00-TJOa2pd5S6a67DOgJ99jytqiIhObUjAyyD77MjQh7N_S-I8aUuWn3Q8Ay-rJmQainc91KXKn4ZOdyXYysCqCnsn7cy4kDddZI4lFB15zsLJmAw.FQAAAAAAAAB6xn0T-tdhh0jp-3HZyaNVHt7y-EShR5GATUWFO58I_N_NmhJngEr-rGhhc3-A1XaM85WjFogQcoQp1ddthLL3fUz1zmff_Ts7CFdtjdLImZzE18FuXXvHUF7aOsqPLww.VfCHxLmsjUkUNkBKxoB6xk7_fNk63vEj5A8L-vq1iaHbhHmTFfTw_ZoyNv83kU3jWa2K7L1AryIPHLrGfo8I17BTAWHoXS2ArnABzbDlvju7xkpZmBc8kAvNsCBgbegJ1Fbzl2jeGs6ecc11or4f1VWVX5WuxlS10Hso9oHBY-1va-BETYRITRRx4-lvFE8ZU8N77NPf37lewR5q9swWB4Zb_ZguyzSPr-LiDAiW_LgxuadXWkuvVZB61UUxqpCVjfMlgKcvO6OSUjDSjre1OMrc5KqjOrxuxHoncRqiFEs1qYUYS6oZ1-loLwUPsut6T2n3jwvqnVg0ByKgRMvMEV88di8Nd0lYjKhv3hJFlVRo39xl1P1dwg_hrecZLrTcnlxT0AEmrKLFNSHzZUCUr7ply6Jf_1GdlPWExjn5zvtUNiUV2ISzchc6c0sd4vehKp1Q_NP1ESfIq6jJX1w2cCk2pHoybaDp0LGMAaI",
    production: "AQAAAAAAAABVZpRGB3OJe9Dyoq00-TJOa2pd5S6a67DOgJ99jytqiIhObUjAyyD77MjQh7N_S-I8aUuWn3Q8Ay-rJmQainc91KXKn4ZOdyXYysCqCnsn7cy4kDddZI4lFB15zsLJmAw.BQAAAAAAAADtfRTVtWKpfDUEurA5Fcnfc0RyzFFihon90jVIMwP5_ONYiaD0hpuhNNi0SC6gXccyvszPCW3rb1KEaJ1vNMXW1O66AF7-6kiWsFPYhbCZ2j0WUXXNvo-TR8mK-hvALQY.RcRGxEnFfFgRzNkgE-Vng9ZyomzErNnE-jDO9ECGtT_DQH-PZe1jifFjJkB84trVoofwUWOTg8_b2p9S9036GXMRAPK6m_crEPu-ynoLZC8q0iJR9uR-6qhAAeCPjmwMhmecSgaYiixrj9l1twdmS18CwYkwe_G73fsBVaIZnP4xDsCtGG176nkPhstf1MN_qGi2joHRx7saPI9C2kY0WZjjChqOVdZ3zIT_lmtmJbypOYztWrnCV5sCr2FV1Ph_j3kniDDWJNpCFAsq9a-oDf4FGVm96Vgl_4DJ8eF8Q4WSdFbm7JR1H4-HkJNZ7pPrZ-esB8Heo_c-LeHYEleJHaapqFgos9gPDd5TOItaBH4SdbOOiBxmSqMqIpIWQEbfP-Bk3Z-SjqAxcSxrQeGtd0KMBXd2GbV2NfoCJU9_nmrTFzlQfG_Ei40eNk0BDJ7htD2Iscy8_ObJcTSzgy7Ju40WkKqQHsDVM9iaasRJ6_87XVzeocCaaWrt1vYH1doRBQ"
};

// 构建消息
function buildMessage(type, flags, json) {
    const jsonBytes = Buffer.from(JSON.stringify(json));
    const header = Buffer.alloc(6);
    header[0] = type;
    header[1] = flags;
    header.writeUInt32BE(jsonBytes.length, 2);
    return Buffer.concat([header, jsonBytes]);
}

// 解析响应
function parseResponse(data) {
    if (data.length < 6) return { error: '响应太短' };
    
    const type = data[0];
    const flags = data[1];
    const len = data.readUInt32BE(2);
    const payload = data.length > 6 ? data.slice(6) : Buffer.alloc(0);
    
    return { type, flags, len, payload, payloadStr: payload.toString('utf8') };
}

// 发送消息并等待响应
function sendMessage(json, type = 1, flags = 0) {
    return new Promise((resolve, reject) => {
        const client = new net.Socket();
        const timeout = setTimeout(() => {
            client.destroy();
            reject(new Error('超时'));
        }, 3000);
        
        client.connect(XCLIENT_PORT, '127.0.0.1', () => {
            const msg = buildMessage(type, flags, json);
            console.log(`发送: ${JSON.stringify(json)}`);
            console.log(`HEX: ${msg.toString('hex')}`);
            client.write(msg);
        });
        
        client.on('data', (data) => {
            clearTimeout(timeout);
            const response = parseResponse(data);
            console.log(`响应: Type=${response.type}, Flags=${response.flags}, Len=${response.len}`);
            console.log(`HEX: ${data.toString('hex')}`);
            if (response.payload.length > 0) {
                console.log(`Payload: ${response.payloadStr}`);
            }
            client.end();
            resolve(response);
        });
        
        client.on('error', (e) => {
            clearTimeout(timeout);
            reject(e);
        });
    });
}

async function main() {
    console.log('=== xclient 协议测试 ===\n');
    
    // 测试1: init命令
    console.log('[1. 测试 init 命令]');
    try {
        await sendMessage({ action: 'init', key: KEYS.production });
    } catch (e) {
        console.log('错误:', e.message);
    }
    
    // 测试2: buildin命令
    console.log('\n[2. 测试 buildin 命令]');
    try {
        await sendMessage({ action: 'buildin' });
    } catch (e) {
        console.log('错误:', e.message);
    }
    
    // 测试3: 请求用户信息
    console.log('\n[3. 测试 request 命令]');
    try {
        await sendMessage({ 
            type: 'request',
            url: '/v1/user/get-user-info',
            params: '{}'
        });
    } catch (e) {
        console.log('错误:', e.message);
    }
    
    // 测试4: 不同type值
    console.log('\n[4. 测试不同Type值]');
    for (let t = 0; t <= 3; t++) {
        console.log(`\nType=${t}:`);
        try {
            await sendMessage({ action: 'buildin' }, t, 0);
        } catch (e) {
            console.log('错误:', e.message);
        }
    }
    
    console.log('\n=== 完成 ===');
}

main().catch(console.error);
