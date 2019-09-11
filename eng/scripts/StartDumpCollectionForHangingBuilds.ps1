param(
  [Parameter(Mandatory = $true)]
  [ValidateNotNullOrEmpty()]
  [string]
  $ProcDumpPath,
  [Parameter(Mandatory = $true)]
  [ValidateNotNullOrEmpty()]
  [string]
  $ProcDumpDumpFolderPath,
  [Parameter(Mandatory = $true)]
  [datetime]
  $WakeTime,
  [Parameter(Mandatory = $true)]
  [ValidateNotNullOrEmpty()]
  [string []]
  $CandidateProcessNames
)

Write-Output "Setting up a scheduled job to capture process dumps.";

if ((-not (Test-Path $ProcDumpPath))) {
  Write-Warning "Can't find ProcDump at '$ProcDumpPath'.";
}
else {
  Write-Output "Using ProcDump from '$ProcDumpPath'.";
}

try {
  Get-Job -Name CaptureDumps* | Stop-Job -PassThru | Remove-Job;
  Get-ScheduledJob CaptureDumps* | Unregister-ScheduledJob;
}
catch {
  Write-Output "There was an error cleaning up previous jobs.";
  Write-Output $_.Exception.Message;
}

$repoRoot = Resolve-Path "$PSScriptRoot\..\..";
$ProcDumpDumpFolderPath = Join-Path $repoRoot $ProcDumpDumpFolderPath;

Write-Output "Dumps will be placed at '$ProcDumpDumpFolderPath'.";
Write-Output "Watching processes $($CandidateProcessNames -join ', ')";

# This script registers as a scheduled job. This scheduled job executes after two hours.
# When the scheduled job executes, it runs procdump on all alive processes whose name matches $CandidateProcessNames.
# The dumps are placed in $ProcDumpDumpFolderPath
# If the build completes sucessfully in less than two hours, a final step unregisters the job.

# Create a unique identifier for the job name
$JobName = "CaptureDumps" + (New-Guid).ToString("N");

# Ensure that all the folders we need exist, by default the layout will be something like
# <<root>>/dumps

if ((-not (Test-Path $ProcDumpDumpFolderPath))) {
  New-Item -ItemType Directory $ProcDumpDumpFolderPath;
}

# We write a sentinel file that we use at the end of the build to determine if there were build errors or the build hanged.
$sentinelFile = Join-Path $ProcDumpDumpFolderPath "dump-sentinel.txt";
Out-File -FilePath $sentinelFile -InputObject $JobName | Out-Null;

[scriptblock] $ScriptCode = {
  param(
    $ProcDumpPath,
    $ProcDumpDumpFolderPath,
    $CandidateProcessNames)

  Write-Output "Waking up to capture process dumps. Determining hanging processes.";

  [System.Diagnostics.Process []]$AliveProcesses = @();
  foreach ($candidate in $CandidateProcessNames) {
    try {
      $candidateProcesses = Get-Process $candidate;
      $candidateProcesses | ForEach-Object { Write-Output "Found candidate process $candidate with PID '$($_.Id)'." };
      $AliveProcesses += $candidateProcesses;
    }
    catch {
      Write-Output "No process found for $candidate";
    }
  }

  Write-Output "Starting process dump capture.";

  $dumpFullPath = [System.IO.Path]::Combine($ProcDumpDumpFolderPath, "HUNG_PROCESSNAME_PID_YYMMDD_HHMMSS.DMP");

  Write-Output "Capturing output for $($AliveProcesses.Length) processes.";

  foreach ($process in $AliveProcesses) {

    $procDumpArgs = @("-accepteula","-ma", $process.Id, $dumpFullPath);
    try {
      Write-Output "Capturing dump for dump for '$($process.Name)' with PID '$($process.Id)'.";
      Start-Process -FilePath $ProcDumpPath -ArgumentList $procDumpArgs -NoNewWindow -Wait;
    }
    catch {
      Write-Output "There was an error capturing a process dump for '$($process.Name)' with PID '$($process.Id)'."
      Write-Warning $_.Exception.Message;
    }
  }

  Write-Output "Done capturing process dumps.";
}

$ScriptTrigger = New-JobTrigger -Once -At $WakeTime;

try {
  Register-ScheduledJob -Name $JobName -ScriptBlock $ScriptCode -Trigger $ScriptTrigger -ArgumentList $ProcDumpPath, $ProcDumpDumpFolderPath, $CandidateProcessNames;
}
catch {
  Write-Warning "Failed to register scheduled job '$JobName'. Dumps will not be captured for build hangs.";
  Write-Warning $_.Exception.Message;
}
