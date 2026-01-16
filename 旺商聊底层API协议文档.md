# 旺商聊(YX)底层API协议完整文档

> **深度逆向分析文档** - 2026年1月11日
> 基于实际运行日志和二进制分析的真实协议

---

## 1. 核心架构概述

### 1.1 软件组件层次
```
┌─────────────────────────────────────────────────────────────┐
│                      用户界面层 (zcg.exe)                    │
├─────────────────────────────────────────────────────────────┤
│                  机器人插件层 (zcg.dll)                      │
├─────────────────────────────────────────────────────────────┤
│              框架通信层 (xplugin.exe + HPSocket)             │
├─────────────────────────────────────────────────────────────┤
│              IM客户端层 (621705120.exe + nim.dll)            │
├─────────────────────────────────────────────────────────────┤
│                    网易云信NIM SDK层                         │
├─────────────────────────────────────────────────────────────┤
│                 网易云信服务器 (*.netease.im)                │
└─────────────────────────────────────────────────────────────┘
```

### 1.2 进程通信关系
```
621705120.exe (NIM客户端)
       ↓ HPSocket TCP
xplugin.exe (框架)
       ↓ HPSocket TCP (端口14745)
zcg.dll (机器人插件)
       ↓
zcg.exe (用户界面)
```

---

## 2. 网易云信NIM SDK底层API

### 2.1 SDK初始化配置

**nim_client_init** 调用参数 (从621705120.exe提取):

```json
{
    "global_config": {
        "db_encrypt_key": "YXSDK",
        "comm_enca": 1,
        "nego_key_neca": 1,
        "hand_shake_type": 1,
        "use_https": true,
        "sdk_log_level": 5,
        "sync_session_ack": true,
        "team_msg_ack": false,
        "animated_image_thumbnail_enabled": false,
        "caching_markread_enabled": false,
        "caching_markread_count": 10,
        "caching_markread_time": 1000,
        "client_antispam": false,
        "dedicated_cluste_flag": false,
        "enable_user_datafile_backup": true,
        "enable_user_datafile_restore": false,
        "enable_user_datafile_defrestoreproc": false,
        "ip_protocol_version": 0,
        "login_retry_max_times": 0,
        "need_update_lbs_befor_relogin": false,
        "preload_attach": true,
        "preload_image_name_template": "thumb_",
        "preload_image_quality": -1,
        "preload_image_resize": "",
        "push_cer_name": "",
        "push_token": "",
        "reset_unread_count_when_recall": true,
        "team_notification_unread_count": false,
        "upload_sdk_events_after_login": false,
        "user_datafile_localbackup_folder": "",
        "vchat_miss_unread_count": false
    }
}
```

### 2.2 NIM SDK 核心函数

从 `nim.dll` 导出的关键函数：

| 函数名 | 功能 | 参数说明 |
|--------|------|----------|
| `nim_client_init` | SDK初始化 | app_data_dir, app_install_dir, json_extension |
| `nim_client_login` | 登录 | app_key, accid, token, json_extension, callback |
| `nim_client_logout` | 登出 | logout_type, json_extension, callback |
| `nim_client_get_login_state` | 获取登录状态 | 返回0-3 |
| `nim_client_reg_disconnect_cb` | 注册断开回调 | callback |
| `nim_client_reg_kickout_cb` | 注册踢出回调 | callback |
| `nim_client_reg_multispot_login_notify_cb` | 多端登录通知 | callback |
| `nim_talk_send_msg` | 发送消息 | json_msg, json_extension, callback |
| `nim_talk_reg_receive_cb` | 注册接收消息回调 | callback |
| `nim_talk_reg_ack_cb` | 注册发送确认回调 | callback |
| `nim_sysmsg_reg_sysmsg_cb` | 注册系统消息回调 | callback |
| `nim_tool_get_uuid` | 获取UUID | 返回消息ID |

### 2.3 服务器地址

| 服务 | 域名 | 功能 |
|------|------|------|
| LBS服务器 | `lbs.netease.im` | 负载均衡，获取最优连接节点 |
| 长连接服务器 | `link.netease.im` | 消息实时推送 |
| 文件服务器 | `nos.netease.com` | 文件上传下载 |
| IPv4检测 | `check-ipv4.netease.im` | 网络检测 |
| IPv6检测 | `check-ipv6.netease.im` | 网络检测 |
| 应用服务 | `app.netease.im` | 应用配置 |

---

## 3. 登录流程详解

### 3.1 登录步骤 (login_step)

```
login_step = 0  → 开始登录
login_step = 2  → 连接服务器中
login_step = 3  → 登录成功
```

