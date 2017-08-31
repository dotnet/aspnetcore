// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

"use strict";

var ECHOENDPOINT_URL = "http://" + document.location.host + "/echo";

function getTransportTypes() {
    var transportTypes = [signalR.TransportType.WebSockets];
    if (typeof EventSource !== "undefined") {
        transportTypes.push(signalR.TransportType.ServerSentEvents);
    }
    transportTypes.push(signalR.TransportType.LongPolling);

    return transportTypes;
}

function eachTransport(action) {
    getTransportTypes().forEach(function (t) {
        return action(t);
    });
}

function eachTransportAndProtocol(action) {
    var protocols = [new signalR.JsonHubProtocol(), new signalRMsgPack.MessagePackHubProtocol()];
    getTransportTypes().forEach(function (t) {
        return protocols.forEach(function (p) {
            return action(t, p);
        });
    });
}
