// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

const path = require("path");
const webpack = require("./common/node_modules/webpack");

module.exports = function (modulePath, browserBaseName, options) {
    const pkg = require(path.resolve(modulePath, "package.json"));

    options = options || {};

    return {
        entry: path.resolve(modulePath, "src", "browser-index.ts"),
        mode: "none",
        node: {
            global: true,
            process: false,
            Buffer: false,
        },
        target: options.target,
        resolveLoader: {
            // Special resolution rules for loaders (which are in the 'common' directory)
            modules: [ path.resolve(__dirname, "common", "node_modules") ],
        },
        module: {
            rules: [
                {
                    test: /\.ts$/,
                    use: [
                        {
                            loader: "ts-loader",
                            options: {
                                configFile: path.resolve(modulePath, "tsconfig.json"),
                            },
                        },
                    ],
                    exclude: /node_modules/,
                }
            ]
        },
        resolve: {
            extensions: [".ts", ".js"],
            alias: {
                ...options.alias,
            }
        },
        output: {
            filename: `${browserBaseName}.js`,
            path: path.resolve(modulePath, "dist", options.platformDist || "browser"),
            library: {
                root: pkg.umd_name.split("."),
                amd: pkg.umd_name,
            },
            libraryTarget: "umd",
        },
        plugins: [
            new webpack.SourceMapDevToolPlugin({
                filename: `${browserBaseName}.js.map`,
                moduleFilenameTemplate(info) {
                    let resourcePath = info.resourcePath;

                    // Clean up the source map urls.
                    while (resourcePath.startsWith("./") || resourcePath.startsWith("../")) {
                        if (resourcePath.startsWith("./")) {
                            resourcePath = resourcePath.substring(2);
                        } else {
                            resourcePath = resourcePath.substring(3);
                        }
                    }

                    // We embed the sources so we can falsify the URLs a little, they just
                    // need to be identifiers that can be viewed in the browser.
                    return `webpack://${pkg.umd_name}/${resourcePath}`;
                }
            }),
            // ES6 Promise uses this module in certain circumstances but we don't need it.
            new webpack.IgnorePlugin(/vertx/),
            new webpack.IgnorePlugin(/eventsource/),
        ],
        externals: options.externals,
    };
}