### 3.2 登录回调JSON格式

```json
{
    "err_code": 200,
    "login_step": 3,
    "relogin": false,
    "retrying": false
}
```

### 3.3 登录成功通知框架

```json
{
    "id": "1628907626",           // NIM accid
    "ret": "登录成功！",
    "err_code": "200"
}
```

### 3.4 账号映射关系

| 类型 | 旺商聊ID | NIM ID | 说明 |
|------|----------|--------|------|
| 机器人号 | 621705120 | 1948408648 (accid) | 用于发消息的主账号 |
| 群号 | 3962369093 | 40821608989 (tid) | 高级群/团队 |

---

## 4. 消息协议详解

### 4.1 框架到IM客户端的消息格式

**发送消息请求格式:**
```json
{
    "msg": "{\"b\":\"加密的消息内容Base64\"}",
    "TYPE": "1",                    // 消息类型: 1=群消息
    "id": "40821608989",            // 目标群tid
    "msgid": "e06a6f71-eead-11f0-9f53-a3225e568098"  // UUID格式
}
```

### 4.2 NIM消息JSON完整结构

**接收消息结构:**
```json
{
    "content": {
        "client_msg_id": "UUID格式",
        "server_msg_id": 2739491805904148218,
        "time": 1768109104083,
        "from_client_type": 32,         // 32=服务端
        "from_device_id": "",
        "from_id": "1628907626",        // 发送者accid
        "from_nick": "加密的昵称MD5",
        "to_accid": "40821608989",      // 接收者
        "to_type": 1,                   // 0=私聊, 1=群聊
        "talk_id": "40821608989",       // 会话ID
        "msg_type": 100,                // 消息类型
        "msg_sub_type": 0,
        "msg_body": "",
        "msg_attach": "{\"b\":\"...\"}",  // 附件数据
        "cloud_history": 1,
        "offline_msg": 1,
        "push_enable": 1,
        "push_need_badge": 1,
        "roam_msg": 1,
        "sync_msg": 1,
        "log_status": 1,
        "log_sub_status": 0,
        "resend_flag": 0,
        "routable_msg": 1,
        "robot_info": {
            "account": "",
            "content": "",
            "function": "",
            "topic": ""
        }
    },
    "feature": 0,
    "rescode": 200
}
```

### 4.3 消息类型 (msg_type)

| 值 | 类型 | 说明 |
|----|------|------|
| 0 | 文本消息 | 普通文本 |
| 1 | 图片消息 | 图片附件 |
| 2 | 语音消息 | 音频附件 |
| 3 | 视频消息 | 视频附件 |
| 5 | 通知消息 | 群操作通知 |
| 100 | 自定义消息 | 旺商聊业务消息 |

### 4.4 发送消息回调

```json
{
    "anti_spam_res": "",
    "msg_id": "e06a6f71-eead-11f0-9f53-a3225e568098",
    "msg_id_server": 2739491805904148218,
    "msg_timetag": 1768109103847,
    "rescode": 200,
    "talk_id": "40821608989",
    "third_party_callback_ext": ""
}
```

---

## 5. 消息内容加密分析

### 5.1 msg_attach.b 字段格式

旺商聊的实际消息内容存储在 `msg_attach` 的 `b` 字段中，使用 **URL安全的Base64编码** + **自定义二进制协议**。

**示例:**
```
CRlJHwGg-EMCESw0Y2kAAAAAGTicP-2cOMpvIkR0jhEjdh_f75KtQ7S0m7LuQMSy1tEpd-DsHref6g...
```

### 5.2 Base64解码后的二进制结构

| 偏移 | 字段 | 说明 |
|------|------|------|
| 0-3 | 头部标识 | 09 1A 49 1F (固定) |
| 4-7 | 版本/类型 | 01 A0 F8 43 |
| 8-11 | 消息ID前缀 | 02 11 2C 34 |
| 12+ | 加密数据 | 消息实际内容 |

### 5.3 群禁言通知解析

**msg_sub_type 值:**
- `NOTIFY_TYPE_GROUP_MUTE_0` - 解除禁言
- `NOTIFY_TYPE_GROUP_MUTE_1` - 开启禁言
- `NOTIFY_TYPE_USER_UPDATE_NAME` - 用户改名

**禁言通知数据:**
```json
{
    "data": {
        "team_info": {
            "mute_all": 0,           // 0=解禁, 1=禁言
            "mute_type": 0,
            "tid": "40821608989",
            "update_timetag": 1768109103931
        },
        "name_cards": [...]
    },
    "id": 3
}
```

