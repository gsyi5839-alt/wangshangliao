# 旺商聊协议捕获分析脚本
# 需要管理员权限运行

param(
    [string]$TargetIP = "120.236.198.109",
    [int]$TargetPort = 47437,
    [int]$Duration = 30
)

Write-Host "开始捕获 $TargetIP:$TargetPort 的流量 ($Duration 秒)..."

# 使用netsh进行流量捕获 (需要管理员权限)
$traceFile = "$env:TEMP\wsl_capture.etl"

try {
    # 开始捕获
    netsh trace start capture=yes IPv4.Address=$TargetIP tracefile=$traceFile maxsize=50
    
    # 等待
    Start-Sleep -Seconds $Duration
    
    # 停止捕获
    netsh trace stop
    
    Write-Host "捕获完成: $traceFile"
    Write-Host "使用 Microsoft Message Analyzer 或 Wireshark 分析该文件"
} catch {
    Write-Host "错误: $($_.Exception.Message)"
    Write-Host "提示: 需要管理员权限运行"
}
