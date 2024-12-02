# Benchmarks

This document explains how to benchmark ASP.NET Core changes in the ASP.NET Core perf lab. Currently, only members of the .NET team have the permissions necessary to trigger performance runs on the official infrastructure.

It describes how to trigger benchmarks using the Crank CLI and the Pull-Request bot.

## Benchmarking local changes

This is the recommended method as it is quick, flexible and doesn't require a pre-existing pull-request.

### Install Crank

The Crank dotnet tool is required to start benchmarks remotely on the available performance infrastructure.

Install Crank with the following command:

```console
dotnet tool install Microsoft.Crank.Controller --version "0.2.0-*" --global
```

For more information about Crank please refer to the [Crank GitHub repository](https://github.com/dotnet/crank).

### Choose which benchmark to run

- In the [PowerBI dashboard](https://msit.powerbi.com/groups/b5743765-ec44-4dfc-91df-e32401023530/reports/10265790-7e2e-41d3-9388-86ab72be3fe9/ReportSection30725cd056a647733762?experience=power-bi) select a scenario that you would like to benchmark and the environment to use.
- At the bottom of the page there is a table that lists the command lines that were used to execute this benchmark. Pick one.
- Run the command in a shell. This will start the benchmark remotely on the same target machines as the charts.

This command line contains the specific version of the runtimes and SDK that were used, which makes it deterministic. This means that if you want to investigate a regression you can pick two points on a chart and re-run these to confirm the regression. You can also change each version to understand if the regression is coming from ASP.NET or the .NET Runtime.

### Use local changes

Using the same Crank command line you can also test the impact of local changes.

- Build your changes in `release` for the desired architecture/os (if it matters).
- Add `--application.options.outputFiles c:\build\release` and replace the folder by the one containing your changes.
- Run the updated Crank command line

## Benchmarking pull-requests

Use this technique when you want to benchmark a community contributed PR or when you don't have the changes available locally.

### Display all available benchmarks

[Here is an example](https://github.com/dotnet/aspnetcore/pull/50016#issuecomment-1677395856) of a PR that was benchmarked using this bot.

- In the pull-request, add `/benchmark` in the comments.
- A text similar to this will be displayed (it might take up to 10 minutes):

---
Crank Pull Request Bot

`/benchmark <benchmark[,...]> <profile[,...]> <component,[...]> <arguments>`

Benchmarks:
- `plaintext`: TechEmpower Plaintext Scenario - ASP.NET Platform implementation
- `json`: TechEmpower JSON Scenario - ASP.NET Platform implementation
- `fortunes`: TechEmpower Fortunes Scenario - ASP.NET Platform implementation
- `yarp`: YARP - http-http with 10 bytes
- `mvcjsoninput2k`: Sends 2Kb Json Body to an MVC controller

Profiles:
- `aspnet-perf-lin`: INTEL/Linux 12 Cores
- `aspnet-perf-win`: INTEL/Windows 12 Cores
- `aspnet-citrine-lin`: INTEL/Linux 28 Cores
- `aspnet-citrine-win`: INTEL/Windows 28 Cores
- `aspnet-citrine-ampere`: Ampere/Linux 80 Cores
- `aspnet-citrine-amd`: AMD/Linux 48 Cores

Components:
- `kestrel` (Microsoft.AspNetCore.Server.Kestrel `src\Servers\Kestrel\build.cmd`)
- `mvc` (Microsoft.AspNetCore.Mvc `src\Mvc\build.cmd`)
- `routing` (Microsoft.AspNetCore.Routing `src\Http\build.cmd`)

See [prbenchmarks.aspnetcore.config.yml](https://github.com/aspnet/Benchmarks/blob/2bddb9b43ffbc8cfdb4a2c3777d4e5213bc42332/build/prbenchmarks.aspnetcore.config.yml) for more details.

Arguments: any additional arguments to pass through to crank, e.g. `--variable name=value`

---

Where:
- *Benchmark* is the application that will be stressed.
- *Profile* is the environment where the application will run on.
- *Component* is the ASP.NET project under test that will be built and contains your changes.

> [!IMPORTANT]
> Any changes outside of the "component" projects will not be benchmarked.

Create a new comment with the correct benchmarks, profiles, and components to build. For instance:

```console
/benchmark json aspnet-citrine-lin kestrel
```

Once the benchmark has started (can take up to 10 minutes) the bot adds a comment to the PR. Then, when the benchmark is finished the results are added in a separate comment for both your changes and the source branch so you can compare the metrics you care about.

## Other scenarios

For more information please look the [Benchmarks repository](https://github.com/aspnet/benchmarks) where we maintain most of the benchmarks, and the [Crank repository](https://github.com/dotnet/crank).
