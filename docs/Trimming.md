# Guide to trimming ASP.NET Core

This guide discusses the steps required to enable, annotate, and verify trimming in ASP.NET Core assemblies. An assembly that supports trimming knows all the types and members it needs at runtime. Unused code is removed when an app is published, reducing app size. Trimming is also a precursor to supporting AOT, which requires knowledge of the APIs used by an app on build.

[Trim self-contained deployments and executables](https://learn.microsoft.com/dotnet/core/deploying/trimming/trim-self-contained) has general documentation for trimming. It includes:

* How to output trim warnings. Typically done by publishing an app.
* How to react to trim warnings. Attributes can be placed on APIs to provide type info, suppress warnings, or verify that an API is incompatible with trimming.
* How to trim libraries.

## Assembly trimming order

Trim assemblies from the bottom up. Order is important because annotating an assembly impacts its dependents and their annotations. Annotating from the bottom up reduces churn.

For example, `Microsoft.AspNetCore.Http` depends on `Microsoft.AspNetCore.Http.Abstractions` so `Microsoft.AspNetCore.Http.Abstractions` should be annotated first.

## Trim an ASP.NET Core assembly

The first step to trimming an ASP.NET Core assembly is adding it to `LinkabilityChecker`. `LinkabilityChecker` is a tool in the ASP.NET Core repo that runs ILLink on its referenced assemblies and outputs trim warnings.

1. Update the project file to enable trimming by adding `<IsTrimmable>true</IsTrimmable>`. This property configures the build to add metadata to the assembly to indicate it supports trimming. It also enables trimming analysis on the project.
2. Run `eng/scripts/GenerateProjectList.ps1` to update the list of projects that are known to be trimmable. The script adds the project to the list of known trimmable projects at [`eng/TrimmableProjects.props`](../eng/TrimmableProjects.props).
3. Open `src/Tools/Tools.slnf`
4. Add the project to `Tools.slnf`:
    1. Right-click `LinkabilityCheck` and select *Load Direct Dependencies*.
    2. Update the solution filter.
5. Build `LinkabilityChecker`.

`LinkabilityChecker` is required to get a complete list of trim warnings because there isn't enough type information when building an assembly on its own. It's possible to introduce new trim warnings during typical dev work after annotating an assembly for trimming. `LinkabilityChecker` automatically runs on the build server and catches new warnings.

## Fix trim warnings

[Introduction to trim warnings](https://learn.microsoft.com/dotnet/core/deploying/trimming/fixing-warnings) and [Prepare .NET libraries for trimming](https://learn.microsoft.com/dotnet/core/deploying/trimming/prepare-libraries-for-trimming) discuss how to fix trim warnings. There is also a complete list of all the trim warnings with some more detail.

## Updating the baselines

If a suppressed warning has been resolved, or if new trimmer warnings are to be baselined, run the following command:

```sh
dotnet build /p:GenerateLinkerWarningSuppressions=true
```

This should update the `WarningSuppressions.xml` files associated with projects.

⚠️ Note that the generated file sometimes messes up formatting for some compiler generated nested types and you may need to manually touch up these files on regenerating. The generated file uses braces `{...}` instead of angle brackets `<...>`:

```diff
- LegacyRouteTableFactory.&lt;&gt;c.{Create}b__2_1(System.Reflection.Assembly)
+ LegacyRouteTableFactory.&lt;&gt;c.&lt;Create&gt;b__2_1(System.Reflection.Assembly)
```

## Validate trimming behavior

We have infrastructure for writing tests to validate trimming behavior.
The two most common tasks are confirming that functionality still works after trimming and confirming that a particular type or member was not preserved.

### Infrastructure

Because trimming is only performed during publish, the tests can't be normal xunit tests.
Instead, we have a system that wraps individual source files in appropriately configured projects and publishes them as standalone executables.
These executables are then run and the test outcome is based on the exit code.
The tests are powered by [trimmingTests.targets](../eng/testing/linker/trimmingTests.targets), which is based on corresponding functionality in https://github.com/dotnet/runtime.

### Adding a new test project

Adding a new test project is very simple just call it \*.TrimmingTests.proj and give it a body like
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <TestConsoleAppSourceFiles Include="MyTest.cs">
  </ItemGroup>
</Project>
```

Unfortunately, there's no good way to specify which aspnetcore projects the tests depend upon so, to avoid dependency ordering problems, it's easiest to specify that they should be built after the main build.
Do this by adding an entry to [RequiresDelayedBuildProjects.props](../eng/RequiresDelayedBuildProjects.props).

### Adding a new test

A test is just a C# file with a main method (or top-level statements, if you prefer).
Write a program that either should keep working after trimming or that validates a particular type/member has been trimmed (e.g. using `Type.GetType`).
If things behave as expected exit with code `100` (a magic number intended to prevent accidental passes).
Any other result a different exit code or a crash will count as a failure.

Now that you have a test program, add a `TestConsoleAppSourceFiles` item to the corresponding test project.

If you have additional files, typically because you want to share code between tests, you can use `AdditionalSourceFiles`.
If you want to en/disable a particular feature switch, you can use `EnabledFeatureSwitches`/`DisabledFeatureSwitches`.

### Running tests

To run the tests locally, build the projects it depends on (it's usually easiest to just build the whole repo) and then run
```sh
.dotnet\dotnet.exe test path\to\project\folder\
```

Alternatively, you can use `Activate.ps1` to ensure you're using the right dotnet binary.

### Native AOT

Native AOT testing is substantially the same: just name your project \*.NativeAotTests.proj, rather than \*.TrimmingTests.proj.

### Tips

* It's easier to author your test as a standalone Native AOT app and then copy the final code into the test (don't forget to add the copyright header).
  The two main advantages are that building is much faster and you get console output that you can use to help debug your test.
* If you are using AdditionalSourceFiles and you need to make a change to one of the additional files, you will also need to update the test file itself * just editing the additional file _won't trigger a rebuild_ on the next run.
* `Type.GetType` is known to the trimmer, so you'll have to (a) make it impossible to statically determine what argument you're passing (so that it isn't preserved) and (b) suppress the resulting warning: `[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2057:UnrecognizedReflectionPattern", Justification = "Using GetType to test trimming")]`
* Pass an assembly-qualified name (`"ns.type, assembly.name"`) to `Type.GetType` as it searches only the executing assembly and corlib by default.
* There's no output indicating that a test has passed, so it's important to check that it's actually running (e.g. by inserting an artificial failure).
* If your test depends on any JSON serialization functionality, you'll probably need to disable the `System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault` feature switch.
* If your test validates that a type is being trimmed, it's good practice to write a dual test (sharing the same helpers) that validates that it is preserved (in a different scenario, of course) so that the trimming test doesn't accidentally pass because of (e.g.) a typo in the type name passed to `GetType`.