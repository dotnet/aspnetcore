# Working on Issues in the Components Area

This guide provides step-by-step instructions for working on issues in the ASP.NET Core Components area.

## Working on issues

You MUST follow this workflow when implementing new features or fixing bugs in the Components area.
* Add the workflow to your `todos` and follow it strictly.
- Create a sample scenario.
- If working on a bug, use playwright to reproduce the behavior/problem first.
- You MUST have reproduced the problem before attempting to fix it.
- Research the problem area using the microsoft docs, existing code, git history, and logging on the sample project.
- Implement the fix or feature in the sample project first.
- Test the fix or feature interactively using Playwright.
- Once the fix or feature is validated in the sample, implement E2E tests for it.
  - When you create an E2E test. First execute it interactively with Playwright.
  - If an E2E test is failing, debug it by running the test server manually and navigating to the scenario in a browser.
- Only after the E2E tests are passing, remove the sample code you added in the Samples projects.
  - Use `git checkout` and `git clean -fd` to remove the sample code.

### Code clarity and durable knowledge

- Before adding a comment, make local behavior discoverable through precise names,
  named methods or variables, and smaller single-purpose responsibilities. A named
  method can improve clarity even when it does not reduce duplication.
- Add a concise implementation comment only when a durable nonlocal reason cannot
  be expressed by structure alone, such as ordering across JavaScript and .NET
  callbacks, lifecycle ownership transfer, compatibility constraints, or a
  required negative guarantee. Do not narrate the call graph or restate the code.
- Do not use public XML documentation to explain internal implementation details,
  including control flow or lifecycle state. Limit it to consumer-observable behavior.

### Overview

The workflow for implementing new features in the Components area follows these steps:

1. **Create a sample scenario first** - This is the most important first step. Update code in one of the projects in the `src/Components/Samples` folder to include the scenarios for the feature you want to build. This allows you to develop and test the feature interactively before writing formal tests.

2. **Build and test interactively** - Build the feature and use Playwright to test it in the browser, ensuring it works end-to-end at a basic level.

### Sample Projects

The `src/Components/Samples` folder contains canonical Blazor Web App samples, and `src/Components/WebAssembly/Samples` contains a standalone WebAssembly sample, that you can use for developing and testing features. All are generated from the `dotnet new blazor`/`blazorwasm` templates with `Auto` interactivity and adapted to reference the in-tree framework:

- **BlazorWebAppGlobal** (+ **.Client**) - A Blazor Web App with **global** interactivity (`@rendermode="InteractiveAuto"` on `Routes`/`HeadOutlet` in `App.razor`). Change that one value to `InteractiveServer`/`InteractiveWebAssembly` to test the whole app on a single platform.
- **BlazorWebAppPerPage** (+ **.Client**) - A Blazor Web App with **per-page** interactivity. Apply `@rendermode` per page/component (`InteractiveServer`/`InteractiveWebAssembly`/`InteractiveAuto`), mix modes, or omit it for static SSR.
- **BlazorWebAssemblyStandalone** - A standalone Blazor WebAssembly app (no server host), under `src/Components/WebAssembly/Samples`.

Together these cover every interactivity platform (Server/WebAssembly/Auto/None) and location (Global/Per-page) by editing a single `@rendermode` rather than restructuring.

**Always start by adding your feature scenario to whichever sample matches the render mode you need.** This allows you to:
- Quickly iterate on the implementation
- Test the feature interactively in a real browser
- Verify the feature works before writing formal E2E tests
- Debug issues more easily with full logging capabilities

3. **Debug when needed**:
   - If something isn't working as expected, increase the logging level in the sample for `Microsoft.AspNetCore.Components` to `Debug` to see detailed logs.
   - Check browser console logs using Playwright's `browser_console_messages`.
   - Use Microsoft documentation to learn more about troubleshooting Blazor applications.
   - You can also increase the log level for JavaScript console output.

4. **Validate the sample works** - You must have a validated, working sample in the Samples folder before proceeding. Use Playwright to confirm the feature works end-to-end in the browser.

5. **Implement E2E tests** - Only after the sample is validated, implement E2E tests for it.

6. **Clean up sample code** - After your E2E tests are passing, remove the sample code you added to the Samples projects. The sample was only for development and interactive testing; the E2E tests now provide the permanent test coverage. Use `git checkout -- src/Components/Samples` and `git clean -df -- src/Components/Samples` to remove the sample code.

