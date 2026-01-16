"""
旺商聊HTTPS流量抓包脚本
用于mitmproxy/mitmdump

使用方法:
1. 安装mitmproxy: pip install mitmproxy
2. 运行: mitmdump -s mitmproxy_capture.py -p 8080 --ssl-insecure
3. 设置系统代理为 127.0.0.1:8080
4. 打开旺商聊客户端
5. 观察输出的API调用

或者使用mitmweb可视化界面:
   mitmweb -s mitmproxy_capture.py -p 8080 --ssl-insecure
"""

import json
import os
from datetime import datetime
from mitmproxy import http, ctx

# 目标服务器
TARGET_SERVERS = [
    "120.233.185.185",  # API服务器
    "120.236.198.109",  # 长连接服务器
    "qixin",            # 旺商聊相关域名
    "wangshangliao",
]

# 日志文件
LOG_DIR = r"C:\wangshangliao\desktop\wangshangliao\captured_traffic"
os.makedirs(LOG_DIR, exist_ok=True)

captured_requests = []

def is_target_host(host: str) -> bool:
    """检查是否是目标服务器"""
    for target in TARGET_SERVERS:
        if target in host:
            return True
    return False

def request(flow: http.HTTPFlow) -> None:
    """处理请求"""
    host = flow.request.host
    
    if is_target_host(host):
        ctx.log.info(f"[REQUEST] {flow.request.method} {flow.request.pretty_url}")
        
        # 记录请求详情
        req_data = {
            "timestamp": datetime.now().isoformat(),
            "type": "request",
            "method": flow.request.method,
            "url": flow.request.pretty_url,
            "host": host,
            "port": flow.request.port,
            "path": flow.request.path,
            "headers": dict(flow.request.headers),
        }
        
        # 记录请求体
        if flow.request.content:
            try:
                req_data["body"] = flow.request.content.decode("utf-8")
                # 尝试解析JSON
                try:
                    req_data["body_json"] = json.loads(req_data["body"])
                except:
                    pass
            except:
                req_data["body_hex"] = flow.request.content.hex()
                req_data["body_length"] = len(flow.request.content)
        
        captured_requests.append(req_data)
        
        # 打印关键信息
        print(f"\n{'='*60}")
        print(f"[{datetime.now().strftime('%H:%M:%S')}] REQUEST")
        print(f"  URL: {flow.request.pretty_url}")
        print(f"  Method: {flow.request.method}")
        if "body_json" in req_data:
            print(f"  Body: {json.dumps(req_data['body_json'], ensure_ascii=False, indent=2)[:500]}")
        elif "body" in req_data:
            print(f"  Body: {req_data['body'][:500]}")

def response(flow: http.HTTPFlow) -> None:
    """处理响应"""
    host = flow.request.host
    
    if is_target_host(host):
        ctx.log.info(f"[RESPONSE] {flow.response.status_code} {flow.request.pretty_url}")
        
        # 记录响应详情
        resp_data = {
            "timestamp": datetime.now().isoformat(),
            "type": "response",
            "status_code": flow.response.status_code,
            "url": flow.request.pretty_url,
            "headers": dict(flow.response.headers),
        }
        
        # 记录响应体
        if flow.response.content:
            try:
                resp_data["body"] = flow.response.content.decode("utf-8")
                # 尝试解析JSON
                try:
                    resp_data["body_json"] = json.loads(resp_data["body"])
                except:
                    pass
            except:
                resp_data["body_hex"] = flow.response.content.hex()[:1000]
                resp_data["body_length"] = len(flow.response.content)
        
        captured_requests.append(resp_data)
        
        # 打印关键信息
        print(f"[{datetime.now().strftime('%H:%M:%S')}] RESPONSE")
        print(f"  Status: {flow.response.status_code}")
        if "body_json" in resp_data:
            print(f"  Body: {json.dumps(resp_data['body_json'], ensure_ascii=False, indent=2)[:500]}")
        elif "body" in resp_data:
            print(f"  Body: {resp_data['body'][:500]}")
        print(f"{'='*60}")

def done():
    """脚本结束时保存所有捕获的数据"""
    if captured_requests:
        filename = os.path.join(LOG_DIR, f"capture_{datetime.now().strftime('%Y%m%d_%H%M%S')}.json")
        with open(filename, "w", encoding="utf-8") as f:
            json.dump(captured_requests, f, ensure_ascii=False, indent=2)
        print(f"\n捕获数据已保存: {filename}")
        print(f"共捕获 {len(captured_requests)} 条记录")


# 额外: 如果需要修改请求/响应
def modify_request_example(flow: http.HTTPFlow) -> None:
    """示例: 修改请求 (默认不启用)"""
    pass

def modify_response_example(flow: http.HTTPFlow) -> None:
    """示例: 修改响应 (默认不启用)"""
    pass
