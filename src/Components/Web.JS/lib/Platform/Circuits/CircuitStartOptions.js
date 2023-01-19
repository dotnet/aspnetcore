// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
import { LogLevel } from '../Logging/Logger';
export function resolveOptions(userOptions) {
    const result = { ...defaultOptions, ...userOptions };
    // The spread operator can't be used for a deep merge, so do the same for subproperties
    if (userOptions && userOptions.reconnectionOptions) {
        result.reconnectionOptions = { ...defaultOptions.reconnectionOptions, ...userOptions.reconnectionOptions };
    }
    return result;
}
const defaultOptions = {
    // eslint-disable-next-line @typescript-eslint/no-empty-function
    configureSignalR: (_) => { },
    logLevel: LogLevel.Warning,
    reconnectionOptions: {
        maxRetries: 8,
        retryIntervalMilliseconds: 20000,
        dialogId: 'components-reconnect-modal',
    },
};
//# sourceMappingURL=CircuitStartOptions.js.map