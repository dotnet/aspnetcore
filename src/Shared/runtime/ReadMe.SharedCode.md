The code in this directory is shared between dotnet/runtime and dotnet/AspNetCore. This contains HTTP/2 and HTTP/3 protocol infrastructure such as an HPACK implementation. Any changes to this dir need to be checked into both repositories.

dotnet/runtime code paths:
- runtime\src\libraries\Common\src\System\Net\Http\aspnetcore
- runtime\src\libraries\Common\tests\Tests\System\Net\aspnetcore

dotnet/AspNetCore code paths:
- AspNetCore\src\Shared\runtime
- AspNetCore\src\Shared\test\Shared.Tests\runtime

## Copying code
- To copy code from dotnet/runtime to dotnet/AspNetCore, set ASPNETCORE_REPO to the AspNetCore repo root and then run CopyToAspNetCore.cmd.
- To copy code from dotnet/AspNetCore to dotnet/runtime, set RUNTIME_REPO to the runtime repo root and then run CopyToRuntime.cmd.

## Building dotnet/runtime code:
- https://github.com/dotnet/runtime/tree/master/docs/workflow
- Run *build.cmd* from the root once: `PS D:\github\runtime> .\build.cmd -runtimeConfiguration Release  -subsetCategory coreclr-libraries`
- Build the individual projects:
- `PS D:\github\dotnet\src\libraries\Common\tests> dotnet build`
- `PS D:\github\dotnet\src\libraries\System.Net.Http\src> dotnet build`

### Running dotnet/runtime tests:
- `PS D:\github\runtime\src\libraries\Common\tests> dotnet build /t:test`
- `PS D:\github\runtime\src\libraries\System.Net.Http\tests\UnitTests> dotnet build /t:test`

## Building dotnet/AspNetCore code:
- https://github.com/dotnet/AspNetCore/blob/master/docs/BuildFromSource.md
- Run restore in the root once: `PS D:\github\AspNetCore> .\restore.cmd`
- Activate to use the repo local runtime: `PS D:\github\AspNetCore> . .\activate.ps1`
- Build the individual projects:
- `(AspNetCore) PS D:\github\AspNetCore\src\Shared\test\Shared.Tests> dotnet build`
- `(AspNetCore) PS D:\github\AspNetCore\src\servers\Kestrel\core\src> dotnet build`

### Running dotnet/AspNetCore tests:
- `(AspNetCore) PS D:\github\AspNetCore\src\Shared\test\Shared.Tests> dotnet test`
- `(AspNetCore) PS D:\github\AspNetCore\src\servers\Kestrel\core\test> dotnet test`
