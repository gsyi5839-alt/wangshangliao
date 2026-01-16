# ZCG 逆向分析完整报告

## 分析日期: 2026-01-15

## 1. 启动流程

`
run.cmd 
  └─> xplugin.exe [授权Token参数]
        ├─> 验证授权Token
        ├─> 启动HPSocket服务器监听端口5749和14745
        │
        ├─> 621705120.exe (YX_Clinent) 连接到 127.0.0.1:5749
        │     └─> 使用YX_Client.dll中的NIM凭证
        │     └─> 调用nim.dll进行NIM SDK登录
        │
        └─> zc25.12.11.exe (主框架) 连接到 127.0.0.1:14745
              └─> 发送API请求
              └─> 接收消息回调
`

## 2. 核心组件分析

### 2.1 run.cmd
- **功能**: 启动脚本
- **内容**: 启动xplugin.exe并传递授权Token
- **Token格式**: JWT-like, 三段Base64 (Header.Payload.Signature)

### 2.2 xplugin.exe
- **语言**: Go
- **大小**: 4.7MB
- **架构**: x64
- **导出函数**: 
  - xp_336a451a883a_doClientV1
  - xp_336a451a883a_doClientV2
- **监听端口**:
  - 5749: NIM客户端连接 (621705120.exe)
  - 14745: 主框架连接 (zc25.12.11.exe)

### 2.3 xplugin.dll
- **大小**: 7.2MB
- **功能**: API模块，处理消息转发
- **导出函数**:
  - xp_336a451a883a_doClientV1
  - xp_336a451a883a_doClientV2

### 2.4 zc25.12.11.exe (主框架)
- **大小**: 70MB
- **功能**: 主界面和业务逻辑
- **HPSocket调用**:
  - HP_Server_Start/Stop/Send
  - HP_Client_Connect
- **NIM SDK调用**:
  - nim_client_init
  - nim_client_login/logout
  - nim_talk_send_msg
  - nim_talk_reg_receive_cb
  - nim_talk_reg_ack_cb

### 2.5 621705120.exe (NIM客户端)
- **位置**: YX_Clinent/621705120/
- **功能**: NIM SDK封装客户端
- **依赖**: nim.dll, h_available.dll, HPSocket4C.dll

## 3. NIM凭证配置 (YX_Client.dll)

`ini
[SVRINFO]
SVR_IP=127.0.0.1
SVR_PORT=5749
APP_KEY=b03cfcd909dbf05c25163cc8c7e7b6cf
USER_ID=1628907626      # NIM accid
USER_PWD=Pm1tYOyYQxzfNCZY25-xtIKwxW3UKIWVD07jf9Nlfg4  # NIM token
OLD_USER=621705120      # 旺旺号
OLD_PASS=7z0R1DYpDPhiAC8jdxgvfw==
S_TOKEN=9O1_UQZcDpme9Qg8kOJ4KdyI3tPa3ICdR-bIXI2fRAdeVT_cZn8CXQetkggkI-hqFidPmRubt_VQpbi7fbL_1ZAaO_zwxmrPkvUjq5gB43arNnBL3xen2io89D-B0_hbPKJCuDc0wuBJ2g1dR_PgrLLM8FHGh6qkOjo2f1aMQfpO-syteMoVex0
UID=9502248
`

## 4. 通信协议

### 4.1 HPSocket Pack协议
- **PackHeaderFlag**: 0x0000 (无包头标记)
- **消息格式**:
`
[4字节: 总长度]
[1字节: Type]
[1字节: TimeLen]
[N字节: 时间字符串]
[4字节: MsgType]
[4字节: JsonLen]
[N字节: JSON数据]
`

### 4.2 JSON消息字段
发送消息:
- client_msg_id: 客户端消息ID
- msg_type: 消息类型 (0=文本, 1=图片, ...)
- msg_attach: 消息附件
- to_accid: 目标accid
- to_type: 目标类型 (0=P2P, 1=Team)

接收消息:
- from_accid: 发送者accid
- from_nick: 发送者昵称
- msg_body: 消息内容
- session_id: 会话ID

## 5. NIM SDK调用

### 5.1 初始化
`c
nim_client_init(data_dir, "", config_json)
nim_client_reg_disconnect_cb(callback)
nim_client_reg_kickout_cb(callback)
nim_talk_reg_receive_cb(callback)
nim_talk_reg_ack_cb(callback)
nim_sysmsg_reg_sysmsg_cb(callback)
`

### 5.2 登录
`c
nim_client_login(app_key, accid, token, callback, "")
`

### 5.3 发送消息
`c
nim_talk_send_msg(json_msg, "", callback, "")
`

消息JSON格式:
`json
{
    "msg_type": 0,
    "to_type": 1,
    "talk_id": "群ID",
    "msg_body": "消息内容",
    "client_msg_id": "UUID",
    "timetag": 时间戳,
    "push_enable": 1,
    "need_push_nick": 1
}
`

## 6. 授权Token分析

Token格式: 段0.段1.段2

- **段0** (Header): 版本标识 (01 00 00 00 ...)
- **段1** (Payload): 授权数据 (0D 00 00 00 ...)
- **段2** (Signature): 签名 (256字节)

## 7. 关键发现

1. **授权验证**: xplugin.exe需要有效的授权Token才能启动
2. **端口分离**: 两个端口分别处理NIM通信(5749)和框架API(14745)
3. **NIM直连**: 可以绕过xplugin直接使用nim.dll进行通信
4. **AppKey固定**: b03cfcd909dbf05c25163cc8c7e7b6cf

## 8. 建议实现方案

1. **方案A**: 直接使用nim.dll
   - 复制nim.dll和依赖DLL
   - 使用ZCG的AppKey
   - 直接调用nim_client_login和nim_talk_send_msg

2. **方案B**: 实现自己的代理服务器
   - 替代xplugin.exe的功能
   - 监听端口5749
   - 转发NIM SDK调用

---
*报告生成: Ghidra + PowerShell 静态分析*
