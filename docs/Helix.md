Helix testing in ASP.NET Core
==============================

Helix is the distributed test platform that we use to run tests.  We build a helix payload that contains the publish directory of every test project that we want to test
send a job with with this payload to a set of queues for the various combinations of OS that we want to test
for example: `Windows.10.Amd64.ClientRS4.VS2017.Open`, `OSX.1012.Amd64.Open`, `Ubuntu.1804.Amd64.Open`. Helix takes care of unzipping, running the job, and reporting results.

For more info about helix see: [SDK](https://github.com/dotnet/arcade/blob/master/src/Microsoft.DotNet.Helix/Sdk/Readme.md), [JobSender](https://github.com/dotnet/arcade/blob/master/src/Microsoft.DotNet.Helix/Sdk/Readme.md)

## Running helix tests locally

To run Helix tests for one particular test project:

```
.\eng\scripts\RunHelix.ps1 -Project path\mytestproject.csproj
```

This will restore, and then publish all the test project including some bootstrapping scripts that will install the correct dotnet runtime/sdk before running the test assembly on the helix machine(s), and upload the job to helix.


## How do I look at the results of a helix run on Azure Pipelines?
There's a link embedded in the build.cmd log of the helix target on Azure Pipelines, near the bottom right that will look something like this:
```
2019-02-07T21:55:48.1516089Z   Results will be available from https://mc.dot.net/#/user/aspnetcore/pr~2Faspnet~2Faspnetcore/ci/20190207.34
2019-02-07T21:56:43.2209607Z   Job 0dedeef6-210e-4815-89f9-fd07513179fe is completed with 108 finished work items.
2019-02-07T21:56:43.5091018Z   Job 4c45a464-9464-4321-906c-2503320066b0 is completed with 108 finished work items.
2019-02-07T21:56:43.6863473Z   Job 91a826de-fd51-42c2-9e7e-84bbe18b16cf is completed with 108 finished work items.
2019-02-07T21:56:43.8591328Z   Job b3595ab8-049d-4775-9cea-d1140a0cb446 is completed with 108 finished work items.
2019-02-07T21:56:44.0384313Z   Job 2f174f2b-f6b1-4683-8303-3f120865c341 is completed with 108 finished work items.
2019-02-07T21:56:44.2069520Z   Job b9387311-e670-4e18-9c84-479b7bfe67d1 is completed with 111 finished work items.
2019-02-07T21:56:44.3946686Z   Job 43582e31-5648-47be-ac42-8a5e4129f15f is completed with 108 finished work items.
2019-02-07T21:56:44.5568847Z   Job 1e6b0051-21a4-4b75-93f3-f739bb71d5dc is completed with 108 finished work items.
2019-02-07T22:01:26.0028154Z   Job d597c581-f81b-446c-8daf-7c6511b526f7 is completed with 108 finished work items.
2019-02-07T22:06:33.6898567Z   Job 82f27d4c-9099-4f0e-b383-870c24d8dc2c is completed with 108 finished work items.
```

The link will take you to an overview of all the tests with clickable links to the logs and each run broken down by queue.

All of the helix runs for aspnetcore can be found here https://mc.dot.net/#/user/aspnetcore/builds

## What do I do if a test fails?
You can simulate how most tests run locally:
```
dotnet publish
cd <the publish directory>
dotnet vstest My.Tests.dll
```

If that doesn't help, you can try the Get Repro environment link from mission control and try to debug that way.

## Differences from running tests locally
Most tests that don't just work on helix automatically are ones that depend on the source code being accessible. The helix payloads only contain whatever is in the publish directories, so any thing else that test depends on will need to be included to the payload (TBD how to do this).

## How to skip tests on helix
There are two main ways to opt out of helix
- Skipping the entire test project via `<BuildHelixPayload>false</BuildHelixPayload>` in csproj (the default value for this is IsTestProject).
- Skipping an individual test via `[SkipOnHelix("url to github issue")]` which might require including a compile reference to: `<Compile Include="$(SharedSourceRoot)test\SkipOnHelixAttribute.cs" />`

Make sure to file an issue for any skipped tests and include that in a comment next to either of these
