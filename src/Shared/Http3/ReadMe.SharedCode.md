The code in this directory is shared between dotnet/runtime and aspnet/AspNetCore. This contains HTTP/3 protocol infrastructure such as a QPACK implementation. Any changes to this dir need to be checked into both repositories.

dotnet/runtime code paths:
- runtime\src\libraries\Common\src\System\Net\Http\Http3
- runtime\src\libraries\Common\tests\Tests\System\Net\Http3
aspnet/AspNetCore code paths:
- AspNetCore\src\Shared\Http3
- AspNetCore\src\Shared\test\Shared.Tests\Http3

## Copying code
To copy code from dotnet/runtime to aspnet/AspNetCore, set ASPNETCORE_REPO to the AspNetCore repo root and then run CopyToAspNetCore.cmd.
To copy code from aspnet/AspNetCore to dotnet/runtime, set RUNTIME_REPO to the runtime repo root and then run CopyToRuntime.cmd.

## Building dotnet/runtime code:
- https://github.com/dotnet/runtime/blob/master/docs/libraries/building/windows-instructions.md
- https://github.com/dotnet/runtime/blob/master/docs/libraries/project-docs/developer-guide.md
- Run libraries.cmd from the root once: `PS D:\github\runtime> .\libraries.cmd`
- Build the individual projects:
- `PS D:\github\dotnet\src\libraries\Common\tests> dotnet msbuild /t:rebuild`
- `PS D:\github\dotnet\src\libraries\System.Net.Http\src> dotnet msbuild /t:rebuild`

### Running dotnet/runtime tests:
- `PS D:\github\runtime\src\libraries\Common\tests> dotnet msbuild /t:rebuildandtest`
- `PS D:\github\runtime\src\libraries\System.Net.Http\tests\UnitTests> dotnet msbuild /t:rebuildandtest`

## Building aspnet/AspNetCore code:
- https://github.com/aspnet/AspNetCore/blob/master/docs/BuildFromSource.md
- Run restore in the root once: `PS D:\github\AspNetCore> .\restore.cmd`
- Activate to use the repo local runtime: `PS D:\github\AspNetCore> . .\activate.ps1`
- Build the individual projects:
- `(AspNetCore) PS D:\github\AspNetCore\src\Shared\test\Shared.Tests> dotnet msbuild`
- `(AspNetCore) PS D:\github\AspNetCore\src\servers\Kestrel\core\src> dotnet msbuild`

### Running aspnet/AspNetCore tests:
- `(AspNetCore) PS D:\github\AspNetCore\src\Shared\test\Shared.Tests> dotnet test`
- `(AspNetCore) PS D:\github\AspNetCore\src\servers\Kestrel\core\test> dotnet test`
