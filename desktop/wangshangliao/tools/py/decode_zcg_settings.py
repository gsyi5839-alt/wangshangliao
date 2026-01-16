# -*- coding: utf-8 -*-
"""
解密 ZCG 设置文件
"""

def xor_decrypt(hex_str, key=0x10):
    """XOR 解密"""
    try:
        # 移除后缀 CB5D79B
        if 'CB5D79B' in hex_str:
            hex_str = hex_str.split('CB5D79B')[0]
        if '20' in hex_str and hex_str.endswith('20'):
            hex_str = hex_str[:-2]
            
        # 转换十六进制到字节
        data = bytes.fromhex(hex_str)
        
        # XOR 解密
        decrypted = bytes([b ^ key for b in data])
        
        # GB2312 解码
        return decrypted.decode('gb2312', errors='ignore')
    except Exception as e:
        return f"[解密失败: {e}]"

# 读取设置文件
settings = {}
with open(r"C:\temp\shezhi.txt", 'r', encoding='utf-8') as f:
    for line in f:
        line = line.strip()
        if '=' in line and not line.startswith('['):
            key, value = line.split('=', 1)
            settings[key] = value

print("=" * 60)
print("         ZCG 设置解密结果")
print("=" * 60)

# 关键设置项
key_items = [
    '编辑框_绑定群号',
    '编辑框_机器人QQ',
    '编辑框_管理QQ号码',
    '编辑框_自动回复_历史关键词',
    '编辑框_自动回复_财付通关键词',
    '编辑框_自动回复_支付宝关键词',
    '编辑框_自动回复_微信关键词',
    '编辑框_自动回复_个人数据关键词',
    '编辑框_自动回复_财付通发送',
    '编辑框_自动回复_支付宝发送',
    '编辑框_自动回复_微信发送',
    '编辑框_上分到词',
    '编辑框_上分没到词',
    '编辑框_下分查分词',
    '编辑框_攻击超范围提示内容',
    '编辑框_无账单下注提醒内容',
]

print("\n【基本设置】")
for key in ['编辑框_绑定群号', '编辑框_机器人QQ', '编辑框_管理QQ号码']:
    if key in settings:
        value = settings[key]
        decrypted = xor_decrypt(value)
        print(f"  {key.replace('编辑框_', '')}: {decrypted}")

print("\n【自动回复关键词】")
for key in settings:
    if '自动回复' in key and '关键词' in key:
        value = settings[key]
        decrypted = xor_decrypt(value)
        name = key.replace('编辑框_自动回复_', '').replace('关键词', '')
        print(f"  {name}: {decrypted}")

print("\n【自动回复内容】")
for key in settings:
    if '自动回复' in key and '发送' in key:
        value = settings[key]
        decrypted = xor_decrypt(value)
        name = key.replace('编辑框_自动回复_', '').replace('发送', '')
        print(f"  {name}: {decrypted}")

print("\n【上下分提示词】")
for key in ['编辑框_上分到词', '编辑框_上分没到词', '编辑框_上分到词_0分', '编辑框_下分查分词', '编辑框_下分拒绝词']:
    if key in settings:
        value = settings[key]
        decrypted = xor_decrypt(value)
        name = key.replace('编辑框_', '')
        print(f"  {name}:")
        print(f"    {decrypted[:100]}...")

print("\n【下注提醒】")
for key in ['编辑框_攻击超范围提示内容', '编辑框_无账单下注提醒内容', '编辑框_中文下注提醒内容']:
    if key in settings:
        value = settings[key]
        decrypted = xor_decrypt(value)
        name = key.replace('编辑框_', '')
        print(f"  {name}:")
        print(f"    {decrypted}")

print("\n" + "=" * 60)
