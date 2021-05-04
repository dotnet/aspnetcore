To test locally
dotnet publish /p:IsHelixJob=true
$env:Helix = "true"
dotnet test .\Microsoft.AspNetCore.Components.Migration.E2ETests.dll from publish directory