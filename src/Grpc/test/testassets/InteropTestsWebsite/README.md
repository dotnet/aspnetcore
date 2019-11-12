Running Grpc.Core interop client against Grpc.AspNetCore.Server interop server.
Context: https://github.com/grpc/grpc/blob/master/doc/interop-test-descriptions.md

## Start the InteropTestsWebsite

```
# From this directory
$ dotnet run
Now listening on: http://localhost:50052
```

## Build gRPC C# as a developer:
Follow https://github.com/grpc/grpc/tree/master/src/csharp
```
python tools/run_tests/run_tests.py -l csharp -c dbg --build_only
```

## Running the interop client

```
cd src/csharp/Grpc.IntegrationTesting.Client/bin/Debug/net45

mono Grpc.IntegrationTesting.Client.exe --server_host=localhost --server_port=50052 --test_case=large_unary
```

NOTE: Currently the some tests will fail because not all the features are implemented
by Grpc.AspNetCore.Server
