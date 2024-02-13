$repoRoot = Resolve-Path -Path "$PSScriptRoot/../..";
Remove-Item -Path "package-lock.json" -Force -ErrorAction Stop
npm install
$diffResult = git diff-index HEAD -- ./package-lock.json
if ($null -eq $diffResult) {
  # There are no changes so we don't need to do anything
  Write-Host "No changes detected in package-lock.json"
} else {
  # There are changes so we need to perform the following actions:
  # Commit package-lock.json with the message "Update NPM dependencies".
  # Get the current commit hash of the repository. We'll call it $CurrentCommitHash
  # Navigate to src/submodules/Node-Externals (which is a submodule) that contains the npm-cache
  # Commit the changes to the submodule with the message. Update offline NPM Cache (Get-Date -Format "yyyy-MM-dd") $CurrentCommitHash
  # Go back to the root of the repository
  # Add the submodule changes to the commit
  # Commit the changes with the message "Update Node-Externals submodule"

  git add package-lock.json
  git commit -m "[Infrastructure] Update NPM dependencies"
  $CurrentCommitHash = (git rev-parse HEAD).Trim()
  Push-Location -Path (Join-Path $repoRoot -ChildPath "src/submodules/Node-Externals")
  git add .
  $commitMessage = @"
  [Infrastructure] Update offline NPM Cache $(Get-Date -Format "yyyy-MM-dd")

  https://github.com/dotnet/aspnetcore/commit/$CurrentCommitHash
"@;
  git commit -m $commitMessage
  Pop-Location
  git add .
  git commit -m "[Infrastructure] Update Node-Externals submodule"

  # Notify the github workflow by setting the result output variable
  Write-Output "{has-changes}=true" >> $GITHUB_OUTPUT
}

