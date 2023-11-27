// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import * as _fs from "fs";
import * as path from "path";
import { promisify } from "util";

const fs = {
    stat: promisify(_fs.stat),
};

// Regression tests to make sure we are building the .min and non .min js files differently.
// It's ok to have to modify these values as long as we know why they changed.

describe("Output files", () => {
    it(".min.js file is small", async () => {
        const size = await (await fs.stat(path.resolve(__dirname, "..", "dist/browser/signalr.min.js"))).size;
        expect(size).toBeLessThan(48000);
    });

    it("non .min.js file is big", async () => {
        const size = await (await fs.stat(path.resolve(__dirname, "..", "dist/browser/signalr.js"))).size;
        expect(size).toBeGreaterThan(120000);
    });
});