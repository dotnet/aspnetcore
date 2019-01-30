param ($Configuration, $TargetDirectory)

$env:PATH = "$PSScriptRoot\..\..\..\..\.dotnet\x64\;$env:PATH"

dotnet --info

Write-Host $env:PATH

$testDir = "$PSScriptRoot\..\IIS\test";
$tempDir = "$PSScriptRoot\..\obj";

$projects = @(
    "$testDir\IIS.Tests\IIS.Tests.csproj",
    "$testDir\IISExpress.FunctionalTests\IISExpress.FunctionalTests.csproj",
    "$testDir\IIS.FunctionalTests\IIS.FunctionalTests.csproj",
    "$testDir\IIS.ForwardsCompatibility.FunctionalTests\IIS.ForwardsCompatibility.FunctionalTests.csproj",
    "$testDir\IIS.BackwardsCompatibility.FunctionalTests\IIS.BackwardsCompatibility.FunctionalTests.csproj"
);

if (Test-Path $tempDir)
{
    Remove-Item $tempDir -Recurse -Force
}

mkdir $tempDir;
mkdir $TargetDirectory -ErrorAction SilentlyContinue;

foreach ($project in $projects)
{
    $projectName = [io.path]::GetFileNameWithoutExtension($project)
    dotnet publish $project  -c $Configuration -o "$tempDir\$projectName"

    $targetArchive = "$TargetDirectory\$projectName.zip";

    Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue;
    Compress-Archive -Path "$tempDir\$projectName\*" -DestinationPath $TargetDirectory;
}
