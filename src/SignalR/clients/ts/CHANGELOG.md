Change log contains changes for both @microsoft/signalr and @microsoft/signalr-protocol-msgpack.

## v10.0.0-rc.1

- Implemented fix for correctly reporting retries in the SignalR TS client. [#62812](https://github.com/dotnet/aspnetcore/pull/62812)
- Send Keep-Alive Ping Immediately When Previous Ping Is Overdue [#63195](https://github.com/dotnet/aspnetcore/pull/63195)

## v10.0.0-preview.1.25120.3
- Replaced deprecated `substr` usage with `substring` [#58732](https://github.com/dotnet/aspnetcore/pull/58732)
- Bumped `ws` dependency to fix component vulnerability [#57536](https://github.com/dotnet/aspnetcore/pull/57536)
- Bumped `webpack` from 5.93.0 to 5.94.0 [#57592](https://github.com/dotnet/aspnetcore/pull/57592)

## v9.0.0
- Bumped `ws` dependency to address security vulnerability [#58458](https://github.com/dotnet/aspnetcore/pull/58458)

## v9.0.0-rc.2.24474.3
- Added `Partitioned` flag to cookie for SignalR browser testing [#57997](https://github.com/dotnet/aspnetcore/pull/57997)

## v9.0.0-preview.7.24406.2
- Reverted split Node dependency workaround due to issues [#56766](https://github.com/dotnet/aspnetcore/pull/56766)

## v9.0.0-preview.1.24081.5
- Updated Karma config [#53247](https://github.com/dotnet/aspnetcore/pull/53247)
- Node.js and npm infrastructure improvements [#53154](https://github.com/dotnet/aspnetcore/pull/53154)
- Improved error handling in SignalR client: rejected promises in invocation messages [#52523](https://github.com/dotnet/aspnetcore/pull/52523)
- Reordered SignalR message parameters for better readability [#51559](https://github.com/dotnet/aspnetcore/pull/51559)

## v8.0.12
- Updated `serialize-javascript` dependency [#58466](https://github.com/dotnet/aspnetcore/pull/58466)

## v8.0.10
- Upgraded `ws` from 7 to 7.5.10 [#57411](https://github.com/dotnet/aspnetcore/pull/57411)

## v8.0.7
- Reverted incorrect handling of Node dependency splitting [#55229](https://github.com/dotnet/aspnetcore/pull/55229)
- Fixed error handling for rejected promises in incoming Invocation messages [#55230](https://github.com/dotnet/aspnetcore/pull/55230)

## v8.0.2
- Updated Karma config [#53411](https://github.com/dotnet/aspnetcore/pull/53411)

## v8.0.0-rc.2.23480.2
- Introduced **Stateful Reconnect** support in SignalR [#49940](https://github.com/dotnet/aspnetcore/pull/49940)
- Renamed internal `UseAck` to `UseStatefulReconnect` [#50407](https://github.com/dotnet/aspnetcore/pull/50407)
- Incremented HubProtocol version for Stateful Reconnect [#50442](https://github.com/dotnet/aspnetcore/pull/50442)

## v8.0.0-preview.7.23375.9
- Removed `__non_webpack_require__` workaround, improved Node dependency handling [#48154](https://github.com/dotnet/aspnetcore/pull/48154)

## v8.0.0-preview.6.23329.11
- SignalR client now sends `CloseMessage` to server [#48577](https://github.com/dotnet/aspnetcore/pull/48577)

## v8.0.0-preview.5.23302.2
- Fixed cookie handling with Fetch API on Node 18+ [#48076](https://github.com/dotnet/aspnetcore/pull/48076)

## v8.0.0-preview.4.23260.4
- Upgraded Webpack for SignalR builds [#47403](https://github.com/dotnet/aspnetcore/pull/47403)

## v8.0.0-preview.1.23112.2
- Fixed `CompletionMessage` handling for `false`/`null` result values [#45169](https://github.com/dotnet/aspnetcore/pull/45169)
- Enabled `ServerTimeout` and `KeepAliveInterval` options in `HubConnectionBuilder` [#46065](https://github.com/dotnet/aspnetcore/pull/46065)
- Migrated links to `learn.microsoft.com` [#46206](https://github.com/dotnet/aspnetcore/pull/46206)
