# 高级协议抓包工具
# 使用TCP代理方式捕获旺商聊与xclient之间的通信

param(
    [int]$ProxyPort = 21304,        # 代理监听端口
    [int]$TargetPort = 21303,       # xclient端口
    [int]$Duration = 60,            # 抓包时长(秒)
    [string]$OutputFile = "captured_packets.json"
)

Write-Host @"
╔════════════════════════════════════════════════════════════╗
║           旺商聊高级协议抓包工具 v1.0                      ║
╠════════════════════════════════════════════════════════════╣
║  代理端口: $ProxyPort                                        ║
║  目标端口: $TargetPort                                        ║
║  抓包时长: $Duration 秒                                        ║
╚════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

$capturedPackets = [System.Collections.ArrayList]::new()

# TCP代理函数
function Start-TcpProxy {
    param(
        [int]$ListenPort,
        [string]$TargetHost,
        [int]$TargetPort,
        [int]$TimeoutSec
    )
    
    $listener = $null
    try {
        $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $ListenPort)
        $listener.Start()
        Write-Host "[代理] 监听端口 $ListenPort 已启动" -ForegroundColor Green
        
        $endTime = (Get-Date).AddSeconds($TimeoutSec)
        $listener.Server.ReceiveTimeout = 1000
        
        while ((Get-Date) -lt $endTime) {
            if ($listener.Pending()) {
                $clientSocket = $listener.AcceptTcpClient()
                Write-Host "[代理] 接受新连接" -ForegroundColor Yellow
                
                # 连接到目标
                $targetSocket = New-Object System.Net.Sockets.TcpClient
                $targetSocket.Connect($TargetHost, $TargetPort)
                
                $clientStream = $clientSocket.GetStream()
                $targetStream = $targetSocket.GetStream()
                
                $buffer = New-Object byte[] 65536
                
                # 设置超时
                $clientStream.ReadTimeout = 500
                $targetStream.ReadTimeout = 500
                
                $packetCount = 0
                while ($clientSocket.Connected -and $targetSocket.Connected -and (Get-Date) -lt $endTime) {
                    try {
                        # 从客户端读取
                        if ($clientStream.DataAvailable) {
                            $bytesRead = $clientStream.Read($buffer, 0, $buffer.Length)
                            if ($bytesRead -gt 0) {
                                $timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss.fff")
                                $hex = [BitConverter]::ToString($buffer, 0, [Math]::Min($bytesRead, 256))
                                $utf8 = [System.Text.Encoding]::UTF8.GetString($buffer, 0, $bytesRead)
                                
                                $packet = @{
                                    Time = $timestamp
                                    Direction = "Client->Target"
                                    Bytes = $bytesRead
                                    Hex = $hex
                                    UTF8 = $utf8
                                }
                                
                                $null = $capturedPackets.Add($packet)
                                $packetCount++
                                
                                Write-Host "[$timestamp] CLIENT -> TARGET ($bytesRead bytes)" -ForegroundColor Magenta
                                Write-Host "  Hex: $($hex.Substring(0, [Math]::Min(100, $hex.Length)))..." -ForegroundColor DarkGray
                                
                                # 转发到目标
                                $targetStream.Write($buffer, 0, $bytesRead)
                            }
                        }
                        
                        # 从目标读取
                        if ($targetStream.DataAvailable) {
                            $bytesRead = $targetStream.Read($buffer, 0, $buffer.Length)
                            if ($bytesRead -gt 0) {
                                $timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss.fff")
                                $hex = [BitConverter]::ToString($buffer, 0, [Math]::Min($bytesRead, 256))
                                $utf8 = [System.Text.Encoding]::UTF8.GetString($buffer, 0, $bytesRead)
                                
                                $packet = @{
                                    Time = $timestamp
                                    Direction = "Target->Client"
                                    Bytes = $bytesRead
                                    Hex = $hex
                                    UTF8 = $utf8
                                }
                                
                                $null = $capturedPackets.Add($packet)
                                $packetCount++
                                
                                Write-Host "[$timestamp] TARGET -> CLIENT ($bytesRead bytes)" -ForegroundColor Cyan
                                Write-Host "  Hex: $($hex.Substring(0, [Math]::Min(100, $hex.Length)))..." -ForegroundColor DarkGray
                                
                                # 转发回客户端
                                $clientStream.Write($buffer, 0, $bytesRead)
                            }
                        }
                    } catch [System.IO.IOException] {
                        # 读取超时,继续
                    }
                    
                    Start-Sleep -Milliseconds 10
                }
                
                Write-Host "[代理] 连接关闭，捕获 $packetCount 个数据包" -ForegroundColor Yellow
                
                $clientSocket.Close()
                $targetSocket.Close()
            }
            
            Start-Sleep -Milliseconds 100
        }
    } catch {
        Write-Host "[错误] $($_.Exception.Message)" -ForegroundColor Red
    } finally {
        if ($listener) {
            $listener.Stop()
            Write-Host "[代理] 监听器已停止" -ForegroundColor Yellow
        }
    }
}

