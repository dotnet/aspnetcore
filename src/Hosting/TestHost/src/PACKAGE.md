## About

`Microsoft.AspNetCore.TestHost` provides an ASP.NET Core web server for testing middleware in isolation.

## Key Features

* Instantiate an app pipeline containing only the components that you need to test
* Send custom requests to verify middleware behavior

## How to Use

To use `Microsoft.AspNetCore.TestHost`, follow these steps:

### Installation

```shell
dotnet add package Microsoft.AspNetCore.TestHost
```

### Usage

To set up the `TestServer`, configure it in your test project. Here's an example:

```csharp
[Fact]
public async Task MiddlewareTest_ReturnsNotFoundForRequest()
{
    // Build and start a host that uses TestServer
    using var host = await new HostBuilder()
        .ConfigureWebHost(builder =>
        {
            builder.UseTestServer()
                .ConfigureServices(services =>
                {
                    // Add any required services that the middleware uses
                    services.AddMyServices();
                })
                .Configure(app =>
                {
                    // Configure the processing pipeline to use the middleware
                    // for the test
                    app.UseMiddleware<MyMiddleware>();
                });
        })
        .StartAsync();

    var response = await host.GetTestClient().GetAsync("/");

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
}
```

## Main Types

The main types provided by this package are:

* `TestServer`: An `IServer` implementation for executing tests
* `TestServerOptions`: Provides options for configuring a `TestServer`

## Additional Documentation

For additional documentation and examples, refer to the [official documentation](https://learn.microsoft.com/aspnet/core/test/middleware) for testing middleware in ASP.NET Core.

## Feedback &amp; Contributing

`Microsoft.AspNetCore.TestHost` is released as open-source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/aspnetcore).
