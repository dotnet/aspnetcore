// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import path from 'path';

import sourceMaps from 'rollup-plugin-sourcemaps'
import commonjs from 'rollup-plugin-commonjs'
import resolve from 'rollup-plugin-node-resolve'

export default {
    input: path.join(__dirname, "obj", "js", "index.js"),
    output: {
        file: path.join(__dirname, "wwwroot", "dist", "signalr-functional-tests.js"),
        format: "iife",
        sourcemap: true,
        banner: "/* @license\r\n" +
            " * Copyright (c) .NET Foundation. All rights reserved.\r\n" +
            " * Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.\r\n" +
            "*/",
        globals: {
            "@aspnet/signalr": "signalR",
            "@aspnet/signalr-protocol-msgpack": "signalR.protocols.msgpack",
        },
    },
    context: "window",
    external: [ "@aspnet/signalr", "@aspnet/signalr-protocol-msgpack" ],
    plugins: [
        commonjs(),
        resolve(),
        sourceMaps()
    ]
}