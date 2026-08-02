## About

`Microsoft.AspNetCore.Owin` provides adapters for running OWIN middleware in an ASP.NET Core application, and to run ASP.NET Core middleware in an OWIN application.

## How to Use

To use `Microsoft.AspNetCore.Owin`, follow these steps:

### Installation

```shell
dotnet add package Microsoft.AspNetCore.Owin
```

### Configuration

To use OWIN middleware in an ASP.NET Core pipeline:
1. Define the OWIN middleware, if not done already. Here's a basic "Hello World" example:
    ```csharp
    public Task OwinHello(IDictionary<string, object> environment)
    {
        var responseText = "Hello World via OWIN";
        var responseBytes = Encoding.UTF8.GetBytes(responseText);

        // OWIN Environment Keys: https://owin.org/spec/spec/owin-1.0.0.html
        var responseStream = (Stream)environment["owin.ResponseBody"];
        var responseHeaders = (IDictionary<string, string[]>)environment["owin.ResponseHeaders"];

        responseHeaders["Content-Length"] = [responseBytes.Length.ToString(CultureInfo.InvariantCulture)];
        responseHeaders["Content-Type"] = ["text/plain"];

        return responseStream.WriteAsync(responseBytes, 0, responseBytes.Length);
    }
    ```
2. Add the middleware to the ASP.NET Core pipeline with the `UseOwin` extension method. For example:
    ```csharp
    app.UseOwin(pipeline =>
    {
        pipeline(next => OwinHello);
    });
    ```

## Additional Documentation

For additional documentation, including examples on running ASP.NET Core on an OWIN-based server, refer to the [official documentation](https://learn.microsoft.com/aspnet/core/fundamentals/owin) on OWIN with ASP.NET Core.

## Feedback &amp; Contributing

`Microsoft.AspNetCore.Owin` is released as open-source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/aspnetcore).
