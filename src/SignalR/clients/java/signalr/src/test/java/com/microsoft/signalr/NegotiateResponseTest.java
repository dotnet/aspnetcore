// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import static org.junit.jupiter.api.Assertions.*;

import java.io.StringReader;

import org.junit.jupiter.api.Test;

import com.google.gson.stream.JsonReader;


class NegotiateResponseTest {
    @Test
    public void VerifyNegotiateResponse() {
        String stringNegotiateResponse = "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\"," +
                "\"negotiateVersion\": 99, \"connectionToken\":\"connection-token-value\"," +
                "\"availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}," +
                "{\"transport\":\"ServerSentEvents\",\"transferFormats\":[\"Text\"]}," +
                "{\"transport\":\"LongPolling\",\"transferFormats\":[\"Text\",\"Binary\"]}]}";
        NegotiateResponse negotiateResponse = new NegotiateResponse(new JsonReader(new StringReader(stringNegotiateResponse)));
        assertTrue(negotiateResponse.getAvailableTransports().contains("WebSockets"));
        assertTrue(negotiateResponse.getAvailableTransports().contains("ServerSentEvents"));
        assertTrue(negotiateResponse.getAvailableTransports().contains("LongPolling"));
        assertNull(negotiateResponse.getAccessToken());
        assertNull(negotiateResponse.getRedirectUrl());
        assertEquals("bVOiRPG8-6YiJ6d7ZcTOVQ", negotiateResponse.getConnectionId());
        assertEquals("connection-token-value", negotiateResponse.getConnectionToken());
        assertEquals(99, negotiateResponse.getVersion());
    }

    @Test
    public void VerifyRedirectNegotiateResponse() {
        String stringNegotiateResponse = "{\"url\":\"www.example.com\"," +
                "\"accessToken\":\"some_access_token\"," +
                "\"availableTransports\":[]}";
        NegotiateResponse negotiateResponse = new NegotiateResponse(new JsonReader(new StringReader(stringNegotiateResponse)));
        assertTrue(negotiateResponse.getAvailableTransports().isEmpty());
        assertNull(negotiateResponse.getConnectionId());
        assertEquals("some_access_token", negotiateResponse.getAccessToken());
        assertEquals("www.example.com", negotiateResponse.getRedirectUrl());
        assertNull(negotiateResponse.getConnectionId());
    }

    @Test
    public void NegotiateResponseIgnoresExtraProperties() {
        String stringNegotiateResponse = "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\"," +
                "\"extra\":\"something\"}";
        NegotiateResponse negotiateResponse = new NegotiateResponse(new JsonReader(new StringReader(stringNegotiateResponse)));
        assertEquals("bVOiRPG8-6YiJ6d7ZcTOVQ", negotiateResponse.getConnectionId());
    }

    @Test
    public void NegotiateResponseIgnoresExtraComplexProperties() {
        String stringNegotiateResponse = "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\"," +
                "\"extra\":[\"something\"]}";
        NegotiateResponse negotiateResponse = new NegotiateResponse(new JsonReader(new StringReader(stringNegotiateResponse)));
        assertEquals("bVOiRPG8-6YiJ6d7ZcTOVQ", negotiateResponse.getConnectionId());
    }

    @Test
    public void NegotiateResponseWithNegotiateVersion() {
        String stringNegotiateResponse = "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\"," +
                "\"negotiateVersion\": 99}";
        NegotiateResponse negotiateResponse = new NegotiateResponse(new JsonReader(new StringReader(stringNegotiateResponse)));
        assertEquals("bVOiRPG8-6YiJ6d7ZcTOVQ", negotiateResponse.getConnectionId());
        assertEquals(99, negotiateResponse.getVersion());
    }

    @Test
    public void NegotiateResponseWithConnectionToken() {
        String stringNegotiateResponse = "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\"," +
                "\"negotiateVersion\": 99, \"connectionToken\":\"connection-token-value\"}";
        NegotiateResponse negotiateResponse = new NegotiateResponse(new JsonReader(new StringReader(stringNegotiateResponse)));
        assertEquals("bVOiRPG8-6YiJ6d7ZcTOVQ", negotiateResponse.getConnectionId());
        assertEquals("connection-token-value", negotiateResponse.getConnectionToken());
        assertEquals(99, negotiateResponse.getVersion());
    }
}
