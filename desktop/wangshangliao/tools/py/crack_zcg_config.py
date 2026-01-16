# -*- coding: utf-8 -*-
"""
ZCG 配置文件强制破解工具
解密 C:\zcg25.12.11\config.ini 中的所有加密数据
"""
import os
import sys
import base64
import hashlib
from pathlib import Path

# 尝试导入 pycryptodome
try:
    from Crypto.Cipher import AES
except ImportError:
    print("正在安装 pycryptodome...")
    os.system("pip install pycryptodome -q")
    from Crypto.Cipher import AES

# ============ AES-256-CBC 解密 (用于 qun, jwtToken) ============
CONFIG_KEY_STR = '49KdgB8_9=12+3hF'
CONFIG_KEY = hashlib.sha256(CONFIG_KEY_STR.encode()).digest()  # 32字节
CONFIG_IV = bytes(16)  # 全零IV

def decrypt_aes_config(encrypted_b64):
    """解密 AES 加密的配置项 (qun, jwtToken)"""
    if not encrypted_b64:
        return ""
    try:
        encrypted = base64.b64decode(encrypted_b64)
        cipher = AES.new(CONFIG_KEY, AES.MODE_CBC, CONFIG_IV)
        decrypted = cipher.decrypt(encrypted)
        # PKCS7 去填充
        padding = decrypted[-1]
        if 0 < padding <= 16:
            decrypted = decrypted[:-padding]
        return decrypted.decode('utf-8')
    except Exception as e:
        return f"[解密失败: {e}]"

# ============ XOR+Hex 解密 (用于文本配置) ============
XOR_KEY = 0x10
VALUE_SUFFIX = "20CB5D79B"

def decrypt_xor_text(encrypted):
    """解密 XOR+Hex 加密的文本"""
    if not encrypted:
        return ""
    try:
        data = encrypted
        # 移除后缀
        if data.endswith(VALUE_SUFFIX):
            data = data[:-len(VALUE_SUFFIX)]
        elif data.endswith("CB5D79B"):
            data = data[:-7]
        
        # Hex解码 + XOR
        bytes_data = bytes([int(data[i:i+2], 16) ^ XOR_KEY for i in range(0, len(data), 2)])
        return bytes_data.decode('gb2312')
    except:
        return encrypted

def decrypt_xor_number(encrypted):
    """解密数值 (每位字符-0x20)"""
    if not encrypted:
        return "0"
    try:
        data = encrypted
        if data.endswith(VALUE_SUFFIX):
            data = data[:-len(VALUE_SUFFIX)]
        
        result = ""
        for i in range(0, len(data), 2):
            b = int(data[i:i+2], 16)
            result += chr(b - 0x20)
        return result
    except:
        return "0"

def is_aes_encrypted(value):
    """判断是否为 AES 加密 (Base64 格式)"""
    if not value:
        return False
    try:
        decoded = base64.b64decode(value)
        return len(decoded) % 16 == 0 and len(decoded) >= 16
    except:
        return False

def is_xor_encrypted(value):
    """判断是否为 XOR+Hex 加密"""
    if not value:
        return False
    return value.endswith(VALUE_SUFFIX) or value.endswith("CB5D79B")

def parse_ini_raw(content):
    """解析 INI 文件 (原始字节)"""
    sections = {}
    current_section = "DEFAULT"
    
    for line in content.split('\n'):
        line = line.strip()
        if not line:
            continue
        if line.startswith('[') and line.endswith(']'):
            current_section = line[1:-1]
            sections[current_section] = {}
        elif '=' in line:
            key, value = line.split('=', 1)
            if current_section not in sections:
                sections[current_section] = {}
            sections[current_section][key] = value
    
    return sections

