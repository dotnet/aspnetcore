// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

module.exports = {
  globals: {
    "ts-jest": {
      "tsConfig": "./tests/tsconfig.json",
      "babeConfig": true,
      "diagnostics": true
    }
  },
  preset: 'ts-jest',
  testEnvironment: 'jsdom'
};