## Design invariants

These are long-standing design rules for framework code in this area. Each one lists its real
exceptions - respect those, because the exceptions are legitimate existing patterns, not
violations to clean up.

### Render modes

- **Framework components must be render-mode agnostic** so they work when consumed from a
  Razor Class Library. Framework components never use `@rendermode` to force a mode - it is a
  choice for the app that consumes the component, not for the component itself.
- **Exception:** a deliberately small set of render-mode-specific components ships in
  `Microsoft.AspNetCore.Components.Endpoints` for static SSR - `CacheView`, `BasePath`,
  `ImportMap`, and `ResourcePreloader`. Keep that set small; do not add to it without a strong
  reason.
- Public APIs should work in every render mode. The exception is APIs defined in a concrete
  render-mode assembly (`Endpoints`, `Server`, `WebAssembly`, `WebView`), which are not
  intended for class-library consumption. Anything meant to be consumed broadly belongs in an
  agnostic assembly such as `Components`, `Web`, or `Forms`.

### Accessing `HttpContext`

- **Do not use `IHttpContextAccessor`** in framework components or framework services. It does
  not exist in any render mode other than static SSR.
- Components receive it as a cascading parameter instead - note the consuming attribute is
  `[CascadingParameter]`:

  ```csharp
  [CascadingParameter] public HttpContext? HttpContext { get; set; }
  ```

  See `Endpoints/src/Assets/ImportMap.cs` and `Endpoints/src/CacheView/CacheView.cs`.
- Services take `HttpContext` as a **method parameter**, never as a constructor dependency.
  `EndpointAntiforgeryStateProvider` is the reference: it takes `IAntiforgery` in its
  constructor and receives the context through `SetRequestContext(HttpContext)`, then falls
  back to persisted state when there is no context.

### Registering services across render modes

New functionality must register the services it needs for **every** render mode it supports.
A feature consumed by class libraries needs a render-mode-agnostic abstraction that each
hosting environment implements. Antiforgery is the reference pattern:

- `AntiforgeryStateProvider` - abstract, in `Web/src/Forms/`
- `DefaultAntiforgeryStateProvider` - shared default, in `Shared/src/`
- `EndpointAntiforgeryStateProvider` - SSR implementation, in `Endpoints/src/Forms/`
- registered once per host: `Endpoints` (`RazorComponentsServiceCollectionExtensions`),
  `Server` (`ComponentServiceCollectionExtensions`), and `WebAssembly`
  (`WebAssemblyHostBuilder`), with `AddPersistentServiceRegistration` covering `InteractiveAuto`

Form binding follows the same shape: the `IFormValueMapper` abstraction lives in
`Web/src/Forms/Mapping/`, and `Endpoints` supplies `HttpContextFormValueMapper`. Copy this
pattern rather than inventing a new one.

### Rendering and the synchronization context

- **Do not call `StateHasChanged()` where `ComponentBase` already re-renders for you.** Inside
  `OnInitialized[Async]`, `OnParametersSet[Async]`, and event-callback handlers, a call before
  the first `await` or after the last `await` is redundant, because `ComponentBase` renders
  after those anyway. This is enforced by analyzer **`BL0012`**
  (`Analyzers/src/StateHasChangedAnalyzer.cs`).
- **That scope is narrow, and calls outside it are usually required.** `BL0012` deliberately
  targets only those methods, so before flagging a call, check whether it is one of these
  legitimate cases:
  - `OnAfterRender[Async]`, which does **not** trigger an automatic re-render - see
    `ComponentBase.IHandleAfterRender.OnAfterRenderAsync`, and `Web/src/Head/HeadOutlet.cs`
    for a component that relies on this
  - ordinary helper methods, such as those in `Web/src/Virtualization/Virtualize.cs`
  - handlers for external events, such as `Web/src/Routing/NavLink.cs` reacting to
    `LocationChanged`
  - lambdas and local functions, which the analyzer explicitly skips because they have their
    own execution timing - see `Web/src/Forms/ValidationMessage.cs`
- **Do not call `InvokeAsync` to re-enter a synchronization context you are already on.**
  Framework code enters the renderer's context at well-defined dispatch points and should not
  leave it on its own.
- **Exception:** marshalling an **external** callback back onto the renderer's context is
  exactly what `InvokeAsync` is for. See `Authorization/src/CascadingAuthenticationState.razor`,
  where `AuthenticationStateChanged` arrives off-context, and the QuickGrid components.
