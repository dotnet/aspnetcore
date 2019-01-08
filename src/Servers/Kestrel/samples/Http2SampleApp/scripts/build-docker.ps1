dotnet publish --framework netcoreapp2.1 "$PSScriptRoot/../Http2SampleApp.csproj"

docker build -t kestrel-http2-sample (Convert-Path "$PSScriptRoot/..")
