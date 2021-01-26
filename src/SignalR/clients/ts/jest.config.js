// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

module.exports = {
    globals: {
        "ts-jest": {
            "tsConfig": "./tsconfig.jest.json",
            "babelConfig": true,
            "diagnostics": true
        }
    },
    reporters: [
        "default",
        ["./common/node_modules/jest-junit/index.js", { "output": "../../../../artifacts/log/" + `${process.platform}` + ".signalr.junit.xml" }]
    ],
    transform: {
        "^.+\\.tsx?$": "./common/node_modules/ts-jest"
    },
    testEnvironment: "node",
    testRegex: "(/__tests__/.*|(\\.|/)(test|spec))\\.(jsx?|tsx?)$",
    moduleNameMapper: {
        "^ts-jest$": "<rootDir>/common/node_modules/ts-jest",
        "^@microsoft/signalr$": "<rootDir>/signalr/dist/cjs/index.js"
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
