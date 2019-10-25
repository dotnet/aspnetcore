The code in this directory is shared between CoreFx and AspNetCore. This contains HTTP/2 protocol infrastructure such as an HPACK implementation. Any changes to this dir need to be checked into both repositories.

Corefx code paths:
- corefx\src\Common\src\System\Net\Http\Http2
- corefx\src\Common\tests\Tests\System\Net\Http2
AspNetCore code paths:
- AspNetCore\src\Shared\Http2
- AspNetCore\src\Shared\test\Shared.Tests\Http2

## Copying code
To copy code from CoreFx to AspNetCore set ASPNETCORE_REPO to the AspNetCore repo root and then run CopyToAspNetCore.cmd.
To copy code from AspNetCore to CoreFx set COREFX_REPO to the CoreFx repo root and then run CopyToCoreFx.cmd.

## Building CoreFx code:
- https://github.com/dotnet/corefx/blob/master/Documentation/building/windows-instructions.md
- https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/developer-guide.md
- Run build.cmd from the root once: `PS D:\github\corefx> .\build.cmd`
- Build the individual projects:
- `PS D:\github\corefx\src\Common\tests> dotnet msbuild /t:rebuild`
- `PS D:\github\corefx\src\System.Net.Http\src> dotnet msbuild /t:rebuild`

### Running CoreFx tests:
- `PS D:\github\corefx\src\Common\tests> dotnet msbuild /t:rebuildandtest`
- `PS D:\github\corefx\src\System.Net.Http\tests\UnitTests> dotnet msbuild /t:rebuildandtest`

## Building AspNetCore code:
- https://github.com/aspnet/AspNetCore/blob/master/docs/BuildFromSource.md
- Run restore in the root once: `PS D:\github\AspNetCore> .\restore.cmd`
- Activate to use the repo local runtime: `PS D:\github\AspNetCore> . .\activate.ps1`
- Build the individual projects:
- `(AspNetCore) PS D:\github\AspNetCore\src\Shared\test\Shared.Tests> dotnet msbuild`
- `(AspNetCore) PS D:\github\AspNetCore\src\servers\Kestrel\core\src> dotnet msbuild`

### Running AspNetCore tests:
- `(AspNetCore) PS D:\github\AspNetCore\src\Shared\test\Shared.Tests> dotnet test`
- `(AspNetCore) PS D:\github\AspNetCore\src\servers\Kestrel\core\test> dotnet test`