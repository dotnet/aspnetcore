MsgPack support for SignalR for ASP.NET Core

## Installation

```bash
npm install @aspnet/signalr-protocol-msgpack
```

## Usage

See the [SignalR Documentation](https://docs.microsoft.com/en-us/aspnet/core/signalr) at docs.microsoft.com for documentation on the latest release.

### Browser

To use the client in a browser, copy `*.js` files from the `dist/browser` folder to your script folder include on your page using the `<script>` tag.


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
