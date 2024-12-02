/*
 * For a detailed explanation regarding each configuration property, visit:
 * https://jestjs.io/docs/configuration
 */

path = require('path');

const ROOT_DIR = path.resolve(__dirname, '..', '..', '..');

/** @type {import('jest').Config} */

module.exports = {
  testEnvironment: 'node',
  roots: ['<rootDir>/src','<rootDir>/test'],
  testMatch: ['**/*.test.ts'],
  moduleFileExtensions: ['js', 'ts'],
  transform: {
    '^.+\\.(js|ts)$': 'babel-jest',
  },
  moduleDirectories: ['node_modules', 'src'],
  testEnvironment: "jsdom",
  reporters: [
      "default",
      [path.resolve(ROOT_DIR, "node_modules", "jest-junit", "index.js"), { "outputDirectory": path.resolve(ROOT_DIR, "artifacts", "log"), "outputName": `${process.platform}` + ".components-webjs.junit.xml" }]
  ],
}
