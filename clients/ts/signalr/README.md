JavaScript and TypeScript clients for SignalR for ASP.NET Core

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
