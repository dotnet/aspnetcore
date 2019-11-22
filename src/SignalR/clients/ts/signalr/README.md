JavaScript and TypeScript clients for SignalR for ASP.NET Core

## Installation

```bash
npm install @microsoft/signalr
```
or
```bash
yarn add @microsoft/signalr
```

## Usage

See the [SignalR Documentation](https://docs.microsoft.com/en-us/aspnet/core/signalr) at docs.microsoft.com for documentation on the latest release. [API Reference Documentation](https://docs.microsoft.com/javascript/api/%40aspnet/signalr/?view=signalr-js-latest) is also available on docs.microsoft.com.

### Browser

To use the client in a browser, copy `*.js` files from the `dist/browser` folder to your script folder include on your page using the `<script>` tag.

### WebWorker

To use the client in a webworker, copy `*.js` files from the `dist/webworker` folder to your script folder include on your webworker using the `importScripts` function. Note that webworker SignalR hub connection supports only absolute path to a SignalR hub.

### Node.js

To use the client in a NodeJS application, install the package to your `node_modules` folder and use `require('@microsoft/signalr')` to load the module. The object returned by `require('@microsoft/signalr')` has the same members as the global `signalR` object (when used in a browser).

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

### Example (WebWorker)


```JavaScript
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

```JavaScript
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
