## About

`Microsoft.AspNetCore.SignalR.Client` provides the .NET client for ASP.NET Core SignalR, which simplifies adding real-time web functionality to apps.

## Key Features

SignalR provides the following capabilities:
* Automatic connection management
* Sending messages to all connected clients simultaneously
* Sending messages to specific clients or groups of clients
* Scaling to handle increasing traffic

## How to Use

To use `Microsoft.AspNetCore.SignalR.Client`, follow these steps:

### Installation

```shell
dotnet add package Microsoft.AspNetCore.SignalR.Client
```

### Configuration

See the [ASP.NET Core SignalR docs](https://learn.microsoft.com/aspnet/core/signalr/hubs) for information about how to configure SignalR hubs on the server.

Then, you can configure the client to connect to the hub. For example:

```csharp
var connection = new HubConnectionBuilder()
    .WithUrl("http://localhost:53353/Chat")
    .Build();

connection.On("ReceiveMessage", (string user, string message) =>
{
    Console.WriteLine($"{user}: {message}");
});

await connection.StartAsync();
```

## Main Types

The main types provided by `Microsoft.AspNetCore.SignalR.Client` include:
* `HubConnectionBuilder`: Provides an abstraction to construct new SignalR hub connections
* `HubConnection`: Defines methods for managing a hub connection, including:
  * Starting and stopping the connection
  * Sending and receiving messages
  * Handling disconnects and attempting reconnects
* `HubConnectionOptions`: Provides options for configuring a `HubConnection`

## Additional Documentation

For additional documentation and examples, refer to the [official documentation](https://learn.microsoft.com/aspnet/core/signalr/dotnet-client) on the .NET client for ASP.NET Core SignalR.

## Feedback & Contributing

`Microsoft.AspNetCore.SignalR.Client` is released as open-source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/aspnetcore).
