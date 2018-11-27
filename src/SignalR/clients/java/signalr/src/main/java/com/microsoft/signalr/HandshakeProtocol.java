// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import com.google.gson.Gson;

final class HandshakeProtocol {
    private static final Gson gson = new Gson();
    private static final String RECORD_SEPARATOR = "\u001e";

    public static String createHandshakeRequestMessage(HandshakeRequestMessage message) {
        // The handshake request is always in the JSON format
        return gson.toJson(message) + RECORD_SEPARATOR;
    }

    public static HandshakeResponseMessage parseHandshakeResponse(String message) {
        return gson.fromJson(message, HandshakeResponseMessage.class);
    }
}
