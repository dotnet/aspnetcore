## About

`Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore` provides an ASP.NET Core middleware for EF Core error pages, allowing detection and diagnosis of errors with EF Core migrations.

## Key Features

* Captures and displays detailed error information from Entity Framework Core database operations
* Helps developers diagnose and troubleshoot database-related issues in ASP.NET Core applications

## How to Use

To use `Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore`, follow these steps:

### Installation

```shell
dotnet add package Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore
```

### Configuration

To use the middleware, add it to the ASP.NET Core pipeline defined in your app's `Program.cs`:

```csharp
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

var app = builder.build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
```

## Additional Documentation

For more information on using Entity Framework Core in ASP.NET Core applications, refer to the [official documentation](https://learn.microsoft.com/aspnet/core/data/ef-rp/intro).

## Feedback & Contributing

`Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore` is released as open-source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/aspnetcore).
