# Define a parameter to accept the commit sha
param(
    [string]$CommitSha
)

$repoRoot = Resolve-Path -Path "$PSScriptRoot/../..";
$submodulePath = Join-Path -Path $repoRoot -ChildPath "src/submodules/Node-Externals"
# Fetch and checkout the commit sha in the submodule in src/submodules/Node-Externals
Set-Location $submodulePath
git fetch origin
git checkout $CommitSha
