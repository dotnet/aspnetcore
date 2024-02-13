# Define a parameter to accept the commit sha
param(
    [string]$CommitSha
)

# Fetch and checkout the commit sha in the submodule in src/submodules/Node-Externals
Set-Location src/submodules/Node-Externals
git fetch origin
git checkout $CommitSha
