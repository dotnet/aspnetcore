param(
  [Parameter(Mandatory = $true)]
  [ValidateNotNullOrEmpty()]
  [string]
  $ProcDumpPath,
  [Parameter(Mandatory = $true)]
  [ValidateNotNullOrEmpty()]
  [string]
  $ProcDumpOutputPath,
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
  $previousJobs = Get-Job -Name CaptureDumps* -ErrorAction SilentlyContinue;
  $previousScheduledJobs = Get-ScheduledJob CaptureDumps* -ErrorAction SilentlyContinue;

  if ($previousJobs.Count -ne 0) {
    Write-Output "Found existing dump jobs.";
  }

  if ($previousScheduledJobs.Count -ne 0) {
    Write-Output "Found existing dump jobs.";
  }

  $previousJobs | Stop-Job -PassThru | Remove-Job;
  $previousScheduledJobs | Unregister-ScheduledJob;
}
catch {
  Write-Output "There was an error cleaning up previous jobs.";
  Write-Output $_.Exception.Message;
}

$repoRoot = Resolve-Path "$PSScriptRoot\..\..";
$ProcDumpOutputPath = Join-Path $repoRoot $ProcDumpOutputPath;

Write-Output "Dumps will be placed at '$ProcDumpOutputPath'.";
Write-Output "Watching processes $($CandidateProcessNames -join ', ')";

# This script registers as a scheduled job. This scheduled job executes after $WakeTime.
# When the scheduled job executes, it runs procdump on all alive processes whose name matches $CandidateProcessNames.
# The dumps are placed in $ProcDumpOutputPath
# If the build completes successfully in less than $WakeTime, a final step unregisters the job.

# Create a unique identifier for the job name
$JobName = "CaptureDumps" + (New-Guid).ToString("N");

# Ensure that the dumps output path exists.
if ((-not (Test-Path $ProcDumpOutputPath))) {
  New-Item -ItemType Directory $ProcDumpOutputPath | Out-Null;
}

# We write a sentinel file that we use at the end of the build to
# find the job we started and to determine the results from the sheduled
# job (Whether it ran or not and to display the outputs form the job)
$sentinelFile = Join-Path $ProcDumpOutputPath "dump-sentinel.txt";
Out-File -FilePath $sentinelFile -InputObject $JobName | Out-Null;

[scriptblock] $ScriptCode = {
  param(
    $ProcDumpPath,
    $ProcDumpOutputPath,
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

  $dumpFullPath = [System.IO.Path]::Combine($ProcDumpOutputPath, "hung_PROCESSNAME_PID_YYMMDD_HHMMSS.dmp");

  Write-Output "Capturing output for $($AliveProcesses.Length) processes.";

  foreach ($process in $AliveProcesses) {

    $procDumpArgs = @("-accepteula", "-ma", $process.Id, $dumpFullPath);
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
  Register-ScheduledJob -Name $JobName -ScriptBlock $ScriptCode -Trigger $ScriptTrigger -ArgumentList $ProcDumpPath, $ProcDumpOutputPath, $CandidateProcessNames;
}
catch {
  Write-Warning "Failed to register scheduled job '$JobName'. Dumps will not be captured for build hangs.";
  Write-Warning $_.Exception.Message;
}
