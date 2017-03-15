dotnet-user-secrets
===================

`dotnet-user-secrets` is a command line tool for managing the secrets in a user secret store.

### How To Install

Install `Microsoft.Extensions.SecretManager.Tools` as a `DotNetCliToolReference` to your project.

```xml
  <ItemGroup>
    <DotNetCliToolReference Include="Microsoft.Extensions.SecretManager.Tools" Version="1.0.0" />
  </ItemGroup>
```

### How To Use

Run `dotnet user-secrets --help` for more information about usage.
