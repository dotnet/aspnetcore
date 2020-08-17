Param
(
    [bool]$use_tls = $false
)

$allTests =
  "client_compressed_unary"

Write-Host "Running $($allTests.Count) tests" -ForegroundColor Cyan
Write-Host "Use TLS: $use_tls" -ForegroundColor Cyan
Write-Host

foreach ($test in $allTests)
{
  Write-Host "Running $test" -ForegroundColor Cyan

  if (!$use_tls)
  {
    dotnet run --use_tls false --server_port 65337 --client_type httpclient --test_case $test
  }
  else
  {
    # Certificate is for test.google.com host. To run locally, setup the host file to point test.google.com to 127.0.0.1
    dotnet run --use_tls true --server_port 50052 --client_type httpclient --test_case $test --server_host test.google.com
  }

  Write-Host
}

Write-Host "Done" -ForegroundColor Cyan
