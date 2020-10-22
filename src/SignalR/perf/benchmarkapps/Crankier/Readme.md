# Crankier

Load testing for ASP.NET Core SignalR

## Commands

### server

The `server` command runs a web host exposing a single SignalR `Hub` endpoint on `/echo`.  After the first client connection, the server will periodically write concurrent connection information to the console.

```
> dotnet run -- help server

Usage:  server [options]

Options:
  --log <LOG_LEVEL>                                     The LogLevel to use.
  --azure-signalr-connectionstring <CONNECTION_STRING>  Azure SignalR Connection string to use
  
```

Notes:

* `LOG_LEVEL` switches internal logging only, not concurrent connection information, and defaults to `LogLevel.None`.  Use this option to control Kestrel / SignalR Warnings & Errors being logged to console.


### local

The `local` command launches a set of local worker clients to establish connections to your SignalR server.

```
> dotnet run -- help local

Usage:  local [options]

Options:
  --target-url <TARGET_URL>                   The URL to run the test against.
  --workers <WORKER_COUNT>                    The number of workers to use.
  --connections <CONNECTION_COUNT>            The number of connections per worker to use.
  --send-duration <SEND_DURATION_IN_SECONDS>  The send duration to use.
  --transport <TRANSPORT>                     The transport to use (defaults to WebSockets).
  --worker-debug                              Provide this switch to have the worker wait for the debugger.
```

Notes:

* `TARGET_URL` needs to be the route to your hub exposed by `UseSignalr` in your application.
* `CONNECTION_COUNT` defaults to 10,000.
* `SEND_DURATION_IN_SECONDS` defaults to 300
* `WORKER_COUNT` defaults to 1

#### Examples

Run the server:

```
dotnet run -- server
```

Run the server using Azure SignalR:

```
dotnet run -- server --azure-signalr-connectionstring Endpoint=https://your-url.service.signalr.net;AccessKey=yourAccessKey;Version=1.0;
```

Attempt to make 10,000 connections to the server using WebSockets and 10 workers:

```
dotnet run -- local --target-url https://localhost:5001/echo --workers 10
```

Attempt to make 5,000 connections to the server using Long Polling

```
dotnet run -- local --target-url https://localhost:5001/echo --connections 5000 --transport LongPolling
```
