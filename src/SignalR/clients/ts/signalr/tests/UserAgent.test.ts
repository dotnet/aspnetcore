// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { constructUserAgent } from "../src/Utils";

describe("User Agent", () => {
    [["1.0.0-build.10", "linux", "NodeJS", "10", "Microsoft SignalR/1.0.0 (1.0.0-build.10; Linux; NodeJS; 10)"]]
    .forEach(([version, os, runtime, runtimeVersion, expected]) => {
        it(`is in correct format`, async () => {
            const userAgent = constructUserAgent(version, os, runtime, runtimeVersion);
            expect(userAgent).toBe(expected);
        });
    });
});
