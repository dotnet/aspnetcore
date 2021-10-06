# Using Build Results

Typically, you'll want to validate changes made in the repo by writing unit tests, integration tests, and manually verifying in the sample app. There are some scenarios where it is also helpful to validate changes in larger, more complex apps.

For these scenarios, you can build development versions of the packages shipped from the repo then consume those development versions in your test app.

To start, build the packages with your changes locally on your repo.

```
.\build.cmd -all -pack -arch x64
```

The packages are built into the `aspnetcore\artifacts\packages\Debug\Shipping` directory within the repo. 

Within your test app, creating a `nuget.config` file if it doesn't exist already and add the following package source to the config.

```
<?xml version="1.0" encoding="utf-8"?>
<configuration>
    <packageSources>
        <clear />
        <add key="MyBuildOfAspNetCore" value="C:\src\aspnet\AspNetCore\artifacts\packages\Debug\Shipping\" />
        <add key="NuGet.org" value="https://api.nuget.org/v3/index.json" />
    </packageSources>
</configuration>
```

In the project files for your repo, update the package versions to target the development version. For example, if you want to validate changes to the `Microsoft.AspNetCore.Components.WebAssembly` package. You can make the following changes to the PackageReference.

```diff
- <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="5.0.0" />
+ <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="7.0.0-dev" />
```

