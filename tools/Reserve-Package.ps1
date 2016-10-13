<#
.SYNOPSIS

Reserve a NuGet package name on nuget.org

.DESCRIPTION

Generate an empty NuGet package and temporarily publish to nuget.org in order to reserve a pacakge name.

.EXAMPLE

Reserve-Package -apiKey somekey -packageName Microsoft.AspNet.Mvc.NewFeature

.PARAMETER apiKey

A NuGet ApiKey is required to publish a NuGet package. If the package is authorized by ASP.NET team, please contact nugetaspnet@microsoft.com for the ApiKey.

.PARAMETER packageName

The name of pacakge to be reserved on nuget.org 

.PARAMETER publish

Switch decides if the publishing will actually happen. Default value is False.

#>
Param(
    [Parameter(Mandatory=$True)]
    [string] $apikey,

    [Parameter(Mandatory=$True)]
    [string] $packageName,

    [switch] $publish=$False
)

$template = @'
<?xml version="1.0"?>
<package >
  <metadata>
    <id>$id$</id>
    <version>0.0.1-alpha</version>
    <authors>aspnet</authors>
    <owners>Microsoft</owners>
    <licenseUrl>http://www.microsoft.com/web/webpi/eula/net_library_eula_enu.htm</licenseUrl>
    <projectUrl>http://www.asp.net/</projectUrl>
    <iconUrl>http://go.microsoft.com/fwlink/?LinkID=288859</iconUrl>
    <requireLicenseAcceptance>true</requireLicenseAcceptance>
    <description>$id$</description>
  </metadata>
</package>
'@

# download nuget.exe if it hasn't been downloaded yet.
if (-not (Test-Path tools\nuget.exe)) {
    mkdir tools | Out-Null
    curl https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -OutFile tools\nuget.exe
}

if (Test-Path nuspec) {
    rmdir -r nuspec | Out-Null
}

mkdir nuspec | Out-Null
mkdir nuspec\content | Out-Null

$template | Out-File "nuspec\$packageName.nuspec"
echo "placeholder" | Out-file "nuspec\content\readme.txt"

pushd nuspec
..\tools\nuget.exe pack -p Id=$packageName

if ($publish) {
    ..\tools\nuget.exe push "$packageName.0.0.1-alpha.nupkg" -Source https://www.nuget.org -ApiKey $apikey -Non
    ..\tools\nuget.exe delete $packageName 0.0.1-alpha -Source https://www.nuget.org -ApiKey $apikey -Non
}
popd