---

## 6. HPSocket本地通信协议

### 6.1 端口配置

文件: `zcg端口.ini`
```ini
[端口]
端口=14745
```

### 6.2 框架API调用日志格式

文件: `YX_Clinent\log1\*调用日志.txt`

```
时间   API名称|参数1|参数2|...|返回结果:Base64编码
```

**API列表:**

| API名称 | 参数 | 返回值 |
|---------|------|--------|
| `发送群消息（文本）` | 机器人号\|消息内容\|群号\|类型\|子类型 | 发送结果 |
| `云信_获取在线账号` | 无 | 在线账号列表 |
| `取绑定群` | 机器人号 | 绑定的群列表 |
| `ww_群禁言解禁` | 机器人号\|群号\|操作(1=禁言,2=解禁) | 操作结果 |
| `ww_改群名片` | 机器人号\|群号\|用户ID\|新名片 | 操作结果 |
| `ww_ID互查` | 旺商聊ID | NIM accid |

### 6.3 调用示例

```
2026年1月11日13时25分1秒   发送群消息（文本）|621705120|開:9 + 2 + 5 = 16 DAS -- H\n人數:0  總分:0\n...|3962369093|1|0|返回结果:Dg15wK9Ua6C+fcRgZoN3NQ==
```

```
2026年1月11日13时25分1秒   ww_群禁言解禁|621705120|3962369093|2|返回结果:iIEcyahRpLaUj9x+zriHjm1S4/uHjzwXxIvTKtsxAjWRDmEkKr+/mNOeaxfIZl28
```

---

## 7. 配置文件详解

### 7.1 config.ini

```ini
[1628907626]                    ; NIM accid 作为配置段名
机器人=621705120                 ; 对应的旺商聊号
jwtToken=DMKxshoPePzQK08U...    ; 登录令牌(128字节,自定义加密)
qun=iQEpb67flWe6pIT36InkpA==    ; 绑定的群(16字节AES加密)
nickName=......                  ; 昵称
自动登录=1
拉取历史=0
```

### 7.2 数据加密

**JWT Token:**
- 长度: 128字节
- 格式: 自定义加密，非标准JWT
- 用于身份验证

**群号加密:**
- 算法: 16字节AES
- 密钥: 待分析

---

## 8. 实现代码示例

### 8.1 Python - NIM SDK调用封装

```python
import ctypes
import json
import uuid
from typing import Callable, Optional

class NIMClient:
    """网易云信NIM SDK Python封装"""
    
    def __init__(self, nim_dll_path: str = "nim.dll"):
        self.nim = ctypes.CDLL(nim_dll_path)
        self._setup_callbacks()
    
    def _setup_callbacks(self):
        """设置回调函数类型"""
        # 登录回调: void (*)(const char* json_result, const void* user_data)
        self.LoginCallback = ctypes.CFUNCTYPE(None, ctypes.c_char_p, ctypes.c_void_p)
        # 消息回调: void (*)(const char* json_msg, const void* user_data)
        self.MessageCallback = ctypes.CFUNCTYPE(None, ctypes.c_char_p, ctypes.c_void_p)
    
    def init(self, app_data_dir: str, app_install_dir: str) -> bool:
        """初始化SDK"""
        config = {
            "global_config": {
                "db_encrypt_key": "YXSDK",
                "comm_enca": 1,
                "nego_key_neca": 1,
                "hand_shake_type": 1,
                "use_https": True,
                "sdk_log_level": 5
            }
        }
        
        result = self.nim.nim_client_init(
            app_data_dir.encode('utf-8'),
            app_install_dir.encode('utf-8'),
            json.dumps(config).encode('utf-8')
        )
        return result == 0
    
    def login(self, app_key: str, accid: str, token: str, 
              callback: Callable[[dict], None]) -> None:
        """登录"""
        login_info = {
            "accid": accid,
            "token": token,
            "app_key": app_key
        }
        
        @self.LoginCallback
        def _callback(json_result, user_data):
            result = json.loads(json_result.decode('utf-8'))
            callback(result)
        
        self.nim.nim_client_login(
            app_key.encode('utf-8'),
            json.dumps(login_info).encode('utf-8'),
            b"{}",  # json_extension
            _callback,
            None
        )
    
    def send_team_message(self, tid: str, msg_content: dict) -> str:
        """发送群消息"""
        msg_id = str(uuid.uuid1())
        
        msg = {
            "talk_id": tid,
            "to_type": 1,  # 群聊
            "msg_type": 100,  # 自定义消息
            "msg_attach": json.dumps(msg_content),
            "client_msg_id": msg_id
        }
        
        self.nim.nim_talk_send_msg(
            json.dumps(msg).encode('utf-8'),
            b"{}",
            None
        )
        
        return msg_id
    
    def register_receive_callback(self, callback: Callable[[dict], None]) -> None:
        """注册消息接收回调"""
        @self.MessageCallback
        def _callback(json_msg, user_data):
            msg = json.loads(json_msg.decode('utf-8'))
            callback(msg)
        
        self._receive_callback = _callback  # 保持引用
        self.nim.nim_talk_reg_receive_cb(b"", _callback, None)
```

