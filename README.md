# AspNetCore-Internal

This repo contains internal-only tooling and infrastructure.

## Using the TeamCity MicroBuild plugin

Code-signing is available via MicroBuild plugin in TeamCity.

Usage:

* The build must include the step named "Install MicroBuild Signing Task"
    * Edit config -> Build Steps -> Add build step -> "Install MicroBuild Signing Task"
    * Make sure this step runs first in the build steps
* Projects which code sign must import [Microsoft.VisualStudioEng.MicroBuild.Core](https://www.nuget.org/packages/Microsoft.VisualStudioEng.MicroBuild.Core/). This adds special targets which find and load the signing plugin into the build process
* Set the MSBuild property `SignType` to 'real'. `/p:SignType=real`.

Machines requirements:
* Windows
* Install the SSL/PKITA certificates for the ESRP client (see the AspNetCoreCerts KeyVault and https://aka.ms/esrpclient for details)


## Prerequisites to build this repo

### Configure the internal package feeds

This build uses packages from two internal-only feeds: <https://dev.azure.com/microsoft/Universal%20Store/_packaging?feed=esrp&_a=feed> and <https://dev.azure.com/devdiv/DevDiv/_packaging?feed=WebTools&_a=feed>.

To consume the NuGet package:

1. Create two Personal Access Tokens (PAT) to access nuget source from https://dev.azure.com/devdiv/_usersSettings/tokens. (You would need one for microsoft account and one for devdiv account. Make sure they have the Packaging scope).
2. Add package source on your machine using the command:
    ```
    nuget.exe sources add -name esrp -source https://microsoft.pkgs.visualstudio.com/_packaging/ESRP/nuget/v3/index.json -username {anything} -password {your microsoft PAT}
    nuget.exe sources add -name webtools -source https://devdiv.pkgs.visualstudio.com/_packaging/WebTools/nuget/v3/index.json -username {anything} -password {your devdiv PAT}
    ```

More help on feed access is at
https://docs.microsoft.com/en-us/nuget/reference/extensibility/nuget-exe-credential-providers#using-a-credential-provider-from-an-environment-variable

### Configuring the ESRP package feed on CI

You can also configure the ESRP package feed access on CI by setting the following environment variable

```
$env:NuGetPackageSourceCredentials_esrp="Username=$alias$@microsoft.com;Password=$pat$"
$env:NuGetPackageSourceCredentials_webtools="Username=$alias$@microsoft.com;Password=$pat$"
```
where `$pat$` is a PAT from  https://dev.azure.com/devdiv/_usersSettings/tokens that has access to the "Packaging" scope.
