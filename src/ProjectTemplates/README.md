# Templates

These are project templates which are used in .NET Core for creating ASP.NET Core applications.

## Description

The following contains a description of each sub-directory in the `ProjectTemplates` directory.

- `Shared`: Contains a collection of shared constants and helper methods/classes including the infrastructure for managing dotnet processes to create, build, run template tests.
- `Web.Client.ItemTemplates`: Contains the Web Client-Side File templates, includes things like less, scss, and typescript
- `Web.ItemTemplates`: Contains the Web File templates, includes things like: protobuf, razor component, razor page, view import and start pages
- `Web.ProjectTemplates`: Contains the ASP.NET Core Web Template pack, including Blazor Server, WASM, Empty, Grpc, Razor Class Library, RazorPages, MVC, WebApi.
- `McpServer.ProjectTemplates`: Contains the standalone MCP Server Template pack.
- `migrations`: Contains migration related scripts.
- `scripts`: Contains a collection of scripts that help running tests locally that avoid having to install the templates to the machine.
- `test`: Contains the template tests.
  - `Templates.Blazor.Tests`: Contains the Blazor template tests. These are currently split out due to not being Helix ready yet.
- `testassets`: Contains assets used by the tests, like a dotnet tools installer

## Submitting pull requests

You can submit changes for templates in this repo by submitting a pull request. If you make changes to any
`content/*/.template.config/template.json` files, build locally (see below) and include any
`content/*/.template.config/localize/` changes in your pull request. (Your build may update the strings in those
files for later localization.)

## Building and running locally

### Preflight

From the repository root:

```powershell
git submodule update --init --recursive
.\restore.cmd
. .\activate.ps1
```

Use `./restore.sh` and `source activate.sh` on Linux or macOS. The `Run-*-Locally.ps1` workflow below currently
requires Windows x64.

Generated projects under `src\ProjectTemplates\scripts` are ignored by Git but are still found by the
repository-wide project glob. Remove each generated project directory before starting a full repository build.

### Package-only validation

Use the ProjectTemplates area build when only template generation and package contents need validation:

```powershell
.\src\ProjectTemplates\build.cmd -pack -configuration Release
```

This produces the four template packages under `artifacts\packages\Release\Shipping`. It does not produce the
complete local package graph, runtime archive, or `Templates.Tests` imports needed to restore, publish, and run a
generated application.

### Prepare generated-application validation

Build and pack the full product graph first:

```powershell
.\eng\build.cmd -all -pack -configuration Release -NoBuildInstallers
```

On Windows, runtime and targeting pack projects can still invoke `wix.exe`; `-NoBuildInstallers` only excludes
installer projects. Ensure WiX is available when building that graph. Do not pass `/p:GenerateInstallers=false`
to the full product pack: runtime-pack targets still expect the installer target. That property is appropriate
for focused builds that exclude runtime-pack generation.

Then build the delayed test project that generates the local restore imports:

```powershell
.\eng\build.cmd -configuration Release `
    -projects src\ProjectTemplates\test\Templates.Tests\Templates.Tests.csproj `
    -NoBuildDeps -NoBuildInstallers `
    /p:OnlyTestProjectTemplates=true /p:GenerateInstallers=false
```

Before running a local script, verify that these producers created:

- `artifacts\packages\Release\Shipping\aspnetcore-runtime-*-dev-win-x64.zip`
- the complete matching package graph in `artifacts\packages\Release\Shipping` and `NonShipping`
- `src\ProjectTemplates\test\Templates.Tests\bin\Release\net11.0\TestTemplates\Directory.Build.props` and
  `Directory.Build.targets`

If an artifact or source is missing, rerun its producer. Do not create an empty package-source directory.

### Instantiate, publish, and run a template

Run the script for the template variant. For example:

```powershell
.\src\ProjectTemplates\scripts\Run-BlazorWeb-Locally.ps1 -Configuration Release
```

The script repacks the selected template project, reinstalls the locally built package, recreates its generated
project under `scripts`, imports the local Shipping and NonShipping package sources, and publishes the generated
application to `.publish`.

#### Generate a template with Individual authentication

Use a local runner when the generated application must be restored, published, and run against the locally built ASP.NET Core packages and runtime:

```powershell
.\src\ProjectTemplates\scripts\Run-BlazorWeb-Locally.ps1 `
    -Auth Individual -Interactivity Auto -Configuration Release
```

