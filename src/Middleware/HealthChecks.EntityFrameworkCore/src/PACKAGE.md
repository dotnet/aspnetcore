## About

`Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` provides components for performing health checks using Entity Framework Core (EF Core).

## How to Use

To use `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`, follow these steps:

### Installation

```shell
dotnet add package Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore
```

### Configuration

To add a health check for an EF Core `DbContext`, use the `AddDbContextCheck` extension method to configure it with your app's service provider. Here's an example:

```csharp
builder.Services.AddDbContext<SampleDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<SampleDbContext>();
```

## Additional Documentation

For additional documentation and examples, refer to the [official documentation](https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks#entity-framework-core-dbcontext-probe) on using the Entity Framework Core `DbContext` probe.

## Feedback &amp; Contributing

`Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` is released as open-source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/aspnetcore).
