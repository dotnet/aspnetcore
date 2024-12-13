## About

`Microsoft.AspNetCore.Components.WebAssembly.DevServer` provides a development server for use when building Blazor WebAssembly standalone applications.

## How to use

To use `Microsoft.AspNetCore.Components.WebAssembly.DevServer`, add the package to your project:

```shell
dotnet add package Microsoft.AspNetCore.Components.WebAssembly.DevServer
```

Make sure that the newly-added `<PackageReference />` in the `.csproj` file includes `PrivateAssets="all"`:

```xml
<PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.DevServer" Version="..." PrivateAssets="all" />
```

## Additional Documentation

For additional documentation and examples, refer to the [official documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/debug#packages) on debugging Blazor WebAssembly applications.

## Feedback &amp; Contributing

`Microsoft.AspNetCore.Components.WebAssembly.DevServer` is released as open-source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/aspnetcore).
