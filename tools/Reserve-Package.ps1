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

# download nuget.exe if it hasn't been downloaded yet.
if (-not (Test-Path nuget.exe)) {
    Invoke-WebRequest https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -OutFile nuget.exe
}

pushd nuspec
..\nuget.exe pack -p Id=$packageName

if ($publish) {
    ..\nuget.exe push "$packageName.0.0.1-alpha.nupkg" -Source https://www.nuget.org -ApiKey $apikey -Non
    ..\nuget.exe delete $packageName 0.0.1-alpha -Source https://www.nuget.org -ApiKey $apikey -Non
}
popd
