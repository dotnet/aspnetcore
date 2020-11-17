# DataProtection

Data Protection APIs for protecting and unprotecting data. You can find documentation for Data Protection in the [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core/security/data-protection/).

## Description

The following contains a description of each sub-directory in the `DataProtection` directory.

- `Abstractions`: Contains the source files for the main DataProtection interfaces like IDataProtector and IDataProtectionProvider
- `Cryptography.Internal`: Contains the source files for cryptography infrastucture, applications and libraries should not reference this package directly.
- `Cryptography.KeyDerivation`: Contains the source files related to key derivation, i.e. PBKDF2
- `DataProtection`: Contains the main implementation of DataProtection for ASP.NET Core to protect and unprotect data.
- `EntityFrameworkCore`: Contains the implementation for storing data using EntityFrameworkCore
- `Extensions`: Contains additional apis via extension methods.
- `StackExchangeRedis`: Contains the implementation for storing data using StackExchangeREdis
- `samples`: Contains a collection of sample apps
- `shared`: Contains a collection of shared constants and helper methods/classes

## Development Setup

### Build

To build this specific project from source, you can follow the instructions [on building a subset of the code](../../BuildFromSource.md#building-a-subset-of-the-code).

### Test

To run the tests for this project, you can [run the tests on the command line](../../BuildFromSource.md#running-tests-on-command-line) in this directory.

## More Information

For more information, see the [ASP.NET Core README](../../README.md).

## Community Maintained Data Protection Providers & Projects

 - [ASP.NET Core DataProtection for Service Fabric](https://github.com/MedAnd/AspNetCore.DataProtection.ServiceFabric)