### 8.2 Python - 框架通信封装

```python
import socket
import json
import threading
from typing import Callable

class FrameworkClient:
    """与xplugin框架的HPSocket通信"""
    
    def __init__(self, host: str = "127.0.0.1", port: int = 14745):
        self.host = host
        self.port = port
        self.socket: Optional[socket.socket] = None
        self.running = False
        self._callbacks = []
    
    def connect(self) -> bool:
        """连接到框架"""
        try:
            self.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            self.socket.connect((self.host, self.port))
            self.running = True
            
            # 启动接收线程
            thread = threading.Thread(target=self._receive_loop, daemon=True)
            thread.start()
            
            return True
        except Exception as e:
            print(f"连接失败: {e}")
            return False
    
    def _receive_loop(self):
        """接收消息循环"""
        buffer = b""
        while self.running:
            try:
                data = self.socket.recv(4096)
                if not data:
                    break
                    
                buffer += data
                # 解析消息 (简化处理)
                try:
                    msg = json.loads(buffer.decode('utf-8'))
                    for callback in self._callbacks:
                        callback(msg)
                    buffer = b""
                except json.JSONDecodeError:
                    continue  # 等待更多数据
                    
            except Exception as e:
                print(f"接收错误: {e}")
                break
    
    def send_message(self, tid: str, content: dict, msg_id: str = None):
        """发送消息到框架"""
        if not msg_id:
            import uuid
            msg_id = str(uuid.uuid1())
        
        msg = {
            "msg": json.dumps(content),
            "TYPE": "1",
            "id": tid,
            "msgid": msg_id
        }
        
        # 框架使用特殊格式
        data = json.dumps(msg, ensure_ascii=False)
        self.socket.send(data.encode('utf-8'))
    
    def register_callback(self, callback: Callable[[dict], None]):
        """注册消息回调"""
        self._callbacks.append(callback)
```

### 8.3 Python - 消息解密

```python
import base64
import struct

def decode_wx_message(b64_content: str) -> bytes:
    """解码旺商聊消息内容"""
    # URL安全Base64解码
    content = b64_content.replace('-', '+').replace('_', '/')
    # 补齐padding
    padding = 4 - (len(content) % 4)
    if padding < 4:
        content += '=' * padding
    
    return base64.b64decode(content)

def parse_message_header(data: bytes) -> dict:
    """解析消息头"""
    if len(data) < 12:
        return None
    
    return {
        "magic": data[0:4].hex(),       # 09 1A 49 1F
        "version": data[4:8].hex(),
        "prefix": data[8:12].hex(),
        "payload": data[12:]
    }

# 使用示例
msg_attach = '{"b":"CRlJHwGg-EMCESw0Y2kAAAAAGTicP-2c..."}'
attach = json.loads(msg_attach)
decoded = decode_wx_message(attach['b'])
parsed = parse_message_header(decoded)
print(f"Magic: {parsed['magic']}")
print(f"Payload length: {len(parsed['payload'])}")
```

---

## 9. 关键发现总结

### 9.1 ID映射规则

旺商聊号和NIM ID是**独立的**，需要通过配置文件或API查询映射关系：
- 旺商聊号: 用户可见的ID (如 621705120)
- NIM accid: 云信内部ID (如 1948408648)
- 群号映射: 旺商聊群号 → NIM tid

### 9.2 消息流转

1. **发送流程:**
   ```
   zcg.dll调用API → HPSocket发送到xplugin
   → xplugin转发到621705120.exe
   → 621705120.exe调用nim.dll
   → nim_talk_send_msg发送到云信服务器
   ```

2. **接收流程:**
   ```
   云信服务器推送 → nim_talk_reg_receive_cb回调
   → 621705120.exe处理 → HPSocket发送到xplugin
   → xplugin分发到zcg.dll → 触发消息处理
   ```

### 9.3 可能的AppKey

从二进制分析发现的候选值：
- `d09f2340818511d396f6aaf844c7e325`
- `27bb20fdd3e145e4bee3db39ddd6e64c`

