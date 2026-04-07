# MCP Server

This README was created using the C# MCP server project template.
It demonstrates how you can easily create an MCP server using C# and run it as an ASP.NET Core web application.

#### ---#if (SelfContained)
The MCP server is built as a self-contained application and does not require the .NET runtime to be installed on the target machine.
However, since it is self-contained, it must be built for each target platform separately.
By default, the template is configured to build for:
* `win-x64`
* `win-arm64`
* `osx-arm64`
* `linux-x64`
* `linux-arm64`
* `linux-musl-x64`

If you require more platforms to be supported, update the list of runtime identifiers in the project's `<RuntimeIdentifiers />` element.
#### ---#else
The MCP server is built as a framework-dependent application and requires the ASP.NET Core runtime to be installed on the target machine.
The application is configured to roll-forward to the next highest major version of the runtime if one is available on the target machine.
If an applicable .NET runtime is not available, the MCP server will not start.
Consider building the MCP server as a self-contained application if you want to avoid this dependency.
#### ---#endif

Please note that this template is currently in an early preview stage. If you have feedback, please take a [brief survey](http://aka.ms/dotnet-mcp-template-survey).

## Developing locally

To test this MCP server from source code (locally), you can configure your IDE to connect to the server using localhost.

#### ---#if (hostIdentifier == "vs")
```json
{
  "servers": {
    "McpServer-CSharp": {
      "type": "http",
      "url": "https://localhost:9995"
    }
  }
}
```
#### ---#else
```json
{
  "servers": {
    "McpServer-CSharp": {
      "type": "http",
      "url": "http://localhost:9996"
    }
  }
}
```
#### ---#endif

Refer to the VS Code or Visual Studio documentation for more information on configuring and using MCP servers:

- [Use MCP servers in VS Code (Preview)](https://code.visualstudio.com/docs/copilot/chat/mcp-servers)
- [Use MCP servers in Visual Studio (Preview)](https://learn.microsoft.com/visualstudio/ide/mcp-servers)

## Testing the MCP Server

Once configured, you can ask Copilot Chat for a random number, for example, `Give me 3 random numbers`. It should prompt you to use the `get_random_number` tool on the `McpServer-CSharp` MCP server and show you the results.

## Known issues

1. When using VS Code, connecting to `https://localhost:9995` fails.
  * This is related to using a self-signed developer certificate, even when the certificate is trusted by the system.
  * Connecting with `http://localhost:9996` succeeds.
  * See [Cannot connect to MCP server via SSE using trusted developer certificate (microsoft/vscode#248170)](https://github.com/microsoft/vscode/issues/248170) for more information.

## More information

ASP.NET Core MCP servers use the [ModelContextProtocol.AspNetCore](https://www.nuget.org/packages/ModelContextProtocol.AspNetCore) package from the MCP C# SDK. For more information about MCP:

- [Official Documentation](https://modelcontextprotocol.io/)
- [Protocol Specification](https://spec.modelcontextprotocol.io/)
- [GitHub Organization](https://github.com/modelcontextprotocol)
- [MCP C# SDK](https://modelcontextprotocol.github.io/csharp-sdk)
