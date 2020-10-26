// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

const path = require("path");
const baseConfig = require("../webpack.config.base");

module.exports = baseConfig(__dirname, "signalr-protocol-msgpack", {
    externals: {
        msgpack5: "msgpack5",
        "@microsoft/signalr": "signalR"
    },
    alias: {
        buffer: path.resolve(__dirname, "node_modules", "buffer", "index.js"),
    },
});