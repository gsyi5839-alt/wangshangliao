# TCP代理脚本 - 捕获xclient流量
param(
    [int]$LocalPort = 47437,
    [string]$RemoteHost = "120.236.198.109",
    [int]$RemotePort = 47437,
    [int]$DurationSeconds = 60
)

Write-Host "启动TCP代理: 本地:$LocalPort -> $RemoteHost`:$RemotePort"
Write-Host "运行时长: $DurationSeconds 秒"

$listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Any, $LocalPort)
$listener.Start()
Write-Host "代理已启动，监听端口 $LocalPort"

$endTime = (Get-Date).AddSeconds($DurationSeconds)
$capturedData = @()

while ((Get-Date) -lt $endTime) {
    if ($listener.Pending()) {
        $client = $listener.AcceptTcpClient()
        Write-Host "接受新连接" -ForegroundColor Green
        
        try {
            $remote = New-Object System.Net.Sockets.TcpClient
            $remote.Connect($RemoteHost, $RemotePort)
            
            $clientStream = $client.GetStream()
            $remoteStream = $remote.GetStream()
            
            $buffer = New-Object byte[] 65536
            
            while ($client.Connected -and $remote.Connected) {
                if ($clientStream.DataAvailable) {
                    $read = $clientStream.Read($buffer, 0, $buffer.Length)
                    if ($read -gt 0) {
                        $hex = [BitConverter]::ToString($buffer, 0, [Math]::Min($read, 64))
                        Write-Host "[C->S] $read bytes: $hex" -ForegroundColor Cyan
                        $remoteStream.Write($buffer, 0, $read)
                    }
                }
                
                if ($remoteStream.DataAvailable) {
                    $read = $remoteStream.Read($buffer, 0, $buffer.Length)
                    if ($read -gt 0) {
                        $hex = [BitConverter]::ToString($buffer, 0, [Math]::Min($read, 64))
                        Write-Host "[S->C] $read bytes: $hex" -ForegroundColor Yellow
                        $clientStream.Write($buffer, 0, $read)
                    }
                }
                
                Start-Sleep -Milliseconds 10
            }
        } catch {
            Write-Host "错误: $($_.Exception.Message)" -ForegroundColor Red
        } finally {
            $client.Close()
            $remote.Close()
        }
    }
    Start-Sleep -Milliseconds 100
}

$listener.Stop()
Write-Host "代理已停止"
