JavaScript and TypeScript clients for SignalR for ASP.NET Core

> :warning: This package will no longer be supported starting August 21, 2021. This package follows the [.NET Core 2.1 support lifecycle](https://dotnet.microsoft.com/platform/support/policy/dotnet-core) which is ending soon. Please transition to using [@microsoft/signalr](https://www.npmjs.com/package/@microsoft/signalr) as described below.

> Note: The JavaScript and TypeScript clients for SignalR for ASP.NET Core have been moved to [@microsoft/signalr](https://www.npmjs.com/package/@microsoft/signalr). If you are already using `@aspnet/signalr` and are unsure when to move to `@microsoft/signalr`, check the [Feature Distribution](https://docs.microsoft.com/en-us/aspnet/core/signalr/client-features) chart in the ASP.NET Core SignalR documentation. Newer client releases are compatible with older version of ASP.NET Core SignalR which means it is safe to upgrade the client before upgrading the server.

## Installation

```bash
npm install @aspnet/signalr
```

## Usage

See the [SignalR Documentation](https://docs.microsoft.com/en-us/aspnet/core/signalr) at docs.microsoft.com for documentation on the latest release.

### Browser

To use the client in a browser, copy `*.js` files from the `dist/browser` folder to your script folder include on your page using the `<script>` tag.

### Node.js

The following polyfills are required to use the client in Node.js applications:
- `XmlHttpRequest` - always
- `WebSockets` - to use the WebSockets transport
- `EventSource` - to use the ServerSentEvents transport
- `btoa/atob` - to use binary protocols (e.g. MessagePack) over text transports (ServerSentEvents)

### Example (Browser)

```JavaScript
let connection = new signalR.HubConnectionBuilder()
    .withUrl("/chat")
    .build();

connection.on("send", data => {
    console.log(data);
});

connection.start()
    .then(() => connection.invoke("send", "Hello"));
```

### Example (NodeJS)

```JavaScript
const signalR = require("@aspnet/signalr");

let connection = new signalR.HubConnectionBuilder()
    .withUrl("/chat")
    .build();

connection.on("send", data => {
    console.log(data);
});

connection.start()
    .then(() => connection.invoke("send", "Hello"));
```
