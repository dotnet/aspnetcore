## About

`Microsoft.AspNetCore.Components.WebAssembly.DevServer` is deprecated. Standalone Blazor WebAssembly apps should use `Microsoft.AspNetCore.Components.Gateway` instead.

## How to use

`Microsoft.AspNetCore.Components.WebAssembly.DevServer` is no longer the recommended package for hosting standalone Blazor WebAssembly applications. Use `Microsoft.AspNetCore.Components.Gateway` instead:

```shell
dotnet add package Microsoft.AspNetCore.Components.Gateway
```

If you still need to reference the legacy package for compatibility reasons, make sure that the `<PackageReference />` in the `.csproj` file includes `PrivateAssets="all"`:

```xml
<PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.DevServer" Version="..." PrivateAssets="all" />
```

## Additional Documentation

For additional documentation and examples, refer to the [official documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/debug#packages) on debugging Blazor WebAssembly applications.

## Feedback &amp; Contributing

`Microsoft.AspNetCore.Components.WebAssembly.DevServer` is released as open-source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/aspnetcore).
