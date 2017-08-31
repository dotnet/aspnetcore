// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { ITransport, TransportType } from "../Microsoft.AspNetCore.SignalR.Client.TS/Transports"

export function eachTransport(action: (transport: TransportType) => void) {
    let transportTypes = [
        TransportType.WebSockets,
        TransportType.ServerSentEvents,
        TransportType.LongPolling ];
    transportTypes.forEach(t => action(t));
};