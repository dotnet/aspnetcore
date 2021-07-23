// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

const path = require("path");
const baseConfig = require("../webpack.config.base");

module.exports = baseConfig(__dirname, "signalr-protocol-msgpack", {
    externals: {
        "@microsoft/signalr": "signalR"
    }
});
