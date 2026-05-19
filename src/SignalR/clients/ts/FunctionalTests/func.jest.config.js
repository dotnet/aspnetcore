// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

module.exports = {
        transformIgnorePatterns: [
        // We reference the ESM output from tests and don't want to run them through jest as it won't understand the syntax
        ".*/node_modules/(?!@microsoft)"
    ],
    reporters: [
        "default",
        ["jest-junit", { "outputDirectory": "../../../../../artifacts/log/", "outputName": `${process.platform}` + ".node.functional.junit.xml" }]
    ],
    testEnvironment: "node",
    testRegex: "(Tests)\\.(jsx?|tsx?)$",
    testRunner: "jest-jasmine2",
    moduleNameMapper: {
        "^ts-jest$": "ts-jest",
        "^@microsoft/signalr$": "<rootDir>../../../../../node_modules/@microsoft/signalr/dist/cjs/index.js"
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
