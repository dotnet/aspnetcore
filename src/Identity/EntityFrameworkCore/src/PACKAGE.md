## About

`Microsoft.AspNetCore.Identity.EntityFrameworkCore` utilizes Entity Framework Core to provide functionality enabling the storage of user, role, and other identity-related data in a database.

## Key Features

* Provides user and role management
* Enables secure authentication and authorization mechanisms
* Allows storage and validatation of user passwords using hashing
* Tracks email confirmation for user account validation
* Tracks two-factor authentication to provide an extra layer of security
* Tracks failed login attempts to help protect against brute-force attacks enabling locking out user accounts after multiple failed login attempts
* Uses claims to define fine-grained access control policies
* Seamlessly integrates with Entity Framework Core for data storage and retrieval

## How to Use

To use `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, follow these steps:

### Installation

```sh
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
```

### Configuration

Add the following code to the `Program.cs` of your ASP.NET Core app:

```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<ApplicationUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
```

You can replace `ApplicationDbContext` with your own database context class derived from `IdentityDbContext` and `ApplicationUser` with your own user class containing additional properties derived from `IdentityUser`.

Make sure to [configure the connection string](https://learn.microsoft.com/ef/core/miscellaneous/connection-strings#aspnet-core) via "ConnectionStrings:DefaultConnection" (or whatever you rename it to) so it can connect to your database.

## Main Types

The main types provided by `Microsoft.AspNetCore.Identity.EntityFrameworkCore` include:

* `IdentityDbContext`: Provides the database context for storing and managing user, role, and other identity-related data
* `IdentityUserContext`: Provides methods and properties for querying and manipulating user information
* `RoleStore`: Provides methods for creating, updating, and deleting roles, as well as querying and managing role-related data
* `UserStore`: Provides methods for creating, updating, and deleting users, as well as querying and managing user-related data
* `UserOnlyStore`: Provides methods for creating, updating, and deleting users, as well as querying and managing user-related data for users without roles

## Additional Documentation

For additional documentation and examples, refer to the [official documentation](https://learn.microsoft.com/aspnet/core/security/authentication/identity) on ASP.NET Core Identity.

## Feedback & Contributing

`Microsoft.AspNetCore.Identity.EntityFrameworkCore` is released as open-source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/aspnetcore).
