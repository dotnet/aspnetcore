## Purpose

These projects assist in Benchmarking MVC.
They makes it easier to test local changes than having the App in the Benchmarks repo by letting us make changes in MVC branches and use the example commandline below to run the benchmarks against our branches.

## Usage

1. Push changes you would like to test to a branch on GitHub
2. Clone aspnet/benchmarks repo to your machine or install the global BenchmarksDriver tool https://www.nuget.org/packages/BenchmarksDriver/
3. If cloned go to the BenchmarksDriver project
4. Use the following command as a guideline for running a test using your changes

`benchmarks --server <server-endpoint> --client <client-endpoint>  -j https://raw.githubusercontent.com/aspnet/MVC/{your branch}/benchmarkaps/BasicApi/BasicApi.json`

5. For more info/commands see https://github.com/aspnet/benchmarks/blob/master/src/BenchmarksDriver/README.md
