// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

module.exports = {
    transformIgnorePatterns: [
        // We reference the ESM output from tests and don't want to run them through jest as it won't understand the syntax
        ".*/node_modules/(?!@microsoft)"
    ],
    globals: {
        "ts-jest": {
            "tsConfig": "./tsconfig.jest.json",
            "babelConfig": true,

            // Needed in order to properly process the JS files
            // We run 'tsc --noEmit' to get TS diagnostics before the test instead
            "diagnostics": false,
        }
    },
    reporters: [
        "default",
        ["../common/node_modules/jest-junit/index.js", { "output": "../../../../../artifacts/log/" + `${process.platform}` + ".node.functional.junit.xml" }]
    ],
    transform: {
        "^.+\\.(jsx?|tsx?)$": "../common/node_modules/ts-jest"
    },
    testEnvironment: "node",
    testRegex: "(Tests)\\.(jsx?|tsx?)$",
    moduleNameMapper: {
        "^ts-jest$": "<rootDir>/../common/node_modules/ts-jest",
        "^@microsoft/signalr$": "<rootDir>/../signalr/dist/cjs/index.js"
    },
    moduleFileExtensions: [
        "ts",
        "tsx",
        "js",
        "jsx",
        "json",
        "node"
    ]
};
