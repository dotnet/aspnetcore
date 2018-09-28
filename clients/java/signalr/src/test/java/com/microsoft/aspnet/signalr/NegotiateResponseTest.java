// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.aspnet.signalr;

import static org.junit.jupiter.api.Assertions.*;

import java.io.IOException;

import org.junit.jupiter.api.Test;


class NegotiateResponseTest {
    @Test
    public void VerifyNegotiateResponse() throws IOException {
        String stringNegotiateResponse = "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\",\"" +
                "availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}," +
                "{\"transport\":\"ServerSentEvents\",\"transferFormats\":[\"Text\"]}," +
                "{\"transport\":\"LongPolling\",\"transferFormats\":[\"Text\",\"Binary\"]}]}";
        NegotiateResponse negotiateResponse = new NegotiateResponse(stringNegotiateResponse);
        assertTrue(negotiateResponse.getAvailableTransports().contains("WebSockets"));
        assertTrue(negotiateResponse.getAvailableTransports().contains("ServerSentEvents"));
        assertTrue(negotiateResponse.getAvailableTransports().contains("LongPolling"));
        assertNull(negotiateResponse.getAccessToken());
        assertNull(negotiateResponse.getRedirectUrl());
        assertEquals("bVOiRPG8-6YiJ6d7ZcTOVQ", negotiateResponse.getConnectionId());
    }

    @Test
    public void VerifyRedirectNegotiateResponse() throws IOException {
        String stringNegotiateResponse = "{\"url\":\"www.example.com\"," +
                "\"accessToken\":\"some_access_token\"," +
                "\"availableTransports\":[]}";
        NegotiateResponse negotiateResponse = new NegotiateResponse(stringNegotiateResponse);
        assertTrue(negotiateResponse.getAvailableTransports().isEmpty());
        assertNull(negotiateResponse.getConnectionId());
        assertEquals("some_access_token", negotiateResponse.getAccessToken());
        assertEquals("www.example.com", negotiateResponse.getRedirectUrl());
        assertNull(negotiateResponse.getConnectionId());
    }

    @Test
    public void NegotiateResponseIgnoresExtraProperties() throws IOException {
        String stringNegotiateResponse = "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\"," +
                "\"extra\":\"something\"}";
        NegotiateResponse negotiateResponse = new NegotiateResponse(stringNegotiateResponse);
        assertEquals("bVOiRPG8-6YiJ6d7ZcTOVQ", negotiateResponse.getConnectionId());
    }

    @Test
    public void NegotiateResponseIgnoresExtraComplexProperties() throws IOException {
        String stringNegotiateResponse = "{\"connectionId\":\"bVOiRPG8-6YiJ6d7ZcTOVQ\"," +
                "\"extra\":[\"something\"]}";
        NegotiateResponse negotiateResponse = new NegotiateResponse(stringNegotiateResponse);
        assertEquals("bVOiRPG8-6YiJ6d7ZcTOVQ", negotiateResponse.getConnectionId());
    }
}
