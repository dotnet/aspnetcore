## About

`Microsoft.AspNetCore.SignalR.Client.Core` provides core functionality for the .NET client for ASP.NET Core SignalR.

> [!NOTE]
> This package contains only the connection-agnostic components of the .NET SignalR client, and does not provide a default connection implementation. In most scenarios, the [`Microsoft.AspNetCore.SignalR.Client`](https://www.nuget.org/packages/Microsoft.AspNetCore.SignalR.Client) package should be used because it provides an HTTP connection implementation.

## Key Features

SignalR provides the following capabilities:
* Automatic connection management
* Sending messages to all connected clients simultaneously
* Sending messages to specific clients or groups of clients
* Scaling to handle increasing traffic

## How to Use

To use `Microsoft.AspNetCore.SignalR.Client.Core`, follow these steps:

### Installation

```shell
dotnet add package Microsoft.AspNetCore.SignalR.Client.Core
```

### Configuration

The .NET SignalR client requires a connection implementation. To use an HTTP connection implementation, install the  [`Microsoft.AspNetCore.SignalR.Client`](https://www.nuget.org/packages/Microsoft.AspNetCore.SignalR.Client) package.

## Main Types

The main types provided by `Microsoft.AspNetCore.SignalR.Client.Core` include:
* `HubConnectionBuilder`: Provides an abstraction to construct new SignalR hub connections
* `HubConnection`: Defines methods for managing a hub connection, including:
  * Starting and stopping the connection
  * Sending and receiving messages
  * Handling disconnects and attempting reconnects
* `HubConnectionOptions`: Provides options for configuring a `HubConnection`

## Additional Documentation

For additional documentation and examples, refer to the [official documentation](https://learn.microsoft.com/aspnet/core/signalr/dotnet-client) on the .NET client for ASP.NET Core SignalR.

## Feedback & Contributing

`Microsoft.AspNetCore.SignalR.Client.Core` is released as open-source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/aspnetcore).
