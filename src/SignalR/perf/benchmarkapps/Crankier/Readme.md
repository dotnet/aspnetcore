# Crankier

Load testing for ASP.NET Core SignalR

## Commands

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

Attempt to make 10,000 connections to the `echo` hub using WebSockets and 10 workers:

```
dotnet run -- local --target-url https://localhost:5001/echo --workers 10
```

Attempt to make 5,000 connections to the `echo` hub using Long Polling

```
dotnet run -- local --target-url https://localhost:5001/echo --connections 5000 --transport LongPolling
```
