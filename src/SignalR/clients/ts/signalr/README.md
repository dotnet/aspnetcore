JavaScript and TypeScript clients for SignalR for ASP.NET Core and Azure SignalR Service

## Installation

```bash
npm install @microsoft/signalr
# or
yarn add @microsoft/signalr
```

To try previews of the next version, use the `next` tag on NPM:

```bash
npm install @microsoft/signalr@next
# or
yarn add @microsoft/signalr@next
```

## Usage

See the [SignalR Documentation](https://learn.microsoft.com/aspnet/core/signalr) at learn.microsoft.com for documentation on the latest release. [API Reference Documentation](https://learn.microsoft.com/javascript/api/%40aspnet/signalr/?view=signalr-js-latest) is also available on learn.microsoft.com.

For documentation on using this client with Azure SignalR Service and Azure Functions, see the [SignalR Service serverless developer guide](https://learn.microsoft.com/azure/azure-signalr/signalr-concept-serverless-development-config).

### Browser

To use the client in a browser, copy `*.js` files from the `dist/browser` folder to your script folder include on your page using the `<script>` tag.

### WebWorker

To use the client in a webworker, copy `*.js` files from the `dist/webworker` folder to your script folder include on your webworker using the `importScripts` function. Note that webworker SignalR hub connection supports only absolute path to a SignalR hub.

### Node.js

To use the client in a NodeJS application, install the package to your `node_modules` folder and use `require('@microsoft/signalr')` to load the module. The object returned by `require('@microsoft/signalr')` has the same members as the global `signalR` object (when used in a browser).

### Example (Browser)

```javascript
let connection = new signalR.HubConnectionBuilder()
    .withUrl("/chat")
    .build();

connection.on("send", data => {
    console.log(data);
});

connection.start()
    .then(() => connection.invoke("send", "Hello"));
```

### Example (WebWorker)

```javascript
importScripts('signalr.js');

let connection = new signalR.HubConnectionBuilder()
    .withUrl("https://example.com/signalr/chat")
    .build();

connection.on("send", data => {
    console.log(data);
});

connection.start()
    .then(() => connection.invoke("send", "Hello"));

```

### Example (NodeJS)

```javascript
const signalR = require("@microsoft/signalr");

let connection = new signalR.HubConnectionBuilder()
    .withUrl("/chat")
    .build();

connection.on("send", data => {
    console.log(data);
});

connection.start()
    .then(() => connection.invoke("send", "Hello"));
```
