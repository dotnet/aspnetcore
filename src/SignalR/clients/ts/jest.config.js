// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

module.exports = {
    reporters: [
        "default",
        ["jest-junit", { "outputDirectory": "../../../../artifacts/log/", "outputName": `${process.platform}` + ".signalr.junit.xml" }]
    ],
    transform: {
        "^.+\\.tsx?$": [
            "ts-jest",
            {
                "tsconfig": "./tsconfig.jest.json",
                "babelConfig": true,
                "diagnostics": true
            }
        ]
    },
    testEnvironment: "node",
    testRegex: "(/__tests__/.*|(\\.|/)(test|spec))\\.(jsx?|tsx?)$",
    testRunner: "jest-jasmine2",
    moduleNameMapper: {
        "^ts-jest$": "ts-jest",
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
