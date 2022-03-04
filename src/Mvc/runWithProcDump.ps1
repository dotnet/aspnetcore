try
{
  $cwd = $PSScriptRoot;
  $job = Start-Job {
    $dumpsFolder = "${using:cwd}/artifacts/dumps";
    mkdir $dumpsFolder -Force;

    $procDumpFolder = "${using:cwd}/obj";
    mkdir $procDumpFolder -Force;

    $procDumpFolder = Resolve-Path $procDumpFolder;
    $ProgressPreference = 'SilentlyContinue' # Workaround PowerShell/PowerShell#2138
    Invoke-WebRequest https://download.sysinternals.com/files/Procdump.zip -OutFile "$procDumpFolder/procdump.zip";
    Expand-Archive "$procDumpFolder/procdump.zip" -DestinationPath "$procDumpFolder" -Force;

    $sleepTime = (1 * 20 * 60)
    Start-Sleep -Seconds $sleepTime;
    Write-Host "Producing dumps in $dumpsFolder";
    Write-Host "Process dumps to capture:"
    $processes = Get-Process dotnet*, testhost*;
    $processes | Format-Table;
    Write-Host "Using ProcDump from $procDumpFolder/procdump.exe";

    $processes |
     Select-Object -ExpandProperty ID |
     ForEach-Object { &"$procDumpFolder/procdump.exe" -accepteula -ma $_ $dumpsFolder }
  }
  Write-Host "Process dump capture job started. Running run.ps1 next";
  ./run.ps1 default-build @args
  # Receive-Job $job
  Stop-Job $job
  Remove-Job $job
}
catch
{
  write-host $_
  exit -1;
}
