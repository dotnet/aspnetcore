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
            file: pkg.browser,
            format: "umd",
            name: pkg.umd_name,
            sourcemap: true,
            banner: "/* @license\r\n" +
                " * Copyright (c) .NET Foundation. All rights reserved.\r\n" +
                " * Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.\r\n" +
                "*/",
            globals: moduleGlobals,
        },
        external: function (m) {
            let match = m.match(/node_modules/);
            if (match) {
                let moduleName = m.substring(match.index + "node_modules".length + 1);
                let slashIndex = moduleName.indexOf('/');
                if(slashIndex < 0) {
                    slashIndex = moduleName.indexOf('\\');
                }
                moduleName = moduleName.substring(0, slashIndex);

                if(polyfills.indexOf(moduleName) >= 0) {
                    // This is a polyfill
                    console.log(`Importing polyfill: ${m}`);
                    return false;
                }
                else if(!allowed_externals.indexOf(moduleName)) {
                    console.log(`WARNING: External module '${m}' in use.`);
                }
                return true;
            }
            return false;
        },
        plugins: [
            commonjs(),
            resolve({
                preferBuiltins: false
            }),
            sourceMaps()
        ]
    }
}