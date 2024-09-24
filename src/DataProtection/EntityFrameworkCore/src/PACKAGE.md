## About

`Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` provides a mechanism for storing data protection keys to a database using Entity Framework Core.

## How to Use

To use `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore`, follow these steps:

### Installation

```shell
dotnet add package Microsoft.AspNetCore.DataProtection.EntityFrameworkCore
```

### Configuration

To store keys in a database, use the `PersistKeysToDbContext` extension method. For example:

```csharp
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<MyDbContext>();
```

Ensure that your DB context (`MyDbContext` in the above example) implements `IDataProtectionKeyContext`. For example:

```csharp
class MyDbContext : DbContext, IDataProtectionKeyContext
{
    public MyKeysContext(DbContextOptions<MyKeysContext> options)
        : base(options)
    {
    }

    // This maps to the table that stores keys
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
}
```

## Additional Documentation

For additional documentation and examples, refer to the [official documentation](https://learn.microsoft.com/aspnet/core/security/data-protection/implementation/key-storage-providers#entity-framework-core) on the Entity Framework Core key storage provider.

## Feedback &amp; Contributing

`Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` is released as open-source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/aspnetcore).
