Param
(
    [bool]$use_tls = $false
)

$allTests =
  "empty_unary",
  "large_unary",
  "client_streaming",
  "server_streaming",
  "ping_pong",
  "empty_stream",

  #"compute_engine_creds",
  #"jwt_token_creds",
  #"oauth2_auth_token",
  #"per_rpc_creds",

  "cancel_after_begin",
  "cancel_after_first_response",
  "timeout_on_sleeping_server",
  "custom_metadata",
  "status_code_and_message",
  "special_status_message",
  "unimplemented_service",
  "unimplemented_method",
  "client_compressed_unary",
  "client_compressed_streaming",
  "server_compressed_unary",
  "server_compressed_streaming"

Write-Host "Running $($allTests.Count) tests" -ForegroundColor Cyan
Write-Host "Use TLS: $use_tls" -ForegroundColor Cyan
Write-Host

foreach ($test in $allTests)
{
  Write-Host "Running $test" -ForegroundColor Cyan

  if (!$use_tls)
  {
    dotnet run --use_tls false --server_port 50052 --client_type httpclient --test_case $test
  }
  else
  {
    # Certificate is for test.google.com host. To run locally, setup the host file to point test.google.com to 127.0.0.1
    dotnet run --use_tls true --server_port 50052 --client_type httpclient --test_case $test --server_host test.google.com
  }

  Write-Host
}

Write-Host "Done" -ForegroundColor Cyan
