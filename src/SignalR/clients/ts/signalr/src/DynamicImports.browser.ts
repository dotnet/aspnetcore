// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/** @private */
export function configureFetch(): boolean {
    return false;
}

/** @private */
export function configureAbortController(): boolean {
    return false;
}

/** @private */
export function getWS(): any {
    throw new Error("Trying to import 'ws' in the browser.");
}

/** @private */
export function getEventSource(): any {
    throw new Error("Trying to import 'eventsource' in the browser.");
}