Equivalent runners are available for Razor Pages, MVC, and standalone Blazor WebAssembly:

```powershell
.\src\ProjectTemplates\scripts\Run-Razor-Locally.ps1 -Auth Individual
.\src\ProjectTemplates\scripts\Run-Starterweb-Locally.ps1 -Auth Individual
.\src\ProjectTemplates\scripts\Run-BlazorWasm-Locally.ps1 -Auth Individual
```

The Blazor Auto runner publishes the server project to
`src\ProjectTemplates\scripts\MyBlazorApp\MyBlazorApp\.publish\MyBlazorApp.exe`.
The helper restores the selected main project before running Entity Framework migrations and passes it explicitly as both the EF target and startup project. SQLite is the default; pass `-UseLocalDb` only when LocalDB is required.

To validate template installation and generation without modifying the default template hive, install the locally built package into a custom hive:

```powershell
$packages = @(Get-ChildItem `
    ".\artifacts\packages\Release\Shipping\Microsoft.DotNet.Web.ProjectTemplates.*-dev.nupkg")
if ($packages.Count -ne 1) {
    throw "Expected exactly one locally built web project-template package, but found $($packages.Count)."
}

$hive = Join-Path $PWD "artifacts\template-hive"
$output = Join-Path $PWD "artifacts\generated\BlazorIndividual"

dotnet new install $packages[0].FullName --debug:custom-hive $hive
dotnet new blazor --auth Individual --interactivity Auto --no-restore `
    --output $output `
    --debug:disable-sdk-templates `
    --debug:custom-hive $hive
```

For Razor Pages or MVC, replace `blazor --interactivity Auto` with `webapp` or `mvc` and retain `--auth Individual`. `--debug:custom-hive` isolates template registration; `--debug:disable-sdk-templates` ensures the SDK-bundled template does not take precedence.

Custom-hive creation validates installation and generation only. Use the `Run-*-Locally.ps1` runners for restore, build, publish, and runtime validation because they add the generated `Templates.Tests` imports, both local package sources, and the isolated locally built runtime.

Run framework-dependent outputs with the isolated SDK and locally built `Microsoft.AspNetCore.App` prepared by
the script:

```powershell
$scripts = Join-Path $PWD "src\ProjectTemplates\scripts"
$env:DOTNET_ROOT = Join-Path $scripts ".dotnet"
$env:DOTNET_ROOT_X86 = $env:DOTNET_ROOT
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
$env:ASPNETCORE_URLS = "http://127.0.0.1:5005"
& "$scripts\MyBlazorApp\.publish\MyBlazorApp.exe"
```

Confirm behavior at the application boundary, then stop the process with <kbd>Ctrl</kbd>+<kbd>C</kbd>:

| Area | Script | Generated output | Manual probe |
| --- | --- | --- | --- |
| Web API | `Run-WebApi-Locally.ps1` | `webapi\.publish\webapi.exe` | Request `/weatherforecast` |
| Blazor | `Run-BlazorWeb-Locally.ps1` | `MyBlazorApp\.publish\MyBlazorApp.exe` (Server) or `MyBlazorApp\MyBlazorApp\.publish\MyBlazorApp.exe` (Auto/WebAssembly) | Request `/`, complete registration/login for Individual auth, and interact with the changed UI |
| Worker | `Run-Worker-Locally.ps1` | `worker\.publish\worker.exe` | Observe the recurring worker log |
| gRPC | `Run-gRPC-Locally.ps1` | `grpc\.publish\grpc.exe` | Call the Greeter service with an HTTP/2 gRPC client |
| MCP | `Run-McpServer-Locally.ps1` | `mcpserver\.publish\mcpserver.exe` | Connect an MCP stdio client and initialize a session |

