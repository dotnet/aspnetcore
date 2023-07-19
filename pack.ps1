# Globals... for now.
$Configuration = "Release"

function Build-Project
{
    param (
        [string]$ProjectName
    )

    $ProjectFiles = @(Get-ChildItem -Path . -Filter $ProjectName -Recurse -File)
    if ($ProjectFiles.Length -eq 0) {
        throw "Couldn't find project $ProjectName."
    }
    if ($ProjectFiles.Length -gt 1) {
        throw "Too many results for project $ProjectName."
    }

    $ProjectFullPath = $ProjectFiles[0].FullName

    WriteOutput "Building $ProjectFullPath"
    
    dotnet pack -c $Configuration $ProjectFullPath
    if (!$?) {
        throw "Failed to build project $ProjectFullPath."
    }
}

function Build-AspNetCore {
    Push-Location -Path src/Servers/Kestrel
    dotnet pack Kestrel.slnf
    Pop-Location
    
    # Main libraries
    Build-Project Microsoft.AspNetCore.Authentication.csproj
    Build-Project Microsoft.AspNetCore.Authentication.Abstractions.csproj
    Build-Project Microsoft.AspNetCore.Authentication.Cookies.csproj
    Build-Project Microsoft.AspNetCore.Authentication.Core.csproj
    Build-Project Microsoft.AspNetCore.Authorization.csproj
    Build-Project Microsoft.AspNetCore.CookiePolicy.csproj
    Build-Project Microsoft.AspNetCore.Connections.Abstractions.csproj
    Build-Project Microsoft.AspNetCore.Diagnostics.Abstractions.csproj
    Build-Project Microsoft.AspNetCore.Diagnostics.csproj
    Build-Project Microsoft.AspNetCore.HostFiltering.csproj
    Build-Project Microsoft.AspNetCore.Hosting.Abstractions.csproj
    Build-Project Microsoft.AspNetCore.Hosting.Server.Abstractions.csproj
    Build-Project Microsoft.AspNetCore.Hosting.csproj
    Build-Project Microsoft.AspNetCore.Http.Abstractions.csproj
    Build-Project Microsoft.AspNetCore.Http.Extensions.csproj
    Build-Project Microsoft.AspNetCore.Http.Features.csproj
    Build-Project Microsoft.AspNetCore.Http.Results.csproj
    Build-Project Microsoft.AspNetCore.Http.csproj
    Build-Project Microsoft.AspNetCore.HttpOverrides.csproj
    Build-Project Microsoft.AspNetCore.Localization.csproj
    Build-Project Microsoft.AspNetCore.Owin.csproj
    Build-Project Microsoft.AspNetCore.Metadata.csproj
    Build-Project Microsoft.AspNetCore.ResponseCompression.csproj
    Build-Project Microsoft.AspNetCore.Rewrite.csproj
    Build-Project Microsoft.AspNetCore.Routing.Abstractions.csproj
    Build-Project Microsoft.AspNetCore.Routing.csproj
    Build-Project Microsoft.AspNetCore.Server.Kestrel.Core.csproj
    Build-Project Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.csproj
    Build-Project Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.csproj
    Build-Project Microsoft.AspNetCore.Server.Kestrel.csproj
    Build-Project Microsoft.AspNetCore.StaticFiles.csproj
    Build-Project Microsoft.AspNetCore.WebUtilities.csproj
    Build-Project Microsoft.AspNetCore.csproj
    Build-Project Microsoft.Extensions.Features.csproj
    Build-Project Microsoft.Extensions.FileProviders.Embedded.csproj
    Build-Project Microsoft.Extensions.ObjectPool.csproj
    Build-Project Microsoft.Extensions.WebEncoders.csproj
    Build-Project Microsoft.Net.Http.Headers.csproj
    
    # signalR
    Build-Project Microsoft.AspNetCore.Authorization.Policy.csproj
    Build-Project Microsoft.AspNetCore.Http.Connections.csproj
    Build-Project Microsoft.AspNetCore.Http.Connections.Client.csproj
    Build-Project Microsoft.AspNetCore.Http.Connections.Common.csproj
    Build-Project Microsoft.AspNetCore.SignalR.Protocols.Json.csproj
    Build-Project Microsoft.AspNetCore.SignalR.Protocols.MessagePack.csproj
    Build-Project Microsoft.AspNetCore.SignalR.Protocols.NewtonsoftJson.csproj
    Build-Project Microsoft.AspNetCore.SignalR.Common.csproj
    Build-Project Microsoft.AspNetCore.SignalR.Core.csproj
    Build-Project Microsoft.AspNetCore.SignalR.csproj
    Build-Project Microsoft.AspNetCore.Websockets.csproj
    
    # mvc minimal api
    Build-Project Microsoft.AspNetCore.Mvc.ApiExplorer.csproj
    Build-Project Microsoft.AspNetCore.Mvc.Abstractions.csproj
    Build-Project Microsoft.AspNetCore.Mvc.Core.csproj
    Build-Project Microsoft.AspNetCore.ResponseCaching.Abstractions.csproj    
}

Build-AspNetCore
