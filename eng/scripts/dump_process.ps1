Set-Location $args[0]

while ($true) {
    Get-Process > artifacts/log/processes/runningProcesses.$(get-date -f yyyy-MM-dd-HH-mm-ss).txt
    Start-Sleep -Seconds 300
}