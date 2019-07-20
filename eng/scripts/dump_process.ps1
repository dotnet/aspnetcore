Set-Location $args[0]

$timestamp = $(get-date -f MM-dd-HH-mm)

while ($true) {
    Get-Process |Out-File -Width 300 artifacts/log/runningProcesses.$timestamp.txt
    Get-CimInstance Win32_Process |select name, processid, commandline |Out-File -Width 800 artifacts/log/runningProcessesCommandLine.$timestamp.txt
    Start-Sleep -Seconds 300
}
