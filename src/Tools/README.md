# DotNetTools

## Bundled tools

The folder contains command-line tools for ASP.NET Core. The following tools are bundled* in the .NET Core CLI. Follow the links below for more details on each tool.

- [dotnet-watch](dotnet-watch/README.md)
- [dotnet-user-secrets](dotnet-user-secrets/README.md)
- [dotnet-sql-cache](dotnet-sql-cache/README.md)
- [dotnet-dev-certs](dotnet-dev-certs/README.md)

*\*This applies to .NET Core CLI 2.1.300-preview2 and up. For earlier versions of the CLI, these tools must be installed separately.*

*For 2.0 CLI and earlier, see <https://github.com/aspnet/DotNetTools/tree/rel/2.0.0/README.md> for details.*

## Non-bundled tools

The following tools are produced by us but not bundled in the .NET Core CLI. They must be aquired independently.

- [Microsoft.dotnet-openapi](Microsoft.dotnet-openapi/README.md)

This folder also contains the infrastructure for our partners' service reference features:

- [Extensions.ApiDescription.Client](Extensions.ApiDescription.Client/README.md) MSBuild glue for OpenAPI code generation.
- [Extensions.ApiDescription.Server](Extensions.ApiDescription.Server/README.md) MSBuild glue for OpenAPI document generation.
- [dotnet-getdocument](dotnet-getdocument/README.md) the outside man of OpenAPI document generation tool.
- [GetDocument.Insider](GetDocumentInsider/README.md) the inside man of OpenAPI document generation tool.

## Usage

The command line tools can be invoked as a subcommand of `dotnet`.

```sh
dotnet watch
dotnet user-secrets
dotnet sql-cache
dotnet dev-certs
dotnet openapi
```

Add `--help` to see more details. For example,

```sh
dotnet watch --help
```
