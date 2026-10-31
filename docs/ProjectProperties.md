Project Properties
==================

In addition to the standard set of MSBuild properties supported by Microsoft.NET.Sdk, projects in this repo often use these additional properties.

Property name      | Meaning
-------------------|--------------------------------------------------------------------------------------------
IsPackable         | Set to `true` when the project should produce a package. That package may or may not ship to customers (depending on `IsShippingPackage`). Defaults to `true` for analyzer and implementation projects; `false` otherwise.
IsShipping         | Set to `true` when the project output is intended for use by customers. Defaults to `true` for analyzer, implementation and specification test projects; `false` otherwise.
IsShippingPackage  | Set to `true` when a package produced from project is intended for use by customers. Defaults to `IsShipping`. Note this may be `true` even for projects with `IsPackable` set to `false`.
IsAspNetCoreApp    | Set to `true` when the assembly is part of the [Microsoft.AspNetCore.App shared framework](./SharedFramework.md) and is not available as a NuGet package (unless `IsPackable` is also set to `true` -- the default). Defaults to `false`.
TestDependsOnMssql | Set to `true` when your tests depends on SQL Server. This will ensure distribute tests on Helix install LocalDB ([more information on Helix](./Helix.md)). Defaults to `false`.
