The code in this directory is shared between CoreFx and AspNetCore. This contains HTTP/2 protocol infrastructure such as an HPACK implementation. Any changes to this dir need to be checked into both repositories.

Corefx code path: corefx\src\Common\src\System\Net\Http\Http2
AspNetCore code path: AspNetCore\src\Shared\Http2

## Copying code
To copy code from CoreFx to AspNetCore set ASPNETCORE_REPO to the AspNetCore repo root and then run CopyToAspNetCore.cmd.
To copy code from AspNetCore to CoreFx set COREFX_REPO to the CoreFx repo root and then run CopyToCoreFx.cmd.

## Building CoreFx code:
- https://github.com/dotnet/corefx/blob/master/Documentation/building/windows-instructions.md
- https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/developer-guide.md
- Run build.cmd from the root once: `PS D:\github\corefx> .\build.cmd`
- Build the individual projects:
- `PS D:\github\corefx\src\System.Net.Http\src> ..\..\..\eng\common\msbuild.ps1`
- `PS D:\github\corefx\src\Common\tests> ..\..\..\eng\common\msbuild.ps1 /t:rebuild`
- `PS D:\github\corefx\src\system.net.http\tests\FunctionalTests> ..\..\..\..\eng\common\build.ps1`

### Running CoreFx tests:
- `PS D:\github\corefx\src\System.Net.Http\tests\UnitTests> ..\..\..\..\eng\common\msbuild.ps1 /t:rebuildandtest`
- `PS D:\github\corefx\src\Common\tests> ..\..\..\eng\common\msbuild.ps1 /t:rebuildandtest`

## Building AspNetCore code:
- https://github.com/aspnet/AspNetCore/blob/master/docs/BuildFromSource.md

TODO:
- Build instructions for each repo
- Shared tests
- Code owners