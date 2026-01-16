Add-Type -AssemblyName System.Net
$listener = New-Object System.Net.Sockets.TcpListener([System.Net.IPAddress]::Any, 5749)
try {
    $listener.Start()
    Write-Host "[代理服务] 端口5749监听成功，等待连接..." -ForegroundColor Green
    
    while ($true) {
        if ($listener.Pending()) {
            $client = $listener.AcceptTcpClient()
            $stream = $client.GetStream()
            $endpoint = $client.Client.RemoteEndPoint
            Write-Host "[代理服务] 客户端已连接: $endpoint" -ForegroundColor Cyan
            
            # 发送握手响应
            $response = '{"code":0,"msg":"ok","type":"handshake"}'
            $bytes = [System.Text.Encoding]::UTF8.GetBytes($response)
            $stream.Write($bytes, 0, $bytes.Length)
            Write-Host "[代理服务] 已发送握手响应" -ForegroundColor Gray
            
            # 读取数据
            $buffer = New-Object byte[] 4096
            while ($client.Connected) {
                if ($stream.DataAvailable) {
                    $count = $stream.Read($buffer, 0, $buffer.Length)
                    if ($count -gt 0) {
                        $data = [System.Text.Encoding]::UTF8.GetString($buffer, 0, $count)
                        Write-Host "[代理服务] 收到: $data" -ForegroundColor Yellow
                        
                        # 返回确认
                        $ack = '{"code":0,"msg":"received"}'
                        $ackBytes = [System.Text.Encoding]::UTF8.GetBytes($ack)
                        $stream.Write($ackBytes, 0, $ackBytes.Length)
                    }
                }
                Start-Sleep -Milliseconds 100
            }
        }
        Start-Sleep -Milliseconds 100
    }
} catch {
    Write-Host "[代理服务] 错误: $_" -ForegroundColor Red
} finally {
    $listener.Stop()
}
