// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { asyncit as it } from "./Utils";
import { AbortController } from "../src/AbortController";

describe("AbortSignal", () => {
    describe("aborted", () => {
        it("is false on initialization", () => {
            expect(new AbortController().signal.aborted).toBe(false);
        });

        it("is true when aborted", () => {
            let controller = new AbortController();
            let signal = controller.signal;
            controller.abort();
            expect(signal.aborted).toBe(true);
        })
    });

    describe("onabort", () => {
        it("is called when abort is called", () => {
            let controller = new AbortController();
            let signal = controller.signal;
            let abortCalled = false;
            signal.onabort = () => abortCalled = true;
            controller.abort();
            expect(abortCalled).toBe(true);
        })
    })
});
