// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import { ITransport, HttpTransportType } from "../src/ITransport";

export function eachTransport(action: (transport: HttpTransportType) => void) {
    const transportTypes = [
        HttpTransportType.WebSockets,
        HttpTransportType.ServerSentEvents,
        HttpTransportType.LongPolling ];
    transportTypes.forEach((t) => action(t));
}

export function eachEndpointUrl(action: (givenUrl: string, expectedUrl: string) => void) {
    const urls = [
        [ "http://tempuri.org/endpoint/?q=my/Data", "http://tempuri.org/endpoint/negotiate?q=my/Data" ],
        [ "http://tempuri.org/endpoint?q=my/Data", "http://tempuri.org/endpoint/negotiate?q=my/Data" ],
        [ "http://tempuri.org/endpoint", "http://tempuri.org/endpoint/negotiate" ],
        [ "http://tempuri.org/endpoint/", "http://tempuri.org/endpoint/negotiate" ],
    ];

    urls.forEach((t) => action(t[0], t[1]));
}
