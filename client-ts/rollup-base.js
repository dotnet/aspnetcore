// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import path from 'path';

import resolve from 'rollup-plugin-node-resolve'
import sourceMaps from 'rollup-plugin-sourcemaps'
import commonjs from 'rollup-plugin-commonjs'

let polyfills = [ 'es6-promise', 'buffer', 'base64-js', 'ieee754' ];
let allowed_externals = [ ];

export default function(rootDir, moduleGlobals) {
    let pkg = require(path.join(rootDir, "package.json"));
    return {
        input: path.join(rootDir, "dist", "cjs", "browser-index.js"),
        output: {
            file: pkg.umd,
            format: "umd",
            name: pkg.umd_name,
            sourcemap: true,
            banner: "/* @license\r\n" +
                " * Copyright (c) .NET Foundation. All rights reserved.\r\n" +
                " * Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.\r\n" +
                " */",
            globals: moduleGlobals,
        },

        // Mark everything in the dependencies list as external
        external: Object.keys(pkg.dependencies || {}),

        plugins: [
            commonjs(),
            resolve({
                preferBuiltins: false
            }),
            sourceMaps()
        ]
    }
}