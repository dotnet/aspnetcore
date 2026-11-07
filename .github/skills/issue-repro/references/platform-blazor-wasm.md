# Platform: Blazor WebAssembly Repro

Use for Blazor WASM bugs that require browser execution.

## Prerequisites

```bash
# Check WASM tooling
dotnet workload list | grep wasm

# Install if missing
dotnet workload install wasm-tools
```

## Create → Build → Serve → Verify

### Create

```bash
mkdir -p /tmp/aspnetcore/repro/{number}-blazor && cd /tmp/aspnetcore/repro/{number}-blazor
dotnet new blazorwasm -n Repro{number}Blazor
cd Repro{number}Blazor
```

### Customize the component

Edit `Pages/Home.razor` or create a new page to reproduce the issue:

```razor
@page "/repro"
@using Microsoft.JSInterop
@inject IJSRuntime JS

<h1>Repro</h1>
<p>Result: @_result</p>
<button @onclick="Test">Run Test</button>

@code {
    private string _result = "Not run";

    private async Task Test()
    {
        // Reproduce the bug here
        _result = "Test ran";
        StateHasChanged();
    }
}
```

### Build and Serve

```bash
dotnet build
dotnet run &
APP_PID=$!
sleep 5  # WASM apps take longer to start
```

### Verify in Browser (via Playwright MCP if available)

If Playwright MCP tools are available, navigate to the app and capture the result:

```
browser_navigate: http://localhost:5000/repro
browser_click: button[text="Run Test"]
browser_screenshot: capture state
```

If Playwright is not available, document the browser steps in `notes` and mark as `needs-platform` only if the bug cannot be verified via server console output.

### Verify via Server Logs (fallback)

For many WASM bugs, server-side errors surface in the browser console. Check if the issue manifests in startup or build errors first:

```bash
dotnet build 2>&1 | grep -i "error\|warning"
```

### Cleanup

```bash
kill $APP_PID 2>/dev/null
rm -rf /tmp/aspnetcore/repro/{number}-blazor
```

## Version Testing

```bash
# Reporter's version
mkdir /tmp/aspnetcore/repro/{number}-blazor-v8
dotnet new blazorwasm -n Repro{number}v8 --framework net8.0 -o /tmp/aspnetcore/repro/{number}-blazor-v8/

# Latest
mkdir /tmp/aspnetcore/repro/{number}-blazor-v9
dotnet new blazorwasm -n Repro{number}v9 --framework net9.0 -o /tmp/aspnetcore/repro/{number}-blazor-v9/
```

## Blazor Server as Fallback

If WASM cannot be tested (missing browser), try Blazor Server for component model bugs (many issues affect both):

```bash
dotnet new blazorserver -n Repro{number}Server
```

Note in `notes` and `reproProject.type` if using Server as WASM fallback.

## Notes on WASM Reproduction

- WASM apps run **in the browser** — server-side code analysis alone is insufficient
- JS console errors are critical evidence for WASM bugs
- WASM-specific bugs (AOT, linking, JS interop) may require actual browser execution
- If browser access is unavailable, use `conclusion: "needs-platform"` with `blockers: ["Browser environment required for WASM reproduction"]`
