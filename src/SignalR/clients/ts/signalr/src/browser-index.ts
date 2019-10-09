// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// This is where we add any polyfills we'll need for the browser. It is the entry module for browser-specific builds.

import "es6-promise/dist/es6-promise.auto.js";

// Copy from Array.prototype into Uint8Array to polyfill on IE. It's OK because the implementations of indexOf and slice use properties
// that exist on Uint8Array with the same name, and JavaScript is magic.
// We make them 'writable' because the Buffer polyfill messes with it as well.
if (!Uint8Array.prototype.indexOf) {
    Object.defineProperty(Uint8Array.prototype, "indexOf", {
        value: Array.prototype.indexOf,
        writable: true,
    });
}
if (!Uint8Array.prototype.slice) {
    Object.defineProperty(Uint8Array.prototype, "slice", {
        // wrap the slice in Uint8Array so it looks like a Uint8Array.slice call
        // tslint:disable-next-line:object-literal-shorthand
        value: function(start?: number, end?: number) { return new Uint8Array(Array.prototype.slice.call(this, start, end)); },
        writable: true,
    });
}
if (!Uint8Array.prototype.forEach) {
    Object.defineProperty(Uint8Array.prototype, "forEach", {
        value: Array.prototype.forEach,
        writable: true,
    });
}

export * from "./index";
