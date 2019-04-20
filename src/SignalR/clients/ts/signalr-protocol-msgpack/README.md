MsgPack support for SignalR for ASP.NET Core

## Installation

```bash
npm install @aspnet/signalr-protocol-msgpack
```

## Usage

See the [SignalR Documentation](https://docs.microsoft.com/en-us/aspnet/core/signalr) at docs.microsoft.com for documentation on the latest release. [API Reference Documentation](https://docs.microsoft.com/javascript/api/%40aspnet/signalr-protocol-msgpack/?view=signalr-js-latest) is also available on docs.microsoft.com.

### Browser

To use the client in a browser, copy `*.js` files from the `dist/browser` folder to your script folder include on your page using the `<script>` tag.

### NodeJS

To use the client in a NodeJS application, install the package to your `node_modules` folder and use `require('@aspnet/signalr-protocol-msgpack')` to load the module. The object returned by `require('@aspnet/signalr-protocol-msgpack')` has the same members as the global `signalR.protocols.msgpack` object (when used in a browser).

### Example (Browser)

```JavaScript
let connection = new signalR.HubConnectionBuilder()
    .withUrl("/chat")
    .withHubProtocol(new signalR.protocols.msgpack.MessagePackHubProtocol())
    .build();

connection.on('send', data => {
    console.log(data);
});

connection.start()
    .then(() => connection.invoke('send', 'Hello'));
```

### Example (NodeJS)

```JavaScript
const signalR = require("@aspnet/signalr");
const signalRMsgPack = require("@aspnet/signalr-protocol-msgpack");

let connection = new signalR.HubConnectionBuilder()
    .withUrl("/chat")
    .withHubProtocol(new signalRMsgPack.MessagePackHubProtocol())
    .build();

connection.on('send', data => {
    console.log(data);
});

connection.start()
    .then(() => connection.invoke('send', 'Hello'));
```
