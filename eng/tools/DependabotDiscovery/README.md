# DependabotDiscovery.csproj

This is **not a real project**. Nothing globs `eng/tools/**` into the real build and it's not added
to `AspNetCore.slnx`, so it's never built, packed, or signed as part of any normal build or CI run.
Do **not** add `ExcludeFromBuild` (or similar) to try to make that more explicit: Arcade's
`ExcludeFromBuild` disables NuGet restore entirely, which would also stop Dependabot from resolving
any packages out of this project.

Projects now use ordinary `<PackageReference>` items and NuGet Central Package Management. This
discovery project remains the entry point for the existing Dependabot jobs, selecting the packages
those jobs should update without requiring them to restore every product and platform.

Its references use the central versions in `Directory.Packages.props`.
The version properties remain in `eng/Versions.props`, shared with the real consuming projects.

Only packages that meet **all** of the following belong here:
- Not already managed by Maestro (see `eng/Version.Details.props`) and not pinned via the shared
  `$(IdentityModelVersion)` property - those are updated by other automation and are excluded from
  Dependabot via `.github/dependabot.yml`.
- Not an in-repository shipping assembly (see `eng/ShippingAssemblies.props`) referenced as a
  project, or an intentionally pinned historical package for an in-repository assembly.
- Backed by a real, resolvable `$(SomePackageNameVersion)` property in `eng/Versions.props` (per the
  central mappings in `Directory.Packages.props`).
- Actually reportable by Dependabot as a top-level, updatable dependency. Two real, referenced
  packages are excluded for this reason: `NETStandard.Library` (dependabot-core hard-codes it as
  "resolved but not reported", being a compile-time SDK compatibility shim) and
  `Microsoft.CodeAnalysis.PublicApiAnalyzers` (the .NET SDK implicitly references it itself, so
  MSBuild marks any explicit reference `IsImplicitlyDefined="true"`, which dependabot-core treats
  as non-top-level).

The project targets both `net472` and `$(DefaultNetCoreTargetFramework)` because a few packages only
support one or the other (e.g. `Microsoft.Owin.*` ship net45-only assets; `Yarp.ReverseProxy` and
similar are netcoreapp-only). Packages incompatible with a TFM are conditioned out of it via
`Condition="'$(TargetFramework)' == 'net472'"` (or `!=`) rather than causing a restore failure.
`NU1605` (downgrade conflicts from combining ~90 unrelated packages' transitive dependencies in one
project) is suppressed via `NoWarn`, since this project is never actually built or consumed.

## Keeping this file up to date

Whenever you add, remove, or rename a package in `Directory.Packages.props`, make the matching change
here (unless it's Maestro- or IdentityModel-managed). `eng/scripts/CodeCheck.ps1` fails CI if
`Directory.Packages.props` changes without a corresponding change to this file.

## Adding a package

1. Confirm it meets all the criteria listed above (not Maestro/IdentityModel-managed, not an
   in-repository shipping assembly name, has a real `$(SomePackageNameVersion)` property).
2. Add `<PackageReference Include="PackageName" />` to the main
   `ItemGroup`.
3. Run `dotnet restore` on this project. If it fails with `NU1202` (package doesn't support a
   target framework), the package needs its own TFM-conditioned `ItemGroup` instead of the main
   one - move it into (or add) an `ItemGroup Condition="'$(TargetFramework)' == 'net472'"` or
   `!= 'net472'` block, matching whichever TFM it actually supports (see the existing
   `Microsoft.Owin.*` and `Grpc.AspNetCore`/etc. groups for examples). Other restore errors
   (e.g. `NU1101`) usually mean the package isn't available on the `dotnet-public` feed and needs
   to be mirrored there first.
