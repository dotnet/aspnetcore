/*
 * For a detailed explanation regarding each configuration property, visit:
 * https://jestjs.io/docs/configuration
 */

path = require('path');

const ROOT_DIR = path.resolve(__dirname, '..', '..', '..');

module.exports = {
  coverageProvider: "v8",

  reporters: [
      "default",
      [path.resolve(ROOT_DIR, "node_modules", "jest-junit", "index.js"), { "outputDirectory": path.resolve(ROOT_DIR, "artifacts", "log"), "outputName": `${process.platform}` + ".components-webjs.junit.xml" }]
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
  testEnvironment: "jsdom",
  transform: {
    '^.+\\.tsx?$': ['@swc/jest'],
    '^.+\\.jsx?$': ['@swc/jest'],
  },
};
