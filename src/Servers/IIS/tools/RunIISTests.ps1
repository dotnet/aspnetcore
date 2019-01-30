$archivesDirectory = ".\artifacts\testarchives"

foreach ($archive in Get-ChildItem "$archivesDirectory\*.zip")
{
    $projectName = [io.path]::GetFileNameWithoutExtension($archive)

    Expand-Archive $archive -DestinationPath $archivesDirectory -Force

    dotnet vstest "$archivesDirectory\$projectName\$projectName.dll"
}