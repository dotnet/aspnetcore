# Components End-To-End (E2E) Testing

This directory contains the E2E tests for `aspnetcore Components`, as well as the associated `testassets`. 

Currently we leverage [Selenium](https://www.selenium.dev/selenium/docs/api/dotnet/) to execute the tests (`E2ETest`), however we have a prototype implementation with [Playright](https://playwright.dev/dotnet/) (`E2ETestMigration`).

#### Running the E2E Tests

1. `cd E2ETest`
2. `yarn install`
3. `dotnet test .\Microsoft.AspNetCore.Components.E2ETests.csproj` (you can filter by test type using `--filter [TestName]`)

### Adding a new E2E Test
1. Update or create a new component in `testassets/BasicTestApp`
2. Update or add a new test in `E2ETest/Tests`
