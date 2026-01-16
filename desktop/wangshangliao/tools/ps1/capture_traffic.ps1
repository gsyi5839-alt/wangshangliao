# 旺商聊通信抓包脚本
# 监控xclient.exe与旺商聊之间的通信

param(
    [int]$Duration = 60,  # 抓包持续时间(秒)
    [string]$OutputFile = "traffic_capture.json"
)

Write-Host "=== 旺商聊通信抓包工具 ===" -ForegroundColor Green
Write-Host "持续时间: $Duration 秒"
Write-Host "输出文件: $OutputFile"
Write-Host ""

# 检查端口状态
Write-Host "[检查通信端口]" -ForegroundColor Yellow
$ports = @(21303, 21308)
foreach ($port in $ports) {
    $conn = netstat -ano | Select-String ":$port"
    if ($conn) {
        Write-Host "  端口 $port 活跃:" -ForegroundColor Green
        $conn | ForEach-Object { Write-Host "    $_" }
    } else {
        Write-Host "  端口 $port 未活跃" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "[启动抓包]" -ForegroundColor Yellow

# 创建TCP监听代理
$messages = @()
$startTime = Get-Date

# 监控函数
function Monitor-Port {
    param([int]$Port)
    
    try {
        $tcpClient = New-Object System.Net.Sockets.TcpClient
        $tcpClient.Connect("127.0.0.1", $Port)
        
        if ($tcpClient.Connected) {
            Write-Host "  已连接到端口 $Port" -ForegroundColor Green
            $stream = $tcpClient.GetStream()
            $stream.ReadTimeout = 1000
            
            $buffer = New-Object byte[] 65536
            
            while ((Get-Date) -lt $startTime.AddSeconds($Duration)) {
                try {
                    $bytesRead = $stream.Read($buffer, 0, 65536)
                    if ($bytesRead -gt 0) {
                        $data = [System.Text.Encoding]::UTF8.GetString($buffer, 0, $bytesRead)
                        $timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss.fff")
                        
                        Write-Host "[$timestamp] 收到 $bytesRead 字节" -ForegroundColor Cyan
                        
                        # 尝试解析JSON
                        try {
                            $json = $data | ConvertFrom-Json
                            Write-Host "  类型: $($json.type)" -ForegroundColor Magenta
                            if ($json.text) {
                                Write-Host "  内容: $($json.text.Substring(0, [Math]::Min(50, $json.text.Length)))..." 
                            }
                        } catch {
                            Write-Host "  原始: $($data.Substring(0, [Math]::Min(100, $data.Length)))..."
                        }
                        
                        $script:messages += @{
                            Time = $timestamp
                            Port = $Port
                            Bytes = $bytesRead
                            Data = $data
                        }
                    }
                } catch [System.IO.IOException] {
                    # 读取超时,继续
                }
            }
            
            $tcpClient.Close()
        }
    } catch {
        Write-Host "  连接端口 $Port 失败: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "[监控端口21303的通信]" -ForegroundColor Yellow
Write-Host "注意: 此脚本只能被动监听，无法截获已建立的连接流量" -ForegroundColor DarkYellow
Write-Host "建议: 使用Wireshark或Fiddler进行更完整的抓包" -ForegroundColor DarkYellow
Write-Host ""

# 使用netstat持续监控
$endTime = $startTime.AddSeconds($Duration)
$lastConnections = @()

Write-Host "[实时连接监控]" -ForegroundColor Yellow
while ((Get-Date) -lt $endTime) {
    $currentConnections = netstat -ano | Select-String "21303|21308" | ForEach-Object { $_.ToString().Trim() }
    
    # 检测新连接
    foreach ($conn in $currentConnections) {
        if ($conn -notin $lastConnections) {
            $timestamp = (Get-Date).ToString("HH:mm:ss.fff")
            Write-Host "[$timestamp] 新连接: $conn" -ForegroundColor Green
            $script:messages += @{
                Time = $timestamp
                Type = "NewConnection"
                Data = $conn
            }
        }
    }
    
    # 检测断开的连接
    foreach ($conn in $lastConnections) {
        if ($conn -notin $currentConnections) {
            $timestamp = (Get-Date).ToString("HH:mm:ss.fff")
            Write-Host "[$timestamp] 断开: $conn" -ForegroundColor Red
            $script:messages += @{
                Time = $timestamp
                Type = "Disconnection"
                Data = $conn
            }
        }
    }
    
    $lastConnections = $currentConnections
    Start-Sleep -Milliseconds 500
}

# 保存结果
Write-Host ""
Write-Host "[保存结果]" -ForegroundColor Yellow
$script:messages | ConvertTo-Json -Depth 5 | Out-File $OutputFile -Encoding UTF8
Write-Host "已保存到: $OutputFile"
Write-Host "共记录 $($script:messages.Count) 条消息"

# 分析xclient进程
Write-Host ""
Write-Host "[xclient进程分析]" -ForegroundColor Yellow
$xclientProc = Get-Process -Name "xclient" -ErrorAction SilentlyContinue
if ($xclientProc) {
    Write-Host "  PID: $($xclientProc.Id)"
    Write-Host "  内存: $([math]::Round($xclientProc.WorkingSet64/1MB, 2)) MB"
    Write-Host "  句柄: $($xclientProc.HandleCount)"
    
    # 获取其网络连接
    Write-Host "  网络连接:"
    netstat -ano | Select-String $xclientProc.Id | ForEach-Object { Write-Host "    $_" }
}

Write-Host ""
Write-Host "=== 抓包完成 ===" -ForegroundColor Green
