The code in this directory is shared between dotnet/runtime and dotnet/aspnetcore. This contains HTTP/2 and HTTP/3 protocol infrastructure such as an HPACK implementation. Any changes to this dir need to be checked into both repositories.

dotnet/runtime code paths:
- runtime\src\libraries\Common\src\System\Net\Http\aspnetcore
- runtime\src\libraries\Common\tests\Tests\System\Net\aspnetcore

dotnet/aspnetcore code paths:
- aspnetcore\src\Shared\runtime
- aspnetcore\src\Shared\test\Shared.Tests\runtime

## Copying code
- To copy code from dotnet/runtime to dotnet/aspnetcore, set ASPNETCORE_REPO to the aspnetcore repo root and then run CopyToAspNetCore.cmd.
- To copy code from dotnet/aspnetcore to dotnet/runtime, set RUNTIME_REPO to the runtime repo root and then run CopyToRuntime.cmd.

## Building dotnet/runtime code:
- https://github.com/dotnet/runtime/tree/main/docs/workflow
- Run *build.cmd* from the root once: `PS D:\github\runtime> .\build.cmd -runtimeConfiguration Release  -subset clr+libs`
- Build the individual projects:
- `PS D:\github\dotnet\src\libraries\Common\tests> dotnet build`
- `PS D:\github\dotnet\src\libraries\System.Net.Http\src> dotnet build`

### Running dotnet/runtime tests:
- `PS D:\github\runtime\src\libraries\Common\tests> dotnet build /t:test`
- `PS D:\github\runtime\src\libraries\System.Net.Http\tests\UnitTests> dotnet build /t:test`

## Building dotnet/aspnetcore code:
- https://github.com/dotnet/aspnetcore/blob/main/docs/BuildFromSource.md
- Run restore in the root once: `PS D:\github\aspnetcore> .\restore.cmd`
- Activate to use the repo local runtime: `PS D:\github\aspnetcore> . .\activate.ps1`
- Build the individual projects:
- `(aspnetcore) PS D:\github\aspnetcore\src\Shared\test\Shared.Tests> dotnet build`
- `(aspnetcore) PS D:\github\aspnetcore\src\servers\Kestrel\core\src> dotnet build`

### Running dotnet/aspnetcore tests:
- `(aspnetcore) PS D:\github\aspnetcore\src\Shared\test\Shared.Tests> dotnet test`
- `(aspnetcore) PS D:\github\aspnetcore\src\servers\Kestrel\core\test> dotnet test`

## GitHub Actions

In dotnet/aspnetcore, the [runtime-sync](https://github.com/dotnet/aspnetcore/actions/workflows/runtime-sync.yml) GitHub action automatically creates PRs to pull in changes from dotnet/runtime.

In dotnet/runtime, the [aspnetcore-sync](https://github.com/dotnet/runtime/actions/workflows/aspnetcore-sync.yml) GitHub action must be run **manually** to create PRs to pull in changes from dotnet/aspnetcore.
This is expected to be less common.
