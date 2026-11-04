// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import static org.junit.jupiter.api.Assertions.*;

import java.nio.ByteBuffer;
import org.junit.jupiter.api.Test;

class HandshakeProtocolTest {
    @Test
    public void VerifyCreateHandshakerequestMessage() {
        HandshakeRequestMessage handshakeRequest = new HandshakeRequestMessage("json", 1);
        ByteBuffer result = HandshakeProtocol.createHandshakeRequestMessage(handshakeRequest);
        String expectedResult = "{\"protocol\":\"json\",\"version\":1}\u001E";
        assertEquals(expectedResult, TestUtils.byteBufferToString(result));
    }

    @Test
    public void VerifyParseEmptyHandshakeResponseMessage() {
        String emptyHandshakeResponse = "{}";
        HandshakeResponseMessage hsr = HandshakeProtocol.parseHandshakeResponse(emptyHandshakeResponse);
        assertNull(hsr.getHandshakeError());
    }

    @Test
    public void VerifyParseHandshakeResponseMessage() {
        String handshakeResponseWithError = "{\"error\": \"Requested protocol \'messagepack\' is not available.\"}";
        HandshakeResponseMessage hsr = HandshakeProtocol.parseHandshakeResponse(handshakeResponseWithError);
        assertEquals(hsr.getHandshakeError(), "Requested protocol 'messagepack' is not available.");
    }

    @Test
    public void InvalidHandshakeResponse() {
        String handshakeResponseWithError = "{\"error\": \"Requested proto";
        Throwable exception = assertThrows(RuntimeException.class, ()-> HandshakeProtocol.parseHandshakeResponse(handshakeResponseWithError));
    }
}