## ASP.NET Core SignalR Typescript

Contains the client-side implementation of the SignalR protocol in TypeScript/JavaScript.

* `signalr/`: Contains the core functionality for connecting and interacting with a SignalR server. Shipped as `@microsoft/signalr` on npm.
* `signalr-protocol-msgpack/`: Contains an extension to the core library to use [msgpack](https://msgpack.org/) for it's core serialization logic instead of JSON. Shipped as `@microsoft/signalr-protocol-msgpack` on npm.

## Changelog

The changelog is located at [`CHANGELOG.md`](CHANGELOG.md). It is manually updated when changes are made to either of the npm packages we ship.

The CI should fail if changes are made to the libraries without any CHANGELOG updates. If the change isn't relevant to the CHANGELOG, add `[no changelog]` to one of the commit messages in the PR.