# Check the code is in sync
$changed = (select-string "nothing to commit" artifacts\status.txt).count -eq 0
if (-not $changed) { return $changed }
# Check if tracking issue is open/closed
$Headers = @{ Authorization = 'token {0}' -f $ENV:GITHUB_TOKEN; };
$result = Invoke-RestMethod -Uri $issue
if ($result.state -eq "closed") { 
 $json = "{ `"state`": `"open`" }"
 $result = Invoke-RestMethod -Method PATCH -Headers $Headers -Uri $issue -Body $json
}
# Add a comment
$status = [IO.File]::ReadAllText("artifacts\status.txt")
$diff = [IO.File]::ReadAllText("artifacts\diff.txt")
$body = @"
The shared code is out of sync.
<details>
  <summary>The Diff</summary>

``````
$status
$diff
``````

</details>
"@
$json = ConvertTo-Json -InputObject @{ 'body' = $body }
$issue = $issue + '/comments'
$result = Invoke-RestMethod -Method POST -Headers $Headers -Uri $issue -Body $json
return $changed