# gRPC

This project area contains code to test [grpc-dotnet](https://github.com/grpc/grpc-dotnet) with the [gRPC interop tests](https://github.com/grpc/grpc/blob/master/doc/interop-test-descriptions.md).

The purpose of these tests is to ensure gRPC runs correctly against the latest changes in the .NET and ASP.NET runtimes.

## Description

* `test/testassets` contains gRPC client and server test apps. The client and server support executing the gRPC interop tests
* `test/InteropTests` builds the client and server test apps and executes them. There is a unit test for each interop test.

## Development Setup

### Build

Run `build.sh` or `build.cmd` in this directory.

### Test

Because `grpc-dotnet` is an external project, its packages use the installed runtime. The tests are run with a runtime built from the latest source code in CI builds.

## More Information

For more information, see the [ASP.NET Core README](../../README.md).
