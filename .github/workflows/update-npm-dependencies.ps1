$repoRoot = Resolve-Path -Path "$PSScriptRoot/../..";
Set-Location $repoRoot;
# Update the .npmrc file to point to the cache location as a repo instead of a submodule
$npmrcPath = Join-Path -Path $repoRoot -ChildPath ".npmrc"
$content = Get-Content -Path $npmrcPath;
$content = $content -replace "cache=./src/submodules/Node-Externals/cache", "cache=../Node-Externals/cache";
$content | Set-Content -Path $npmrcPath;

Remove-Item -Path "package-lock.json" -Force -ErrorAction Stop
npm install
$diffResult = git diff-index HEAD -- ./package-lock.json
if ($null -eq $diffResult) {
  # There are no changes so we don't need to do anything
  Write-Host "No changes detected in package-lock.json"
}

# Undo the changes to the .npmrc file
git checkout -- ./.npmrc
