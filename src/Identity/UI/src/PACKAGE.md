## About

`Microsoft.AspNetCore.Identity.UI` provides the default Razor Pages UI for the ASP.NET Core Identity framework.

## Key Features

* User registration and login functionality
* Account management
* Two-factor authentication

## How to Use

To use `Microsoft.AspNetCore.Identity.UI`, follow these steps:

### Installation

```sh
dotnet add package Microsoft.AspNetCore.Identity.UI
```

### Configuration

Add the following code to the `Program.cs` of your ASP.NET Core app:

```csharp
builder.Services.AddDefaultIdentity<IdentityUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
```

## Additional Documentation

For additional documentation and examples, refer to the [official ASP.NET Core Identity documentation](https://docs.microsoft.com/aspnet/core/security/authentication/identity).

## Feedback & Contributing

`Microsoft.AspNetCore.Identity.UI` is released as open-source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/aspnetcore).
