Param(
    [Parameter(Mandatory=$true)][string] $ManifestDirPath    # Manifest directory where sbom will be placed
)

Write-Host "Creating dir $ManifestDirPath"
# create directory for sbom manifest to be placed
if (!(Test-Path -path $ManifestDirPath))
{
  New-Item -ItemType Directory -path $ManifestDirPath
  Write-Host "Successfully created directory $ManifestDirPath"
}
else{
  Write-PipelineTelemetryError -category 'Build'  "Unable to create sbom folder."
}

Write-Host "Updating artifact name"
$artifact_name = "${env:SYSTEM_STAGENAME}_${env:AGENT_JOBNAME}_SBOM" -replace '["/:<>\\|?@*"() ]', '_'
Write-Host "Artifact name $artifact_name"
Write-Host "##vso[task.setvariable variable=ARTIFACT_NAME]$artifact_name"
