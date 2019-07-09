Set-Location $args[0]

$timestamp = $(get-date -f MM-dd-HH-mm)

while ($true) {
    Get-Process > artifacts/log/runningProcesses.$timestamp.txt
    Get-WmiObject Win32_Process | select name, processid, commandline > artifacts/log/runningProcessesCommandLine.$timestamp.txt
    Start-Sleep -Seconds 300
}