# Object Pool

This directory contains sources for [`Microsoft.Extensions.ObjectPool`](https://www.nuget.org/packages/Microsoft.Extensions.ObjectPool). This package provides types that enable object reuse. You can find additional information in the [ASP.NET Core Documentation](https://learn.microsoft.com/aspnet/core/performance/objectpool).

## Description

This project contains abstractions and default implementations for pooling objects. Commonly used types include:

`ObjectPool<T>` - This represents a pool of objects. This is used to get and return pooled objects.
`IPooledObjectPolicy<T>` - This policy defines how pooled objects are created and returned.
`ObjectPoolProvider` - This represents a provider of `ObjectPool<T>`. This is used to create object pools based on an `IPooledObjectPolicy<T>`.


For a full list of the types defined in this project, see [the namespace documentation](https://learn.microsoft.com/dotnet/api/microsoft.extensions.objectpool).

## Development Setup

### Build

To build this specific project from source, follow the instructions [on building the project](../../docs/BuildFromSource.md#step-3-build-the-repo).

### Test

To run the tests for this project, [run the tests on the command line](../../docs/BuildFromSource.md#running-tests-on-command-line) in this directory.

## More Information

For more information, see the [ASP.NET Core README](../../README.md).
