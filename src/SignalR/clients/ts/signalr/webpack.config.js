// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
const baseConfig = require("../webpack.config.base");
module.exports = env => baseConfig(__dirname, "signalr", {
    // These are only used in Node environments
    // so we tell webpack not to pull them in for the browser
    target: env && env.webworker ?  "webworker" : undefined,
    platformDist: env && env.webworker ?  "webworker" : undefined,
    externals: [
        "websocket",
        "eventsource",
        "request"
    ]
});