Param(
  [string] $Repository,
  [string] $BranchName="master",
  [string] $GdnFolder,
  [string] $AzureDevOpsAccessToken,
  [string] $PushReason
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0
$global:LASTEXITCODE = 0

# We create the temp directory where we'll store the sdl-config repository
$sdlDir = Join-Path $env:TEMP "sdl"
if (Test-Path $sdlDir) {
  Remove-Item -Force -Recurse $sdlDir
}

Write-Host "git clone https://dnceng:`$AzureDevOpsAccessToken@dev.azure.com/dnceng/internal/_git/sdl-tool-cfg $sdlDir"
git clone https://dnceng:$AzureDevOpsAccessToken@dev.azure.com/dnceng/internal/_git/sdl-tool-cfg $sdlDir
if ($LASTEXITCODE -ne 0) {
  Write-Error "Git clone failed with exit code $LASTEXITCODE."
}
# We copy the .gdn folder from our local run into the git repository so it can be committed
$sdlRepositoryFolder = Join-Path (Join-Path (Join-Path $sdlDir $Repository) $BranchName) ".gdn"
if (Get-Command Robocopy) {
  Robocopy /S $GdnFolder $sdlRepositoryFolder
} else {
  rsync -r $GdnFolder $sdlRepositoryFolder
}
# cd to the sdl-config directory so we can run git there
Push-Location $sdlDir
# git add . --> git commit --> git push
Write-Host "git add ."
git add .
if ($LASTEXITCODE -ne 0) {
  Write-Error "Git add failed with exit code $LASTEXITCODE."
}
# check if there are any staged changes (0 = no changes, 1 = changes)
# if we don't do this and there's nothing to commit `git commit` will return
# exit code 1 and we will fail
Write-Host "git diff --cached --exit-code"
git diff --cached --exit-code
Write-Host "git diff exit code: $LASTEXITCODE"
if ($LASTEXITCODE -ne 0) {
  Write-Host "git -c user.email=`"dn-bot@microsoft.com`" -c user.name=`"Dotnet Bot`" commit -m `"$PushReason for $Repository/$BranchName`""
  git -c user.email="dn-bot@microsoft.com" -c user.name="Dotnet Bot" commit -m "$PushReason for $Repository/$BranchName"
  if ($LASTEXITCODE -ne 0) {
    Write-Error "Git commit failed with exit code $LASTEXITCODE."
  }
  Write-Host "git push"
  git push
  if ($LASTEXITCODE -ne 0) {
    Write-Error "Git push failed with exit code $LASTEXITCODE."
  }
}
else {
  Write-Host "Skipping commit and push because there is nothing to commit"
}
# Return to the original directory
Pop-Location