- `Dispatcher.InvokeAsync` in hosting infrastructure (`CircuitHost`, `EndpointHtmlRenderer`,
  `WebViewManager`, `RemoteRenderer`) *is* the well-defined dispatch point, not a violation.
  `EventCallback.InvokeAsync` is an unrelated API for invoking a callback - it is not affected
  by this rule.

### Components and events

- **Do not implement `IHandleEvent` in new components.** `ComponentBase` already implements it
  and provides the automatic re-render after event handlers; implementing it yourself opts out
  of that behavior. Tests that implement it directly are exercising that opt-out deliberately.

### Trimming and AOT

Trim and AOT safety is handled with **annotations**, not by banning reflection:
`[DynamicallyAccessedMembers]`, `[RequiresUnreferencedCode]`, and `[RequiresDynamicCode]`.
Annotate new reflection-based code rather than avoiding reflection outright. Do not propose a
blanket no-reflection or source-generated-serialization-only rule - JS interop itself depends
on annotated reflection. `JsonSerializerContext` is still the right tool where it fits, such as
`Endpoints/src/Assets/ImportMapSerializerContext.cs`.

### Dependencies

**No new dependencies as a default.** Almost every shipping project in this area has zero
`<PackageReference>` entries and uses `<Reference Include="..." />` for framework references
instead. The few exceptions are inherent to what the project is - the analyzer needs Roslyn,
and `Gateway` needs OpenTelemetry. Adding a package reference to a shipping Components project
needs an explicit justification.

### Coordinating async work in tests

Prefer `TaskCompletionSource` to coordinate async work; it is the dominant pattern in this
area. Avoid `Task.Delay` and `Thread.Sleep` as a way to wait for something to happen, since
that is what makes tests flaky.

**Legitimate exceptions - do not "fix" these:**

- E2E/Selenium polling and retry loops, which must wait on a real browser
- tests that assert on measured duration, such as the metrics tests
- analyzer test fixtures, where `await Task.Delay(1)` is the *source text being analyzed*
  rather than test coordination
- deliberately simulating slow async work

## Build Tips

### Efficient Build Strategy

To avoid unnecessary full repository builds, follow this optimized approach:

#### 1. Initial Setup - Check for First Build
Before running any commands, check if a full build has already been completed:
- Look for `artifacts\agent-sentinel.txt` in the repository root
- If this file exists, skip to step 2
- If not present, run the initial build and create the sentinel file:

```bash
.\eng\build.cmd
echo "We ran eng\build.cmd successfully" > artifacts\agent-sentinel.txt
```

#### 2. Check for JavaScript Assets
Before running tests or samples, verify that JavaScript assets are built:
- Check for `src\Components\Web.JS\dist\Debug\blazor.web.js`
- If not present, run from the repository root: `npm run build`

#### 3. Iterating on C# Changes

**Most of the time (no dependency changes):**
```bash
dotnet build --no-restore -v:q
```

Or with `eng\build.cmd`:
```bash
.\eng\build.cmd -NoRestore -NoBuildDeps -NoBuildNative -NoBuildNodeJS -NoBuildJava -NoBuildInstallers -verbosity:quiet
```

**When you've added/changed project references or package dependencies:**

First restore:
```bash
.\restore.cmd
```

Then build:
```bash
dotnet build --no-restore -v:q
```

**Note:** The `-v:q` (or `-verbosity:quiet`) flag minimizes build output to only show success/failure and error details. Remove this flag if you need to see detailed build output for debugging.

#### 4. Building Individual Projects (Fixing Build Errors)

When fixing build errors in a specific project, you can build just that project without its dependencies for even faster iteration:

```bash
dotnet build <path-to-project.csproj> --no-restore --no-dependencies -v:q
```

**When to use `--no-dependencies`:**
- Fixing compilation errors in a single project (syntax errors, type errors, etc.)
- Making isolated changes that don't affect project references
- Rapid iteration on a specific library

**When NOT to use `--no-dependencies`:**
- You've changed public APIs that other projects depend on
- You need to verify that dependent projects still compile correctly
- You're unsure if your changes affect other projects (safer to build without this flag)

**Example:**
```bash
# Fix a compilation error in Components.Endpoints
dotnet build src\Components\Endpoints\src\Microsoft.AspNetCore.Components.Endpoints.csproj --no-restore --no-dependencies -v:q
```

#### Quick Reference