Rerun the same script after editing template sources; it removes and recreates only that generated output. Use
`dotnet new uninstall` with the isolated SDK to list and remove the locally installed package when finished.

### Automated tests

To run ProjectTemplate tests, first ensure the ASP.NET localhost development certificate is installed and
trusted. Otherwise, navigation tests fail with "Certificate error: Navigation blocked".

Run `src\ProjectTemplates\build.cmd -test -NoRestore -NoBuild -NoBuildDeps -configuration Release` (or the
equivalent `build.sh` command) after producing its prerequisites. ProjectTemplates tests require Visual Studio
unless a full CI build is performed.

##### Focused `Templates.Tests` workflow

`Templates.Tests.csproj` is a delayed-build project. When selecting it directly through the repository build
orchestration, pass `/p:OnlyTestProjectTemplates=true`; otherwise the normal delayed-project import can change
project selection before the requested test runs.

On Windows, a focused validation that excludes runtime-pack generation can pass
`/p:GenerateInstallers=false`. `-NoBuildInstallers` controls installer project selection by setting
`BuildInstallers=false`, but it does not suppress installer generation requested while producing runtime packs.

The applications generated by `Templates.Tests` restore from both
`artifacts\packages\<Configuration>\Shipping` and `artifacts\packages\<Configuration>\NonShipping`. For this
branch, restore requires the complete matching `11.0.0-dev` package graph in those sources, not only the template
packages. Before running a focused test, map each required package to its producer and run the corresponding
build or pack steps. If a local artifact source is missing, produce it; do not create an empty directory to make
the source path exist.

#### Running Blazor Playwright Template Tests

1. From the root of the repo, build the templates: `.\eng\build.cmd -all -pack`
2. `cd .\src\ProjectTemplates\test\Templates.Blazor.Tests`
3. `dotnet test .\Templates.Blazor.Tests.csproj` with optional `--filter` arg to run a specific test.

The requisite browsers should be automatically installed. If you encounter browser errors, the browsers can be manually installed via the following script, replacing `[TFM]` with the current target TFM (ex. `net8.0`).

```cmd
.\bin\Debug\[TFM]\playwright.ps1 install
```

#### Conditional tests & skipping test platforms

Individual test methods can be decorated with attributes to configure them to not run ("skip running") on certain platforms. The `[ConditionalFact]` and `[ConditionalTheory]` attributes must be used on tests using the skip attributes in order for them to actually be skipped:

``` csharp
[ConditionalFact]
[OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
[SkipOnHelix("cert failure", Queues = "All.OSX;" + HelixConstants.Windows10Arm64)]
public async Task MvcTemplate_SingleFileExe()
{
```

An entire test project can be configured to skip specific platforms using the `<SkipHelixQueues>` property in the project's .csproj file, e.g.:

```xml
<SkipHelixQueues>
    $(HelixQueueArmDebian);
</SkipHelixQueues>
```

Tests that are skipped should have details, or better yet link to an issue, explaining why they're being skipped, either as a string argument to the attribute or a code comment.

#### Test timeouts

When tests are run as part of the CI infrastructure, a number of different timeouts can impact whether tests pass or not.

##### Helix job timeout

When queuing test jobs to the Helix infrastructure, a timeout value is passed that the entire Helix job must complete within, i.e. that job running on a single queue. This default value is set in [eng\targets\Helix.props](/eng/targets/Helix.props):

```xml
<HelixTimeout>00:45:00</HelixTimeout>
```

This value is printed by the Helix runner at the beginning of the console log, formatted in seconds, e.g.:

