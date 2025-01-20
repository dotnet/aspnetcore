## About

`Microsoft.Extensions.Http.Polly` integrates `IHttpClientFactory` with the [Polly](https://github.com/App-vNext/Polly) library to provide comprehensive resilience and transient fault-handling. It allows developers to express policies such as Retry, Circuit Breaker, Timeout, Bulkhead Isolation, and Fallback in a fluent and thread-safe manner.

> [!NOTE]
> This package is deprecated. Please use either [`Microsoft.Extensions.Resilience`](https://www.nuget.org/packages/Microsoft.Extensions.Resilience) or [`Microsoft.Extensions.Http.Resilience`](https://www.nuget.org/packages/Microsoft.Extensions.Http.Resilience) instead.

## How to Use

To use `Microsoft.Extensions.Http.Polly`, follow these steps:

### Installation

```shell
dotnet add package Microsoft.Extensions.Http.Polly
```

### Usage

#### Handle transient faults

`AddTransientHttpErrorPolicy` can be used define a policy that handles transient errors:

```csharp
builder.Services.AddHttpClient("PollyWaitAndRetry")
    .AddTransientHttpErrorPolicy(policyBuilder =>
        policyBuilder.WaitAndRetryAsync(
            retryCount: 3,
            retryNumber => TimeSpan.FromMilliseconds(600)));
```

In the preceding example, failed requests are retried up to three times with a delay of 600 ms between attempts.

#### Dynamically select policies

To dynamically inspect a request and decide which policy apply, use the `AddPolicyHandler` extension method:

```csharp
var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
    TimeSpan.FromSeconds(10));
var longTimeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
    TimeSpan.FromSeconds(30));

builder.Services.AddHttpClient("PollyDynamic")
    .AddPolicyHandler(httpRequestMessage =>
        httpRequestMessage.Method == HttpMethod.Get
            ? timeoutPolicy
            : longTimeoutPolicy);
```

In this example, if the outgoing request is an HTTP GET, a 10-second timeout is applied. For any other HTTP method, a 30-second timeout is used.

## Main Types

The main types provided by this package are:

* `PollyHttpClientBuilderExtensions`: Provides extension methods for configuring `PolicyHttpMessageHandler` message handlers as part of an `HttpClient` message handler pipeline
* `PolicyHttpMessageHandler`: A `DelegatingHandler` implementation that executes request processing surrounded by a `Polly.Policy`
* `PollyServiceCollectionExtensions`: Provides convenience extension methods to register `Polly.Registry.IPolicyRegistry<string>` and `Polly.Registry.IReadOnlyPolicyRegistry<string>` in a service collection
* `HttpRequestMessageExtensions`: Provides extension methods for `HttpRequestMessage` Polly integration

## Additional Documentation

For additional documentation and examples, refer to the [official documentation](https://learn.microsoft.com/aspnet/core/fundamentals/http-requests?view#use-polly-based-handlers) on using Polly-based handlers in ASP.NET Core.

## Feedback &amp; Contributing

`Microsoft.Extensions.Http.Polly` is released as open-source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/aspnetcore).