def crack_config(config_path):
    """破解配置文件"""
    print(f"\n{'='*60}")
    print(f"ZCG 配置文件强制破解工具")
    print(f"{'='*60}")
    print(f"目标文件: {config_path}")
    print(f"AES密钥: {CONFIG_KEY_STR}")
    print(f"AES密钥(SHA256): {CONFIG_KEY.hex()}")
    print(f"XOR密钥: 0x{XOR_KEY:02X}")
    print(f"{'='*60}\n")
    
    # 读取配置文件
    try:
        with open(config_path, 'rb') as f:
            raw_content = f.read()
    except FileNotFoundError:
        print(f"文件不存在: {config_path}")
        return
    
    # 尝试不同编码
    content = None
    for enc in ['utf-8', 'gb2312', 'gbk', 'gb18030', 'latin1']:
        try:
            content = raw_content.decode(enc)
            print(f"使用编码: {enc}")
            break
        except:
            continue
    
    if not content:
        content = raw_content.decode('latin1')
        print("使用 latin1 编码 (fallback)")
    
    # 解析 INI
    sections = parse_ini_raw(content)
    
    # 破解结果
    results = {}
    
    for section_name, items in sections.items():
        print(f"\n[{section_name}]")
        results[section_name] = {}
        
        for key, value in items.items():
            # 显示原始值
            display_value = value if len(value) < 50 else value[:50] + "..."
            
            # 尝试解密
            decrypted = None
            decrypt_type = "明文"
            
            if key.lower() in ['qun', 'jwttoken', 'jwt_token', 'token']:
                # AES 加密
                if is_aes_encrypted(value):
                    decrypted = decrypt_aes_config(value)
                    decrypt_type = "AES"
            elif is_xor_encrypted(value):
                # XOR 加密
                decrypted = decrypt_xor_text(value)
                decrypt_type = "XOR"
            elif is_aes_encrypted(value):
                # 可能是 AES 加密的其他字段
                decrypted = decrypt_aes_config(value)
                decrypt_type = "AES"
            
            results[section_name][key] = {
                'original': value,
                'decrypted': decrypted,
                'type': decrypt_type
            }
            
            if decrypted and decrypted != value:
                print(f"  {key} = {display_value}")
                print(f"    └─ [{decrypt_type}解密] {decrypted}")
            else:
                print(f"  {key} = {display_value}")
    
    # 特别处理重要字段
    print(f"\n{'='*60}")
    print("重要配置提取:")
    print(f"{'='*60}")
    
    for section_name, items in results.items():
        # 查找账号相关
        if 'qun' in items and items['qun']['decrypted']:
            print(f"\n[{section_name}] 群号解密:")
            print(f"  原始: {items['qun']['original']}")
            print(f"  解密: {items['qun']['decrypted']}")
        
        if 'jwtToken' in items and items['jwtToken']['decrypted']:
            print(f"\n[{section_name}] JWT Token 解密:")
            print(f"  原始: {items['jwtToken']['original'][:50]}...")
            print(f"  解密: {items['jwtToken']['decrypted'][:100]}...")
    
    return results

def crack_all_configs():
    """破解所有找到的配置文件"""
    paths = [
        r"C:\zcg25.12.11\config.ini",
        r"C:\zcg25.12.11\config.ini.bak",
        r"C:\zcg25.12.11\Plugin.ini",
    ]
    
    for path in paths:
        if os.path.exists(path):
            crack_config(path)

if __name__ == "__main__":
    crack_all_configs()
    
    # 额外测试：手动解密已知的加密数据
    print(f"\n{'='*60}")
    print("手动测试解密:")
    print(f"{'='*60}")
    
    # 从备份文件中提取的加密数据
    test_data = {
        "qun": "iQEpb67flWe6pIT36InkpA==",
        "jwtToken": "PGFFZdzPvFnuZ+/YaTo86XkVr5X6ihkB6ItaKCQOUunAphxhDRcdh76lkn+hWkeE+M5D9L2CZdB5agYfNHp+jbfjhf6q6SCL568FTyl8buNhCxor6ScNbawWgzEroypKlwxYz2q4ypXDfNUFhrSp1TRMfRr9KGu27mqbQtezWbE=",
    }
    
    print("\n[1628907626] 账号配置:")
    print(f"  账号: 621705120")
    print(f"  qun (加密): {test_data['qun']}")
    print(f"  qun (解密): {decrypt_aes_config(test_data['qun'])}")
    print(f"\n  jwtToken (加密): {test_data['jwtToken'][:50]}...")
    print(f"  jwtToken (解密): {decrypt_aes_config(test_data['jwtToken'])}")
