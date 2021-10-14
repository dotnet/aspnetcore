// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
const webpack = require('webpack');
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
    plugins: [
        new webpack.ProvidePlugin({
          process: 'process/browser',
        }),
      ],
    externals: {
        "@microsoft/signalr": "signalR",
        "@microsoft/signalr-protocol-msgpack": "signalR.protocols.msgpack",
    },
};