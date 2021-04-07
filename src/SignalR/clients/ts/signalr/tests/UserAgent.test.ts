// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
