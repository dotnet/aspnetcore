## About

`Microsoft.AspNetCore.DataProtection.StackExchangeRedis` allows storing data protection keys in a Redis cache.

> [!WARNING]
> Only Redis versions supporting [Redis Data Persistence](https://learn.microsoft.com/azure/azure-cache-for-redis/cache-how-to-premium-persistence) should be used to store keys. [Azure Blob storage](https://learn.microsoft.com/azure/storage/blobs/storage-blobs-introduction) is persistent and can be used to store keys. For more information, see [this GitHub issue](https://github.com/dotnet/AspNetCore/issues/13476).

## How to Use

To use `Microsoft.AspNetCore.DataProtection.StackExchangeRedis`, follow these steps:

### Installation

```shell
dotnet add package Microsoft.AspNetCore.DataProtection.StackExchangeRedis
```

### Configuration

To configure data protection key storage on Redis, call one of the `PersistKeysToStackExchangeRedis` overloads:

```csharp
var redis = ConnectionMultiplexer.Connect("<URI>");
builder.Services.AddDataProtection()
    .PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys");
```

## Additional Documentation

For additional documentation and examples, refer to the [official documentation](https://learn.microsoft.com/aspnet/core/security/data-protection/implementation/key-storage-providers#redis) on the Redis key storage provider.

## Feedback &amp; Contributing

`Microsoft.AspNetCore.DataProtection.StackExchangeRedis` is released as open-source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/aspnetcore).
