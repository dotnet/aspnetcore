## About

`Microsoft.AspNetCore.Components.QuickGrid.EntityFrameworkAdapter` provides an Entity Framework Core (EF Core) adapter for the [`Microsoft.AspNetCore.Components.QuickGrid`](https://www.nuget.org/packages/Microsoft.AspNetCore.Components.QuickGrid) package.

## How to Use

To use `Microsoft.AspNetCore.Components.QuickGrid.EntityFrameworkAdapter`, follow these steps:

### Installation

```shell
dotnet add package Microsoft.AspNetCore.Components.QuickGrid.EntityFrameworkAdapter
```

### Configuration

To register an EF-aware `IAsyncQueryExecutor` implementation, call `AddQuickGridEntityFrameworkAdapter` on the service collection in `Program.cs`:

```csharp
builder.Services.AddQuickGridEntityFrameworkAdapter();
```

## Additional Documentation

For additional documentation and examples, refer to the [official documentation](https://learn.microsoft.com/aspnet/core/blazor/components/quickgrid#entity-framework-core-ef-core-data-source) on using EF Core with `QuickGrid`.

## Feedback &amp; Contributing

`Microsoft.AspNetCore.Components.QuickGrid.EntityFrameworkAdapter` is released as open-source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/aspnetcore).
