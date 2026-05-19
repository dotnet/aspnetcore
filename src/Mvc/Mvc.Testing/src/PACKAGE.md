## About

`Microsoft.AspNetCore.Mvc.Testing` provides support for writing integration tests for ASP.NET Core apps that utilize MVC or Minimal APIs.

## Key Features

* Copies the dependencies file (`.deps.json`) from the System Under Test (SUT) into the test project's `bin` directory
* Sets the [content root](https://learn.microsoft.com/aspnet/core/fundamentals/#content-root) to the SUT's project root so that static files are found during test execution
* Provides the [`WebApplicationFactory`](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.mvc.testing.webapplicationfactory-1) class to streamline bootstrapping the SUT with [`TestServer`](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.testhost.testserver)

## How to Use

To use `Microsoft.AspNetCore.Mvc.Testing`, follow these steps:

### Installation

To install the package, run the following command from the directory containing the test project file:

```shell
dotnet add package Microsoft.AspNetCore.Mvc.Testing
```

### Configuration

To configure the test app, follow these steps:

1. Specify the Web SDK in the test project file (`<Project Sdk="Microsoft.NET.Sdk.Web">`).
2. Add references to the following packages:
    * `xunit`
    * `xunit.runner.visualstudio`
    * `Microsoft.NET.Test.Sdk`
3. Add a test class to the test project:
    ```csharp
    public class BasicTests
        : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public BasicTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData("/")]
        [InlineData("/Index")]
        [InlineData("/About")]
        [InlineData("/Privacy")]
        [InlineData("/Contact")]
        public async Task Get_EndpointsReturnSuccessAndCorrectContentType(string url)
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync(url);

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            Assert.Equal("text/html; charset=utf-8",
                response.Content.Headers.ContentType.ToString());
        }
    }
    ```

## Additional Documentation

For additional documentation and examples, refer to the [official documentation](https://learn.microsoft.com/aspnet/core/test/integration-tests) on integration testing in ASP.NET Core.

## Feedback &amp; Contributing

`Microsoft.AspNetCore.Mvc.Testing` is released as open-source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/aspnetcore).
