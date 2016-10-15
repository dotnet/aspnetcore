dotnet-user-secrets
===================

`dotnet-user-secrets` is a command line tool for managing the secrets in a user secret store.

### How To Install

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

### How To Use

Run `dotnet user-secrets --help` for more information about usage.