# 分析数据包格式
function Analyze-Packets {
    param($Packets)
    
    Write-Host "`n=== 数据包分析 ===" -ForegroundColor Green
    Write-Host "总数据包: $($Packets.Count)"
    
    $clientToTarget = $Packets | Where-Object { $_.Direction -eq "Client->Target" }
    $targetToClient = $Packets | Where-Object { $_.Direction -eq "Target->Client" }
    
    Write-Host "Client -> Target: $($clientToTarget.Count)"
    Write-Host "Target -> Client: $($targetToClient.Count)"
    
    # 分析前几个数据包的结构
    Write-Host "`n[前5个数据包详情]" -ForegroundColor Yellow
    $Packets | Select-Object -First 5 | ForEach-Object {
        Write-Host "---"
        Write-Host "时间: $($_.Time)"
        Write-Host "方向: $($_.Direction)"
        Write-Host "大小: $($_.Bytes) bytes"
        
        # 分析头部
        $hexBytes = $_.Hex -split '-'
        if ($hexBytes.Count -ge 4) {
            $header = $hexBytes[0..3] -join ' '
            Write-Host "头部: $header"
        }
        
        # 尝试识别格式
        if ($_.UTF8 -match '^\{') {
            Write-Host "格式: JSON"
        } elseif ($hexBytes[0] -eq "08" -or $hexBytes[0] -eq "0A") {
            Write-Host "格式: 可能是Protobuf (varint)"
        } else {
            Write-Host "格式: 自定义二进制"
        }
    }
}

# 直接抓取现有连接的数据
Write-Host "`n[方式1: 监控现有连接]" -ForegroundColor Yellow
Write-Host "检查端口53930 (旺商聊Electron) 与 21303 (xclient) 的通信..."

# 尝试直接连接并发送测试数据
Write-Host "`n[方式2: 直接连接测试]" -ForegroundColor Yellow

try {
    $client = New-Object System.Net.Sockets.TcpClient
    $client.ReceiveTimeout = 5000
    $client.Connect("127.0.0.1", $TargetPort)
    
    Write-Host "已连接到 $TargetPort" -ForegroundColor Green
    
    $stream = $client.GetStream()
    $buffer = New-Object byte[] 65536
    
    # 发送不同格式的测试数据
    $testPackets = @(
        # JSON格式
        [System.Text.Encoding]::UTF8.GetBytes('{"type":"ping"}'),
        # 带长度前缀的二进制格式
        @(0x00, 0x00, 0x00, 0x04, 0x70, 0x69, 0x6E, 0x67),  # 4字节长度 + "ping"
        # Protobuf风格
        @(0x0A, 0x04, 0x70, 0x69, 0x6E, 0x67),  # varint field + "ping"
        # 简单二进制
        @(0x01, 0x00, 0x00, 0x00)
    )
    
    foreach ($packet in $testPackets) {
        $hexStr = ($packet | ForEach-Object { '{0:X2}' -f $_ }) -join ' '
        Write-Host "`n发送: $hexStr" -ForegroundColor Magenta
        
        $stream.Write($packet, 0, $packet.Length)
        $stream.Flush()
        
        Start-Sleep -Milliseconds 1000
        
        if ($stream.DataAvailable) {
            $bytesRead = $stream.Read($buffer, 0, $buffer.Length)
            $responseHex = ($buffer[0..([Math]::Min($bytesRead-1, 63))] | ForEach-Object { '{0:X2}' -f $_ }) -join ' '
            $responseUtf8 = [System.Text.Encoding]::UTF8.GetString($buffer, 0, $bytesRead)
            
            Write-Host "响应 ($bytesRead bytes):" -ForegroundColor Green
            Write-Host "  Hex: $responseHex"
            Write-Host "  UTF8: $responseUtf8"
            
            $null = $capturedPackets.Add(@{
                Time = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss.fff")
                Direction = "Response"
                Bytes = $bytesRead
                Hex = $responseHex
                UTF8 = $responseUtf8
            })
        } else {
            Write-Host "无响应" -ForegroundColor Yellow
        }
    }
    
    $client.Close()
} catch {
    Write-Host "错误: $($_.Exception.Message)" -ForegroundColor Red
}

# 保存结果
if ($capturedPackets.Count -gt 0) {
    Write-Host "`n[保存捕获数据]" -ForegroundColor Yellow
    $capturedPackets | ConvertTo-Json -Depth 5 | Out-File $OutputFile -Encoding UTF8
    Write-Host "已保存到: $OutputFile"
    
    # 分析数据包
    Analyze-Packets $capturedPackets
} else {
    Write-Host "`n未捕获到数据包" -ForegroundColor Red
}

Write-Host "`n=== 抓包完成 ===" -ForegroundColor Green
