dotnet-user-secrets
===================

`dotnet-user-secrets` is a command line tool for managing the secrets in a user secret store.

### How To Install

**project.json**
Add `Microsoft.Extensions.SecretManager.Tools` to the `tools` section of your `project.json` file:

```js
{
    ..
    "tools": {
        "Microsoft.Extensions.SecretManager.Tools": "1.0.0-*"
    }
    ...
}
```

**MSBuild**
Install `Microsoft.Extensions.SecretManager.Tools` as a `DotNetCliToolReference` to your project.

```xml
  <ItemGroup>
    <DotNetCliToolReference Include="Microsoft.Extensions.SecretManager.Tools" Version="1.0.0-msbuild1-final" />
  </ItemGroup>
```

### How To Use

Run `dotnet user-secrets --help` for more information about usage.