// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
const webpack = require('webpack');
const FileManagerPlugin = require('filemanager-webpack-plugin');
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
    resolve: {
        extensions: [".ts", ".js"]
    },
    externals: {
        "@microsoft/signalr": "signalR",
        "@microsoft/signalr-protocol-msgpack": "signalR.protocols.msgpack",
        "signalR": "signalR",
        "signalR.protocols.msgpack": "signalR.protocols.msgpack"
    },
    output: {
        filename: 'signalr-functional-tests.js',
        path: path.resolve(__dirname, "wwwroot", "dist"),
    },
    plugins: [
        new webpack.ProvidePlugin({
            process: 'process/browser',
        }),
        new FileManagerPlugin({
            events: {
                onEnd: {
                    copy: [
                        { source: path.resolve(__dirname, '../../../../../node_modules/jasmine-core/lib/jasmine-core/*.js'), destination: path.resolve(__dirname, 'wwwroot/lib/jasmine/') },
                        { source: path.resolve(__dirname, '../../../../../node_modules/jasmine-core/lib/jasmine-core/*.css'), destination: path.resolve(__dirname, 'wwwroot/lib/jasmine/') },
                        { source: path.resolve(__dirname, '../../../../../node_modules/@microsoft/signalr/dist/browser/*'), destination: path.resolve(__dirname, 'wwwroot/lib/signalr/') },
                        { source: path.resolve(__dirname, '../../../../../node_modules/@microsoft/signalr-protocol-msgpack/dist/browser/*'), destination: path.resolve(__dirname, 'wwwroot/lib/signalr/') },
                        { source: path.resolve(__dirname, '../../../../../node_modules/@microsoft/signalr/dist/webworker/'), destination: path.resolve(__dirname, 'wwwroot/lib/signalr-webworker/') },
                    ]
                }
            }
        })
    ]
};
