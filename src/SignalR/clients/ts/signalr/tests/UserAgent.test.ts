// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { constructUserAgent } from "../src/Utils";

describe("User Agent", () => {
    [["1.0.4-build.10", "Linux", "NodeJS", "10", "Microsoft SignalR/1.0 (1.0.4-build.10; Linux; NodeJS; 10)"],
     ["1.4.7-build.10", "", "Browser", "", "Microsoft SignalR/1.4 (1.4.7-build.10; Unknown OS; Browser; Unknown Runtime Version)"],
     ["3.1.1-build.10", "macOS", "Browser", "", "Microsoft SignalR/3.1 (3.1.1-build.10; macOS; Browser; Unknown Runtime Version)"],
     ["3.1.3-build.10", "", "Browser", "4", "Microsoft SignalR/3.1 (3.1.3-build.10; Unknown OS; Browser; 4)"]]
    .forEach(([version, os, runtime, runtimeVersion, expected]) => {
        it(`is in correct format`, async () => {
            const userAgent = constructUserAgent(version, os, runtime, runtimeVersion);
            expect(userAgent).toBe(expected);
        });
    });
});
