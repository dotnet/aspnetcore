Set-Location $args[0]

while ($true) {
    Get-Process > artifacts/log/runningProcesses.txt
    Start-Sleep -Seconds 300
}