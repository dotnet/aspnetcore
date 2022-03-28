# Guide to trimming ASP.NET Core

This guide discusses the steps required to enable, annotate, and verify trimming in ASP.NET Core assmblies. Enabling trimming allows .NET to statically know which types and members are used by an app. Unused code can be removed when an app is published, reducing app size. Trimming is also a prelude to supporting AOT, which requires knowledge of the APIs used by an app on build.

The general documentation for trimming is found at [Trim self-contained deployments and executables](https://docs.microsoft.com/dotnet/core/deploying/trimming/trim-self-contained). It includes:

* How to output trim warnings. Typically done by publishing an app.
* How to react to trim warnings. Attributes can be placed on APIs to provide type info, or suppress warnings, or verify that an API is incompatible with trimming.
* How to trim libraries.

## Assembly trimming order

Ideally, assemblies should be annotated for trimming from the bottom up. Order is important because annotating an assembly impacts its dependents and their annotations. Annotating from the bottom up reduces churn.

For example, `Microsoft.AspNetCore.Http` depends on `Microsoft.AspNetCore.Http.Abstractions` so `Microsoft.AspNetCore.Http.Abstractions` should be annotated first.

## Trim an ASP.NET Core assembly

The first step to trimming an ASP.NET Core assembly is adding it to `LinkabilityChecker`. `LinkabilityChecker` is a tool in the ASP.NET Core repo that runs ILLink on its referenced assemblies and outputs trim warnings.

1. Add the project to `Tools.slnf`.
  1. Right click solution and select *View unloaded projects*.
  2. Right click on the project and select *Reload project*.
  3. Update the solution filter.
2. Update the project file to enable trimming `<Trimmable>true</Trimmable>`.
3. Run `eng/scripts/GenerateProjectList.ps1` to update the list of projects that are known to be trimmable.
4. Build `LinkabilityChecker`.

There isn't enough type information when bulding an assembly to provide all trimming warnings which is why `LinkabilityChecker`, or publish an app is required. It's possible to introduce new trim warnings during typical dev work after an assembly has been annotated for trimming. `LinkabilityChecker` runs on the build server and catches new warnings.

## Fix trim warnings

[Introduction to trim warnings](https://docs.microsoft.com/en-us/dotnet/core/deploying/trimming/fixing-warnings) and [Prepare .NET libraries for trimming](https://docs.microsoft.com/dotnet/core/deploying/trimming/prepare-libraries-for-trimming) discuss how to fix trim warnings. There is also a list of all the trim warnings with some more detail.

## Updating the baselines

If a suppressed warning has been resolved, or if new trimmer warnings are to be baselined, run the following command:

```
dotnet build /p:GenerateLinkerWarningSuppressions=true
```

This should update the `WarningSuppressions.xml` files associated with projects.

⚠️ Note that the generated file sometimes messing up formatting for some compiler generated nested types and you may need to manually touch up these files on regenerating. The generated file uses braces `{...}` instead of angle brackets `<...>`:

```diff
- LegacyRouteTableFactory.&lt;&gt;c.{Create}b__2_1(System.Reflection.Assembly)
+ LegacyRouteTableFactory.&lt;&gt;c.&lt;Create&gt;b__2_1(System.Reflection.Assembly)
```
