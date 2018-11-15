param(
    [Parameter(Mandatory = $true)]
    $ProdConBuildId,
    [Parameter(Mandatory = $true)]
    $ProductVersion,
    $FeedContainer = 'orchestrated-release-2-2'
)

$patchUrl = "https://dotnetfeed.blob.core.windows.net/${FeedContainer}/${ProdconBuildId}/final/assets/aspnetcore/Runtime/${ProductVersion}/nuGetPackagesArchive-ci-server-${ProductVersion}.patch.zip"

dotnet run $patchUrl "../Archive.CiServer.Patch/ArchiveBaseline.${ProductVersion}.txt"

$compatPatchUrl = "https://dotnetfeed.blob.core.windows.net/${FeedContainer}/${ProdconBuildId}/final/assets/aspnetcore/Runtime/${ProductVersion}/nuGetPackagesArchive-ci-server-compat-${ProductVersion}.patch.zip"

dotnet run $compatPatchUrl "../Archive.CiServer.Patch.Compat/ArchiveBaseline.${ProductVersion}.txt"
