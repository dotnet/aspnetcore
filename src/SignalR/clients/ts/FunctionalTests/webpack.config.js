// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

const path = require("path");

module.exports = {
    entry: path.resolve(__dirname, "ts", "index.ts"),
    mode: "none",
    devtool: "source-map",
    module: {
        rules: [
            {
                test: /\.ts$/,
                use: [
                    {
                        loader: "ts-loader",
                        options: {
                            configFile: path.resolve(__dirname, "tsconfig.json"),
                        },
                    },
                ],
                exclude: /node_modules/,
            }
        ]
    },
    resolveLoader: {
        // Special resolution rules for loaders (which are in the 'common' directory)
        modules: [ path.resolve(__dirname, "..", "common", "node_modules") ],
    },
    resolve: {
        extensions: [".ts", ".js"]
    },
    output: {
        filename: 'signalr-functional-tests.js',
        path: path.resolve(__dirname, "wwwroot", "dist"),
    },
    externals: {
        "@microsoft/signalr": "signalR",
        "@microsoft/signalr-protocol-msgpack": "signalR.protocols.msgpack",
    },
};