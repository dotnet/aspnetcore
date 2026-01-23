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

### Overview

The workflow for implementing new features in the Components area follows these steps:

1. **Create a sample scenario first** - This is the most important first step. Update code in one of the projects in the `src/Components/Samples` folder to include the scenarios for the feature you want to build. This allows you to develop and test the feature interactively before writing formal tests.

2. **Build and test interactively** - Build the feature and use Playwright to test it in the browser, ensuring it works end-to-end at a basic level.

### Sample Projects

The `src/Components/Samples` folder contains several sample projects you can use for developing and testing features:

- **BlazorServerApp** - A Blazor Server application for testing server-side scenarios
- **BlazorUnitedApp** - A Blazor Web App (united/hybrid mode) for testing combined server and WebAssembly scenarios
- **BlazorUnitedApp.Client** - The client-side portion of the BlazorUnitedApp

**Always start by adding your feature scenario to one of these sample projects first.** This allows you to:
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
.\eng\build.cmd -NoRestore -NoBuildDeps -NoBuildRepoTasks -NoBuildNative -NoBuildNodeJS -NoBuildJava -NoBuildInstallers -verbosity:quiet
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

The E2E tests use Selenium. To build and run tests:

```bash
# Build the E2E test project (this includes all test assets as dependencies)
dotnet build src/Components/test/E2ETest/Microsoft.AspNetCore.Components.E2ETests.csproj --no-restore -v:q

# Run a specific test
dotnet test src/Components/test/E2ETest/Microsoft.AspNetCore.Components.E2ETests.csproj --no-build --filter "FullyQualifiedName~TestName"
```

**Important**: Never run all E2E tests locally as that is extremely costly. Full test runs should only happen on CI machines.

If a test is failing, it's best to run the server manually and navigate to the test to investigate. The test output won't be very useful for debugging.