---

## 10. 实际调用方式

要实现与旺商聊的连接，有两种方式：

### 方式1: 使用现有框架 (推荐)
直接对接 `xplugin.exe` 框架，通过HPSocket协议发送/接收消息。

### 方式2: 直接调用NIM SDK
需要：
1. 获取有效的 app_key
2. 获取账号的 accid 和 token
3. 正确初始化 nim.dll
4. 实现消息加解密

---

## 11. 消息Protobuf编码详解

### 11.1 消息二进制结构

```
┌────────────────────────────────────────────────────────────┐
│ 偏移 0-3:  魔数 (09 1A 49 1F 或 09 19 49 1F)              │
│ 偏移 4-7:  版本标识 (01 A0 F8 43 或变体)                  │
│ 偏移 8-11: 消息类型前缀                                    │
│ 偏移 12+:  Protobuf编码的消息体                           │
└────────────────────────────────────────────────────────────┘
```

### 11.2 解密后的Protobuf结构

从实际消息中解析出的内容示例：
```
{"mMuteAllMember":false}  - JSON格式的业务数据
机器人                     - 用户类型标识
管理员关闭了禁言            - 实际显示的消息内容
```

### 11.3 消息类型识别

根据 `msg_sub_type` 识别消息类型：
| 子类型 | 含义 | 解析方式 |
|--------|------|----------|
| `0` | 普通消息 | 解码Protobuf获取文本 |
| `NOTIFY_TYPE_GROUP_MUTE_0` | 解除禁言 | JSON: mMuteAllMember=false |
| `NOTIFY_TYPE_GROUP_MUTE_1` | 开启禁言 | JSON: mMuteAllMember=true |
| `NOTIFY_TYPE_USER_UPDATE_NAME` | 改名 | JSON: afterNickname |

---

## 12. 实际连接方案

### 方案A: 对接现有框架 (最简单)

**原理**: 通过HPSocket TCP连接到 `xplugin.exe`，直接复用其NIM连接。

**优点**: 
- 无需实现NIM SDK调用
- 无需处理账号认证
- 框架已处理好所有底层逻辑

**实现步骤**:
1. 启动 `xplugin.exe` 和 `621705120.exe`
2. TCP连接到 `127.0.0.1:14745`
3. 发送/接收消息使用框架协议

### 方案B: 直接调用NIM SDK (完全控制)

**需要获取**:
1. AppKey (可能是 `d09f2340818511d396f6aaf844c7e325`)
2. accid 和 token (从config.ini或服务器获取)

**实现步骤**:
1. 加载 `nim.dll`
2. 调用 `nim_client_init` 初始化
3. 调用 `nim_client_login` 登录
4. 注册消息回调
5. 使用 `nim_talk_send_msg` 发送消息

---

## 13. 快速验证代码

### Python - 连接框架测试

```python
#!/usr/bin/env python3
"""
旺商聊框架连接测试
连接到xplugin.exe的HPSocket服务
"""

import socket
import json
import time

def connect_framework(host="127.0.0.1", port=14745):
    """连接到框架"""
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.settimeout(5)
    
    try:
        sock.connect((host, port))
        print(f"✓ 成功连接到框架 {host}:{port}")
        return sock
    except Exception as e:
        print(f"✗ 连接失败: {e}")
        return None

def send_test_message(sock, robot_id, group_id, content):
    """发送测试消息"""
    import uuid
    
    msg = {
        "msg": json.dumps({"content": content}),
        "TYPE": "1",
        "id": str(group_id),
        "msgid": str(uuid.uuid1())
    }
    
    data = json.dumps(msg, ensure_ascii=False)
    sock.send(data.encode('utf-8'))
    print(f"→ 发送: {content}")

def receive_messages(sock, timeout=10):
    """接收消息"""
    sock.settimeout(timeout)
    buffer = b""
    
    try:
        while True:
            data = sock.recv(4096)
            if not data:
                break
            buffer += data
            print(f"← 收到: {buffer.decode('utf-8', errors='ignore')}")
    except socket.timeout:
        print("接收超时")

if __name__ == "__main__":
    # 测试连接
    sock = connect_framework()
    if sock:
        # 发送测试
        # send_test_message(sock, "621705120", "3962369093", "测试消息")
        
        # 监听消息
        print("监听消息中...")
        receive_messages(sock)
        
        sock.close()
```

---

*文档版本: 1.1*
*更新日期: 2026-01-11*
*新增: Protobuf编码分析、实际连接方案、测试代码*
