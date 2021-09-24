// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

module.exports = {
  globals: {
    transform: {
      '^.+\\.ts?$': 'ts-jest',
    },
    transformIgnorePatterns: ['Microsoft.JSInterop.js'],
    "ts-jest": {
      "tsConfig": "./tests/tsconfig.json",
      "babelConfig": true,
      "isolatedModules": true,
      "diagnostics": true
    }
  },
  preset: 'ts-jest',
  transform: {
    '^.+\\.(ts|tsx)?$': 'ts-jest',
    "^.+\\.(js|jsx)$": "babel-jest",
  },
  testEnvironment: 'jsdom'
};
