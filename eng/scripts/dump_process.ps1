Set-Location $args[0]
$local:timestamp = $(get-date -f MM-dd-HH-mm)

while ($true) {
    $local:file = "artifacts/log/runningProcesses.$timestamp.txt"
    if (Test-Path $file) {Move-Item -Force $file "$file.bak"}

    $local:temp = Get-Process |Out-String -Width 300
    $temp.Split([Environment]::NewLine).TrimEnd() |? Length -gt 0 |Out-File -Width 300 $file

    $file = "artifacts/log/runningProcessesCommandLine.$timestamp.txt"
    if (Test-Path $file) {Move-Item -Force $file "$file.bak"}

    $temp = Get-CimInstance Win32_Process |Format-Table -AutoSize Name, ProcessId, CommandLine |Out-String -Width 800
    $temp.Split([Environment]::NewLine).TrimEnd() |? Length -gt 0 |Out-File -Width 800 $file

    Start-Sleep -Seconds 120
}
