// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { ITransport, TransportType } from "../src/Transports"

export function eachTransport(action: (transport: TransportType) => void) {
    let transportTypes = [
        TransportType.WebSockets,
        TransportType.ServerSentEvents,
        TransportType.LongPolling ];
    transportTypes.forEach(t => action(t));
};

export function eachEndpointUrl(action: (givenUrl: string, expectedUrl: string) => void) {
    let urls = [
        [ "http://tempuri.org/endpoint/?q=my/Data", "http://tempuri.org/endpoint/negotiate?q=my/Data" ],
        [ "http://tempuri.org/endpoint?q=my/Data", "http://tempuri.org/endpoint/negotiate?q=my/Data" ],
        [ "http://tempuri.org/endpoint", "http://tempuri.org/endpoint/negotiate" ],
        [ "http://tempuri.org/endpoint/", "http://tempuri.org/endpoint/negotiate" ]
    ];

    urls.forEach(t => action(t[0], t[1]));
}