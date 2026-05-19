// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copied from signalr/Utils.ts
/** @private */
export function isArrayBuffer(val: any): val is ArrayBuffer {
    return val && typeof ArrayBuffer !== "undefined" &&
        (val instanceof ArrayBuffer ||
        // Sometimes we get an ArrayBuffer that doesn't satisfy instanceof
        (val.constructor && val.constructor.name === "ArrayBuffer"));
}
