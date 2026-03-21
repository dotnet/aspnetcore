## Description

Selenium is used in the aspnetcore repo for automated E2E integration testing.
Playwright is used in the aspnetcore repo for some benchmarking apps. We need to ensure the docker file and the package version match.

## Instructions

To update the Selenium and Playwright versions, these files need to be updated:

### Packages
  - [ ] [Selenium in `Versions.props`](eng/Versions.props) from NuGet:
    - [ ] [Selenium.WebDriver](https://www.nuget.org/packages/Selenium.WebDriver/) (Config variable `SeleniumWebDriverVersion`)
    - [ ] [Selenium.Support](https://www.nuget.org/packages/Selenium.Support/) (Config variable `SeleniumSupportVersion`)
  - [ ] Ensure Playwright versions match
    - [ ]  [Blazor Wasm benchmarks in `src/Components/benchmarkapps/Wasm.Performance/dockerfile`](src/Components/benchmarkapps/Wasm.Performance/dockerfile) (image starts with `mcr.microsoft.com`)
    - [ ]  [Playwright package version](eng/Versions.props) (Config variable `PlaywrightVersion`)

## Actions

Please, open the PR against `main` branch and include changes to the files listed above.

Also:
- mention @dotnet/aspnet-build in the opened Pull Request - this will be a responsible engineer for changes validation.
- add the `build-ops` label to the opened Pull Request
