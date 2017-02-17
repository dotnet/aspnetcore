param($references)
$ErrorActionPreference = "Stop";

function ToProjectName($file)
{
    return $file.Directory.Name;
}

$projectreferences = ls (Join-Path $references *.csproj) -rec;

$localprojects = ls -rec *.csproj;

foreach ($project in $localprojects)
{
    Write-Host "Processing $project";

    [Reflection.Assembly]::LoadWithPartialName("System.Xml.Linq") | Out-Null;

    $changed = $false
    $xDoc = [System.Xml.Linq.XDocument]::Load($project, [System.Xml.Linq.LoadOptions]::PreserveWhitespace);
    $endpoints = $xDoc.Descendants("PackageReference") | %{
        $packageName = $_.Attribute("Include").Value;
        $replacementProject = $projectreferences | ? { 
            return (ToProjectName($_)) -eq $packageName 
        };

        if ($replacementProject)
        {
            $changed = $true
            Write-Host "      Replacing $packageName with $($project.FullName)";
            $_.Name = "ProjectReference";
            $_.Attribute("Include").Value = $replacementProject.FullName;
        }
    };
    if ($changed)
    {
        $settings = New-Object System.Xml.XmlWriterSettings
        $settings.OmitXmlDeclaration = $true;
        $writer = [System.Xml.XmlWriter]::Create($project, $settings)

        $xDoc.Save($writer);
        $writer.Dispose();
    }

}