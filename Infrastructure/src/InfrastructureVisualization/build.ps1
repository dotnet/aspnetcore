./Infrastructure/src/InfrastructureVisualization/dotnet-install.ps1 master
Write-Host "Done installing dotnet"
dotnet run --project ./Infrastructure/src/InfrastructureVisualization/GithubVisualizer.csproj

Write-Host "Done running"
