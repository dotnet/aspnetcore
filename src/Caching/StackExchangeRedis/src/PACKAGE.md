## About

`Microsoft.Extensions.Caching.StackExchangeRedis` provides a distributed cache implementation of `Microsoft.Extensions.Caching.Distributed.IDistributedCache` using Redis.

## How to Use

To use `Microsoft.Extensions.Caching.StackExchangeRedis`, follow these steps:

### Installation

```shell
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis
```

### Configuration

To configure the Redis cache in your app, use the `AddStackExchangeRedisCache` extension method. Here's an example:

```csharp
var builder = WebApplication.CreateBuilder();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("MyRedisConStr");
    options.InstanceName = "MyCache";
});
```

## Main Types

* `RedisCache`: Provides a distributed cache implementation using Redis
* `RedisCacheOptions`: Provides options used for configuring a `RedisCache`

## Additional Documentation

For additional documentation and examples, refer to the [official documentation](https://learn.microsoft.com/aspnet/core/performance/caching/distributed#distributed-redis-cache) on using the Distributed Redis Cache in ASP.NET Core.

## Feedback &amp; Contributing

`Microsoft.Extensions.Caching.StackExchangeRedis` is released as open-source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/aspnetcore).