```log
Console log: 'ProjectTemplates.Tests--net8.0' from job b2f6fbe0-4dbe-45fa-a123-9a8c876d5923 (ubuntu.1804.armarch.open) using docker image mcr.microsoft.com/dotnet-buildtools/prereqs:debian-11-helix-arm64v8-20211001171229-97d8652 on ddvsotx2l137
running $HELIX_CORRELATION_PAYLOAD/scripts/71557bd7f20e49fbbaa81cc79bd57fd6/execute.sh in /home/helixbot/work/C08609D9/w/B3D709E1/e max 2700 seconds
```

Note that some test projects might override this value in their project file and that some Helix queues are slower than others, so the same test job might complete within the timeout on one queue but exceed the timeout on another (the ARM queues are particularly prone to being slower than their AMD/Intel counterparts).

##### Helix runner timeout

The [Helix test runner](/eng/tools/HelixTestRunner) launches the actual process that runs tests within a Helix job and when doing so configures its own timeout that is 5 minutes less than the Helix job timeout, e.g. if the Helix job timeout is 45 minutes, the Helix test runner process timeout will be 40 minutes.

If this timeout is exceeded, the Helix test runner will capture a dump of the test process before terminating it and printing a message in the console log, e.g.:

```log
2022-05-12T00:27:28.8279660Z Non-quarantined tests exceeded configured timeout: 40m.
```

##### Helix runner `dotnet test` timeout

When running in Helix, a test hang timeout, e.g. `dotnet test --blame-hang-timeout 15m` , is configured in [eng\tools\HelixTestRunner\TestRunner.cs](/eng/tools/HelixTestRunner/TestRunner.cs)

```csharp
public async Task<int> RunTestsAsync()
{
    ...
        var commonTestArgs = $"test {Options.Target} --diag:{diagLog} --logger xunit --logger \"console;verbosity=normal\" " +
                                "--blame-crash --blame-hang-timeout 15m";
```

This timeout applies to each individual `[Fact]` or `[Theory]`. Note that for `[Theory]` this timeout is **not** reset for each instance of the theory, i.e. the entire `[Theory]` must run within the specified timeout.

If this timeout is triggered, a message will be printed to the `vstest.datacollector.[date-time-stamp].log` file for the test run, e.g.:

```
19:54:18.888, 4653892436493, datacollector.dll, The specified inactivity time of 15 minutes has elapsed. Collecting hang dumps from testhost and its child processes
```

**Note:** It's a good idea to spread the number of cases for `[Theory]` tests across different test methods if the test method takes more than a few seconds to complete as this will help to keep the total `[Theory]` runtime less than the configured timeout, e.g.:

``` csharp
[ConditionalTheory]
[SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/28090", Queues = HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
[InlineData("IndividualB2C", null)]
[InlineData("IndividualB2C", new[] { ArgConstants.UseProgramMain })]
[InlineData("IndividualB2C", new[] { ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
[InlineData("IndividualB2C", new[] { ArgConstants.UseProgramMain, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
public Task MvcTemplate_IdentityWeb_IndividualB2C_BuildsAndPublishes(string auth, string[] args) => MvcTemplateBuildsAndPublishes(auth: auth, args: args);

[ConditionalTheory]
[SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/28090", Queues = HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
[InlineData("SingleOrg", null)]
[InlineData("SingleOrg", new[] { ArgConstants.UseProgramMain })]
[InlineData("SingleOrg", new[] { ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
[InlineData("SingleOrg", new[] { ArgConstants.UseProgramMain, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
[InlineData("SingleOrg", new[] { ArgConstants.CallsGraph })]
[InlineData("SingleOrg", new[] { ArgConstants.UseProgramMain, ArgConstants.CallsGraph })]
public Task MvcTemplate_IdentityWeb_SingleOrg_BuildsAndPublishes(string auth, string[] args) => MvcTemplateBuildsAndPublishes(auth: auth, args: args);
```

## More Information

For more information, see the [ASP.NET Core README](../../README.md).
