// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import java.nio.charset.StandardCharsets;
import java.nio.ByteBuffer;

import com.google.gson.Gson;

final class HandshakeProtocol {
    private static final Gson gson = new Gson();
    private static final String RECORD_SEPARATOR = "\u001e";

    public static ByteBuffer createHandshakeRequestMessage(HandshakeRequestMessage message) {
        // The handshake request is always in the JSON format
        return ByteBuffer.wrap((gson.toJson(message) + RECORD_SEPARATOR).getBytes(StandardCharsets.UTF_8));
    }

    public static HandshakeResponseMessage parseHandshakeResponse(String message) {
        return gson.fromJson(message, HandshakeResponseMessage.class);
    }
}