1. **First time only**: `.\eng\build.cmd` → create `artifacts\agent-sentinel.txt`
2. **Check JS assets**: Verify `src\Components\Web.JS\dist\Debug\blazor.web.js` exists, run `npm run build` if missing
3. **Most C# changes**: `dotnet build --no-restore -v:q`
4. **Fixing build errors in one project**: `dotnet build <project.csproj> --no-restore --no-dependencies -v:q`
5. **Added/changed dependencies**: Run `.\restore.cmd` first, then use step 3

### E2E Testing Structure

Tests live in `src/Components/test`. The structure includes:

- **testassets folder** - Contains test assets and scenarios
- **Components.TestServer project** - A web application that launches multiple web servers with different scenarios (different project startups). Avoid adding new startup files unless strictly necessary.

### Running E2E Tests Manually

1. **Build the tests**: Follow the build instructions to build the E2E test project and its dependencies.
2. **Start Components.TestServer**:
   ```bash
   cd src\Components\test\testassets\Components.TestServer
   dotnet run --project Components.TestServer.csproj
   ```
3. **Navigate to the test server** - The main server runs on `http://127.0.0.1:5019/subdir`
4. **Select a test scenario** - The main page shows a dropdown with all available test components
5. **Reproduce the scenario** to verify it works the same way as in the sample

Note: There are also other server instances launched for different test configurations (authentication, CORS, prerendering, etc.). These are listed in the "scenarios" table on the main page.

### Understanding Logging Configuration

#### Server-side (.NET) Logging

The server uses `Microsoft.Extensions.Logging.Testing.TestSink` for capturing logs. Log configuration is in `Program.cs`:

```csharp
.ConfigureLogging((ctx, lb) =>
{
    TestSink sink = new TestSink();
    lb.AddProvider(new TestLoggerProvider(sink));
    lb.Services.Add(ServiceDescriptor.Singleton(sink));
})
```

#### Client-side (Blazor WebAssembly) Logging

Logs appear in the browser console. Log levels:
- Logs with `warn:` prefix are Warning level
- Logs with `info:` prefix are Information level
- Logs with `fail:` prefix are Error level

The Blazor WebAssembly log level can be configured at startup:

```javascript
Blazor.start({
    logLevel: 1 // LogLevel.Debug
});
```

LogLevel values: Trace=0, Debug=1, Information=2, Warning=3, Error=4, Critical=5

For Server-side Blazor (SignalR):
```javascript
Blazor.start({
    circuit: {
        configureSignalR: builder => {
            builder.configureLogging("debug") // LogLevel.Debug
        }
    }
});
```

#### Viewing Logs in Playwright

Use `browser_console_messages` to see JavaScript console output including .NET logs routed to the console.

### Creating E2E Tests

E2E tests are located in `src/Components/test/E2ETest`.

1. First, check if there are already E2E tests for the component/feature area you're working on
2. Try to add an additional test to existing test files when possible
3. When adding test coverage, prefer extending existing test components and assets over creating a set of new ones if it doesn't complicate the existing ones excessively. This reduces test infrastructure complexity and keeps related scenarios together.

### Running E2E Tests

The E2E tests use Selenium. Selenium is the E2E framework for this area - do not introduce a
second one. The Playwright references in this guide are the Playwright MCP **browser tool**,
used for manual interactive validation of a sample while developing; it is not a test framework
here and is not referenced by the E2E test project. To build and run tests:

```bash
# Build the E2E test project and its dependencies
dotnet build src/Components/test/E2ETest/Microsoft.AspNetCore.Components.E2ETests.csproj --no-restore -v:q

# After the build succeeds, run a specific test
dotnet test src/Components/test/E2ETest/Microsoft.AspNetCore.Components.E2ETests.csproj --no-build --filter "FullyQualifiedName~TestName"
```

For the first E2E run in a fresh worktree, or after relevant build, configuration, or output changes, run the dependency-aware build above. Do not use `--no-dependencies` to prepare E2E tests when referenced test-app outputs may be stale or missing. It may copy existing dependency outputs, but it does not rebuild referenced projects or apps. After the build succeeds, `--no-build` is the supported fast loop for repeated targeted tests while those inputs remain unchanged.

**Important**: Never run all E2E tests locally as that is extremely costly. Full test runs should only happen on CI machines.

If a test is failing, it's best to run the server manually and navigate to the test to investigate. The test output won't be very useful for debugging.
