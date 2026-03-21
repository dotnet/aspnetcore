/*
 * For a detailed explanation regarding each configuration property, visit:
 * https://jestjs.io/docs/configuration
 */

import path from "path";
import { fileURLToPath } from "url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const ROOT_DIR = path.resolve(__dirname, "..", "..", "..", "..");

/** @type {import('jest').Config} */

const config = {
    roots: ["<rootDir>/src", "<rootDir>/test"],
    testMatch: ["**/*.test.(ts|js)"],
    moduleFileExtensions: ["js", "ts"],
    transform: {
        "^.+\\.(js|ts)$": "babel-jest",
    },
    moduleDirectories: ["node_modules", "src"],
    testEnvironment: "jsdom",
    reporters: [
        "default",
        [
            path.resolve(ROOT_DIR, "node_modules", "jest-junit", "index.js"),
            { "outputDirectory": path.resolve(ROOT_DIR, "artifacts", "log"), "outputName": `${process.platform}` + ".jsinterop.junit.xml" }
        ]
    ],
};

export default config;