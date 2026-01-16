# 旺商聊登录 API 深度逆向分析

> **注意**: 旺商聊客户端已经登录的情况下，机器人框架**无需调用登录API**，直接从本地存储读取token即可。

## 重要发现

旺商聊客户端使用 `xclient.node` 原生模块处理所有HTTP请求。该模块内部有加密的服务器配置，无法直接调用公开API进行登录。

**推荐方式**: 从旺商聊本地存储 (`config.json`) 直接读取已登录用户的 token 信息。

## 从本地存储获取登录信息

**存储位置**: `%APPDATA%\wangshangliao\config.json`

**关键字段路径**: `production_account[0].userInfo`

```json
{
  "uid": 9903485,
  "nickName": "法拉利",
  "nimId": 1948408648,
  "nimToken": "3GazfQC_eC-310wRx6FBleCad39uu34IuWAuaZdVejQ",
  "accountId": 781361487,
  "token": "1oJ1TQLWsA-Vf5KfCSP6Am...",
  "accountState": "ACCOUNT_STATE_GOOD"
}
```

## 1. 登录类型 (仅需账号密码)

| 登录类型 | 类型标识 | 说明 |
|---------|---------|------|
| 账号密码登录 | `LOGIN_TYPE_ACCOUNT_PWD` | 使用旺商聊账号+密码登录 |

## 2. 登录 API 端点

### 2.1 账号密码登录 (主要使用)
```
POST /v1/user/login
```

> **注意**: 此API通过旺商聊客户端的 xclient.node 内部调用，服务器地址可能与公开的 API 不同。

**请求参数**:
```json
{
  "account": "旺商聊账号",
  "passwd": "密码",
  "type": "LOGIN_TYPE_ACCOUNT_PWD"
}
```

**响应**:
```json
{
  "code": 0,
  "msg": "success",
  "avatar": "头像URL",
  "nickName": "昵称",
  "uid": 用户ID,
  "accountId": 账号ID,
  "token": "登录Token",
  "nimToken": "NIM登录Token",
  "nimId": "NIM账号ID",
  "isBan": false
}
```

## 3. AES 加密配置

从 `config-b87783b6.js` 提取的加密配置：

```javascript
// 加密参数
const ENCRYPTION_KEY = "49KdgB8_9=12+3hF";  // 密钥原文
const IV = "00000000000000000000000000000000";  // IV (32位十六进制)

// 加密配置
{
  iv: CryptoJS.enc.Hex.parse(IV),
  mode: CryptoJS.mode.CBC,
  padding: CryptoJS.pad.Pkcs7
}

// 加密函数
function encrypt(plainText) {
  return CryptoJS.AES.encrypt(
    plainText, 
    CryptoJS.SHA256(ENCRYPTION_KEY),
    {
      iv: CryptoJS.enc.Hex.parse(IV),
      mode: CryptoJS.mode.CBC,
      padding: CryptoJS.pad.Pkcs7
    }
  ).toString();
}

// 解密函数
function decrypt(cipherText) {
  return CryptoJS.AES.decrypt(
    cipherText,
    CryptoJS.SHA256(ENCRYPTION_KEY),
    {
      iv: CryptoJS.enc.Hex.parse(IV),
      mode: CryptoJS.mode.CBC,
      padding: CryptoJS.pad.Pkcs7
    }
  ).toString(CryptoJS.enc.Utf8);
}
```

## 4. 简化登录流程 (客户端已登录)

```
┌─────────────────────────────────────────────────────────────┐
│              机器人框架登录流程 (简化版)                      │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1. 发送登录请求 ─────> POST /v1/user/login                 │
│     └─ { account, passwd, type: "LOGIN_TYPE_ACCOUNT_PWD" }  │
│                                                             │
│  2. 处理登录响应                                             │
│     ├─ code=0: 登录成功, 获取 token, nimToken, nimId        │
│     └─ code=401: 账号或密码错误                             │
│                                                             │
│  3. 使用返回的 token 进行后续 API 调用                       │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

> **说明**: 由于旺商聊客户端已经登录，不需要网易盾验证码和手机短信验证。

## 5. C# 实现示例

```csharp
/// <summary>
/// 从旺商聊本地存储获取登录信息
/// </summary>
public class WangShangLiaoTokenReader
{
    /// <summary>
    /// 读取已登录用户的Token信息
    /// </summary>
    public static LoginInfo ReadFromLocalStorage()
    {
        var configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "wangshangliao", "config.json"
        );
        
        if (!File.Exists(configPath))
            return null;
            
        var json = File.ReadAllText(configPath);
        var config = JsonSerializer.Deserialize<JsonElement>(json);
        
        if (config.TryGetProperty("production_account", out var accounts) 
            && accounts.GetArrayLength() > 0)
        {
            var account = accounts[0];
            if (account.TryGetProperty("userInfo", out var userInfo))
            {
                return new LoginInfo
                {
                    Uid = userInfo.GetProperty("uid").GetInt64(),
                    NickName = userInfo.GetProperty("nickName").GetString(),
                    NimId = userInfo.GetProperty("nimId").GetInt64(),
                    NimToken = userInfo.GetProperty("nimToken").GetString(),
                    AccountId = userInfo.GetProperty("accountId").GetInt64(),
                    Token = userInfo.GetProperty("token").GetString()
                };
            }
        }
        return null;
    }
}

/// <summary>
/// 登录信息
/// </summary>
public class LoginInfo
{
    public long Uid { get; set; }          // 旺旺号
    public string NickName { get; set; }   // 昵称
    public long NimId { get; set; }        // NIM ID
    public string NimToken { get; set; }   // NIM Token (用于IM连接)
    public long AccountId { get; set; }    // 账号ID
    public string Token { get; set; }      // API Token
}
```

## 6. 请求头配置

```
Content-Type: application/json
X-Device: 1
User-Agent: Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36
```

登录成功后，后续请求需要添加：
```
x-token: {token}
x-id: {userId}
```

## 7. 错误码说明

| 错误码 | 说明 |
|-------|------|
| 0 | 成功 |
| 401 | 账号或密码错误 |
| 1069 | 设备变更，需要验证 |
| -10243 | 无用户ID |
| -10261 | 消息错误 |

## 8. 密码规则

从 `config-b87783b6.js` 提取的密码正则：

```javascript
// 密码规则: 6-16位，必须包含数字、字母、特殊字符中的至少两种，不能包含中文
const passwordRegex = /(?!^\d+$)(?!^[A-Za-z]+$)(?!^[^A-Za-z0-9]+$)(?!^.*[\u4E00-\u9FA5].*$)^\S{6,16}$/;

// 手机号规则
const phoneRegex = /^1[3456789]\d{9}$/;
```
