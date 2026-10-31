# Framework

Contains packages that bundle reference and implementation assembles for Microsoft.AspNetCore.App.Runtime shared framework and for use in targeting pack installers.

## Description

The following contains a description of each sub-directory in the `Framework` directory.

- `App.Ref.Internal`: Empty project for identifying the non-stable version of AspNetCore at the time the targeting pack was last built. This package does not ship and is for internal use.
- `App.Ref`: Contains reference assemblies, documentation, and other design-time assets
- `App.Runtime`: Provides a default set of APIs for building ASP.NET Core applications, and assets used for self-contained deployments.

## Development Setup

### Build

To build this specific project from source, follow the instructions [on building the project](../../docs/BuildFromSource.md#step-3-build-the-repo).

### Test

To run the tests for this project, [run the tests on the command line](../../docs/BuildFromSource.md#running-tests-on-command-line) in this directory.

## More Information

For more information, see the [ASP.NET Core README](../../README.md).
