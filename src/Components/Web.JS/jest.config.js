// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
