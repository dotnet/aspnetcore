# Templates

These are project templates which are used in .NET Core for creating ASP.NET Core applications.

## Description

The following contains a description of each sub-directory in the `ProjectTemplates` directory.

- `Shared`: Contains a collection of shared constants and helper methods/classes including the infrastructure for managing dotnet processes to create, build, run template tests.
- `Web.Client.ItemTemplates`: Contains the Web Client-Side File templates, includes things like less, scss, and typescript
- `Web.ItemTemplates`: Contains the Web File templates, includes things like: protobuf, razor component, razor page, view import and start pages
- `Web.ProjectTemplates`: Contains the ASP.NET Core Web Template pack, including Blazor Server, WASM, Empty, Grpc, Razor Class Library, RazorPages, MVC, WebApi.
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

## Building locally

### Build

Some projects in this repository (like SignalR Java Client) require JDK installation and configuration of `JAVA_HOME` environment variable.

1. If you don't have the JDK installed, you can find it from https://www.oracle.com/technetwork/java/javase/downloads/index.html
1. After installation define a new environment variable named `JAVA_HOME` pointing to the root of the latest JDK installation (for Windows it will be something like `c:\Program Files\Java\jdk-12`).
1. Add the `%JAVA_HOME%\bin` directory to the `PATH` environment variable

To build the ProjectTemplates, use one of:

1. Run `eng\build.cmd -all -pack -configuration Release` in the repository root to build and pack all of the repo, including template projects.
1. Run `src\ProjectTemplates\build.cmd -pack -configuration Release` to produce NuGet packages only for the template projects.
    - This will also build and pack the shared framework.

**Note** use `eng/build.sh` or `src/ProjectTemplates/build.sh` on non-Windows platforms.

### Test

#### Running ProjectTemplate tests

To run ProjectTemplate tests, first ensure the ASP.NET localhost development certificate is installed and trusted.
Otherwise, you'll get a test error "Certificate error: Navigation blocked".

Then, use one of:

1. Run `src\ProjectTemplates\build.cmd -test -NoRestore -NoBuild -NoBuildDeps -configuration Release` (or equivalent src\ProjectTemplates\build.sh` command) to run all template tests.
1. To test specific templates, use the `Run-[Template]-Locally.ps1` scripts in the script folder.
    - These scripts do `dotnet new -i` with your packages, but also apply a series of fixes and tweaks to the created template which keep the fact that you don't have a production `Microsoft.AspNetCore.App` from interfering.
1. Run templates manually with `custom-hive` and `disable-sdk-templates` to install to a custom location and turn off the built-in templates e.g.
    - `dotnet new -i Microsoft.DotNet.Web.ProjectTemplates.6.0.6.0.0-dev.nupkg --debug:custom-hive C:\TemplateHive\`
    - `dotnet new angular --auth Individual --debug:disable-sdk-templates --debug:custom-hive C:\TemplateHive\`
1. Install the templates to an existing Visual Studio installation.
    1. Pack the ProjectTemplates: `src\ProjectTemplates\build.cmd -pack -configuration Release`
        - This will produce the `*dev.nupkg` containing the ProjectTemplates at `artifacts\packages\Release\Shipping\Microsoft.DotNet.Web.ProjectTemplates.7.0.7.0.0-dev.nupkg`
    2. Install ProjectTemplates in local Visual Studio instance: `dotnet new install "<REPO_PATH>\artifacts\packages\Release\Shipping\Microsoft.DotNet.Web.ProjectTemplates.7.0.7.0.0-dev.nupkg"`
    3. Run Visual Studio and test out templates manually.
    4. Uninstall ProjectTemplates from local Visual Studio instance: `dotnet new uninstall Microsoft.DotNet.Web.ProjectTemplates.7.0`

**Note** ProjectTemplates tests require Visual Studio unless a full build (CI) is performed.

**Note** Because the templates build against the version of `Microsoft.AspNetCore.App` that was built during the
previous step, it is NOT advised that you install templates created on your local machine using just
`dotnet new -i [nupkgPath]`.

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
    $(HelixQueueArmDebian12);
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
