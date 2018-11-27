// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Copied from signalr/Utils.ts
/** @private */
export function isArrayBuffer(val: any): val is ArrayBuffer {
    return val && typeof ArrayBuffer !== "undefined" &&
        (val instanceof ArrayBuffer ||
        // Sometimes we get an ArrayBuffer that doesn't satisfy instanceof
        (val.constructor && val.constructor.name === "ArrayBuffer"));
}
