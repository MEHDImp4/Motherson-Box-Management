try {
    $tcp = New-Object System.Net.Sockets.TcpClient
    $tcp.Connect("localhost", 1433)
    Write-Host "Port 1433 OPEN"
    $tcp.Close()
} catch {
    Write-Host "Port 1433 CLOSED"
}
