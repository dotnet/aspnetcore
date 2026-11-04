param(
  [Parameter(Mandatory = $true)]
  [ValidateNotNullOrEmpty()]
  [string]
  $ProcDumpOutputPath
)

Write-Output "Finishing dump collection for hanging builds.";

$repoRoot = Resolve-Path "$PSScriptRoot\..\..";
$ProcDumpOutputPath = Join-Path $repoRoot $ProcDumpOutputPath;

$sentinelFile = Join-Path $ProcDumpOutputPath "dump-sentinel.txt";
if ((-not (Test-Path $sentinelFile))) {
  Write-Output "No sentinel file available in '$sentinelFile'. " +
    "StartDumpCollectionForHangingBuilds.ps1 has not been executed, is not correctly configured or failed before creating the sentinel file.";
  return;
}

Get-Process "procdump" -ErrorAction SilentlyContinue | ForEach-Object { Write-Output "ProcDump with PID $($_.Id) is still running."; };

$capturedDumps = Get-ChildItem $ProcDumpOutputPath -Filter *.dmp;
$capturedDumps | ForEach-Object { Write-Output "Found captured dump $_"; };

$JobName = (Get-Content $sentinelFile);

if ($JobName.Count -ne 1) {
  if ($JobName.Count -eq 0) {
    Write-Warning "No job name found. This is likely an error.";
    return;
  }
  else {
    Write-Output "Multiple job names found '$JobName'.";
    return;
  }
}

$dumpCollectionJob = Get-Job -Name $JobName -ErrorAction SilentlyContinue;
$registeredJob = Get-ScheduledJob -Name $JobName -ErrorAction SilentlyContinue;

if ($null -eq $dumpCollectionJob) {
  Write-Output "No job found for '$JobName'. It either didn't run or there is an issue with the job definition.";

  if ($null -eq $registeredJob) {
    Write-Warning "Couldn't find a scheduled job '$JobName'.";
  }
  return;
}

Write-Output "Listing existing jobs";
Get-Job -Name CaptureDumps*

Write-Output "Listing existing scheduled jobs";
Get-ScheduledJob -Name CaptureDumps*

Write-Output "Displaying job output";
Receive-Job $dumpCollectionJob;

Write-Output "Waiting for current job to finish";
Get-Job -ErrorAction SilentlyContinue | Wait-Job;

try {
    Write-Output "Removing collection job";
    Remove-Job $dumpCollectionJob;
}
catch {
    Write-Output "Failed to remove collection job";
}

try {
    Write-Output "Unregistering scheduled job";
    Unregister-ScheduledJob $registeredJob;
}
catch {
    Write-Output "Failed to unregister $JobName";
}
