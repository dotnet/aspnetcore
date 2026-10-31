// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.lang.reflect.Type;
import java.nio.ByteBuffer;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.SortedMap;
import java.util.TreeMap;

import org.junit.jupiter.api.Test;

import com.microsoft.signalr.messagepack.MessagePackHubProtocol;

class MessagePackHubProtocolTest {
    private MessagePackHubProtocol messagePackHubProtocol = new MessagePackHubProtocol();

    @Test
    public void checkProtocolName() {
        assertEquals("messagepack", messagePackHubProtocol.getName());
    }

    @Test
    public void checkVersionNumber() {
        assertEquals(1, messagePackHubProtocol.getVersion());
    }

    @Test
    public void verifyWriteInvocationMessage() {
        InvocationMessage invocationMessage = new InvocationMessage(null, null, "test", new Object[] { 42 }, null);
        ByteBuffer result = messagePackHubProtocol.writeMessage(invocationMessage);
        byte[] expectedBytes = {0x0C, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA4, 0x74, 0x65, 0x73,
            0x74, (byte) 0x91, 0x2A, (byte) 0x90};
        ByteString expectedResult = ByteString.of(expectedBytes);
        assertEquals(expectedResult, ByteString.of(result));
    }

    @Test
    public void verifyWriteInvocationMessageWithHeaders() {
        Map<String, String> headers = new HashMap<String, String>();
        headers.put("a", "b");
        headers.put("c", "d");
        InvocationMessage invocationMessage = new InvocationMessage(headers, null, "test", new Object[] { 42 }, null);
        ByteBuffer result = messagePackHubProtocol.writeMessage(invocationMessage);
        byte[] expectedBytes = {0x14, (byte) 0x96, 0x01, (byte) 0x82, (byte) 0xA1, 0x61, (byte) 0xA1, 0x62, (byte) 0xA1, 0x63,
            (byte) 0xA1, 0x64, (byte) 0xC0, (byte) 0xA4, 0x74, 0x65, 0x73, 0x74, (byte) 0x91, 0x2A, (byte) 0x90};
        ByteString expectedResult = ByteString.of(expectedBytes);
        assertEquals(expectedResult, ByteString.of(result));
    }

    @Test
    public void verifyWriteStreamItem() {
        StreamItem streamItem = new StreamItem(null, "id", 42);
        ByteBuffer result = messagePackHubProtocol.writeMessage(streamItem);
        byte[] expectedBytes = {0x07, (byte) 0x94, 0x02, (byte) 0x80, (byte) 0xA2, 0x69, 0x64, 0x2A};
        ByteString expectedResult = ByteString.of(expectedBytes);
        assertEquals(expectedResult, ByteString.of(result));
    }

    @Test
    public void verifyWriteCompletionMessageNonVoid() {
        CompletionMessage completionMessage = new CompletionMessage(null, "id", 42, null);
        ByteBuffer result = messagePackHubProtocol.writeMessage(completionMessage);
        byte[] expectedBytes = {0x08, (byte) 0x95, 0x03, (byte) 0x80, (byte) 0xA2, 0x69, 0x64, 0x03, 0x2A};
        ByteString expectedResult = ByteString.of(expectedBytes);
        assertEquals(expectedResult, ByteString.of(result));
    }

    @Test
    public void verifyWriteCompletionMessageVoid() {
        CompletionMessage completionMessage = new CompletionMessage(null, "id", null, null);
        ByteBuffer result = messagePackHubProtocol.writeMessage(completionMessage);
        byte[] expectedBytes = {0x07, (byte) 0x94, 0x03, (byte) 0x80, (byte) 0xA2, 0x69, 0x64, 0x02};
        ByteString expectedResult = ByteString.of(expectedBytes);
        assertEquals(expectedResult, ByteString.of(result));
    }

    @Test
    public void verifyWriteCompletionMessageError() {
        CompletionMessage completionMessage = new CompletionMessage(null, "id", null, "error");
        ByteBuffer result = messagePackHubProtocol.writeMessage(completionMessage);
        byte[] expectedBytes = {0x0D, (byte) 0x95, 0x03, (byte) 0x80, (byte) 0xA2, 0x69, 0x64, 0x01, (byte) 0xA5, 0x65, 0x72, 0x72, 0x6F, 0x72};
        ByteString expectedResult = ByteString.of(expectedBytes);
        assertEquals(expectedResult, ByteString.of(result));
    }

    @Test
    public void verifyWriteStreamInvocationMessage() {
        List<String> streamIds = new ArrayList<String>();
        streamIds.add("stream");
        StreamInvocationMessage streamInvocationMessage = new StreamInvocationMessage(null, "id", "test", new Object[] {42}, streamIds);
        ByteBuffer result = messagePackHubProtocol.writeMessage(streamInvocationMessage);
        byte[] expectedBytes = {0x15, (byte) 0x96, 0x04, (byte) 0x80, (byte) 0xA2, 0x69, 0x64, (byte) 0xA4, 0x74, 0x65, 0x73, 0x74, (byte) 0x91,
            0x2A, (byte) 0x91, (byte) 0xA6, 0x73, 0x74, 0x72, 0x65, 0x61, 0x6D};
        ByteString expectedResult = ByteString.of(expectedBytes);
        assertEquals(expectedResult, ByteString.of(result));
    }

    @Test
    public void verifyWriteCancelInvocationMessage() {
        CancelInvocationMessage cancelInvocationMessage = new CancelInvocationMessage(null, "id");
        ByteBuffer result = messagePackHubProtocol.writeMessage(cancelInvocationMessage);
        byte[] expectedBytes = {0x06, (byte) 0x93, 0x05, (byte) 0x80, (byte) 0xA2, 0x69, 0x64};
        ByteString expectedResult = ByteString.of(expectedBytes);
        assertEquals(expectedResult, ByteString.of(result));
    }

    @Test
    public void verifyWritePingMessage() {
        ByteBuffer result = messagePackHubProtocol.writeMessage(PingMessage.getInstance());
        byte[] expectedBytes = {0x02, (byte) 0x91, 0x06};
        ByteString expectedResult = ByteString.of(expectedBytes);
        assertEquals(expectedResult, ByteString.of(result));
    }

    @Test
    public void verifyWriteCloseMessage() {
        CloseMessage closeMessage = new CloseMessage();
        ByteBuffer result = messagePackHubProtocol.writeMessage(closeMessage);
        byte[] expectedBytes = {0x04, (byte) 0x93, 0x07, (byte) 0xC0, (byte) 0xC2};
        ByteString expectedResult = ByteString.of(expectedBytes);
        assertEquals(expectedResult, ByteString.of(result));
    }

    @Test
    public void verifyWriteCloseMessageWithError() {
        CloseMessage closeMessage = new CloseMessage("Error");
        ByteBuffer result = messagePackHubProtocol.writeMessage(closeMessage);
        byte[] expectedBytes = {0x09, (byte) 0x93, 0x07, (byte) 0xA5, 0x45, 0x72, 0x72, 0x6F, 0x72, (byte) 0xC2};
        ByteString expectedResult = ByteString.of(expectedBytes);
        assertEquals(expectedResult, ByteString.of(result));
    }

    @Test
    public void parsePingMessage() {
        byte[] messageBytes = {0x02, (byte) 0x91, 0x06};
        ByteBuffer message = ByteBuffer.wrap(messageBytes);
        TestBinder binder = new TestBinder(null, null);

        List<HubMessage> messages = messagePackHubProtocol.parseMessages(message, binder);

        //We know it's only one message
        assertNotNull(messages);
        assertEquals(1, messages.size());
        assertEquals(HubMessageType.PING, messages.get(0).getMessageType());
    }

    @Test
    public void parseCloseMessage() {
        byte[] messageBytes = {0x04, (byte) 0x93, 0x07, (byte) 0xC0, (byte) 0xC2};
        ByteBuffer message = ByteBuffer.wrap(messageBytes);
        TestBinder binder = new TestBinder(null, null);

        List<HubMessage> messages = messagePackHubProtocol.parseMessages(message, binder);

        //We know it's only one message
        assertNotNull(messages);
        assertEquals(1, messages.size());

        assertEquals(HubMessageType.CLOSE, messages.get(0).getMessageType());

        //We can safely cast here because we know that it's a close message.
        CloseMessage closeMessage = (CloseMessage) messages.get(0);

        assertEquals(null, closeMessage.getError());
    }

    @Test
    public void parseCloseMessageWithError() {
        byte[] messageBytes = {0x09, (byte) 0x93, 0x07, (byte) 0xA5, 0x45, 0x72, 0x72, 0x6F, 0x72, (byte) 0xC2};
        ByteBuffer message = ByteBuffer.wrap(messageBytes);
        TestBinder binder = new TestBinder(null, null);

        List<HubMessage> messages = messagePackHubProtocol.parseMessages(message, binder);

        //We know it's only one message
        assertNotNull(messages);
        assertEquals(1, messages.size());

        assertEquals(HubMessageType.CLOSE, messages.get(0).getMessageType());

        //We can safely cast here because we know that it's a close message.
        CloseMessage closeMessage = (CloseMessage) messages.get(0);

        assertEquals("Error", closeMessage.getError());
    }

    @Test
    public void parseSingleInvocationMessage() {
        byte[] messageBytes = {0x0C, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA4, 0x74, 0x65, 0x73,
            0x74, (byte) 0x91, 0x2A, (byte) 0x90};
        ByteBuffer message = ByteBuffer.wrap(messageBytes);
        TestBinder binder = new TestBinder(new Type[] { int.class }, null);

        List<HubMessage> messages = messagePackHubProtocol.parseMessages(message, binder);

        //We know it's only one message
        assertNotNull(messages);
        assertEquals(1, messages.size());

        assertEquals(HubMessageType.INVOCATION, messages.get(0).getMessageType());

        //We can safely cast here because we know that it's an invocation message.
        InvocationMessage invocationMessage = (InvocationMessage) messages.get(0);

        assertEquals("test", invocationMessage.getTarget());
        assertEquals(null, invocationMessage.getInvocationId());
        assertEquals(null, invocationMessage.getHeaders());
        assertEquals(null, invocationMessage.getStreamIds());

        int messageResult = (int)invocationMessage.getArguments()[0];
        assertEquals(42, messageResult);
    }

    @Test
    public void parseSingleInvocationMessageWithHeaders() {
        byte[] messageBytes = {0x14, (byte) 0x96, 0x01, (byte) 0x82, (byte) 0xA1, 0x61, (byte) 0xA1, 0x62, (byte) 0xA1, 0x63,
                (byte) 0xA1, 0x64, (byte) 0xC0, (byte) 0xA4, 0x74, 0x65, 0x73, 0x74, (byte) 0x91, 0x2A, (byte) 0x90};
        ByteBuffer message = ByteBuffer.wrap(messageBytes);
        TestBinder binder = new TestBinder(new Type[] { int.class }, null);

        List<HubMessage> messages = messagePackHubProtocol.parseMessages(message, binder);

        //We know it's only one message
        assertNotNull(messages);
        assertEquals(1, messages.size());

        assertEquals(HubMessageType.INVOCATION, messages.get(0).getMessageType());

        //We can safely cast here because we know that it's an invocation message.
        InvocationMessage invocationMessage = (InvocationMessage) messages.get(0);

        assertEquals("test", invocationMessage.getTarget());
        assertEquals(null, invocationMessage.getInvocationId());

        Map<String, String> headers = invocationMessage.getHeaders();
        assertEquals(2, headers.size());
        assertEquals("b", headers.get("a"));
        assertEquals("d", headers.get("c"));

        assertEquals(null, invocationMessage.getStreamIds());

        int messageResult = (int)invocationMessage.getArguments()[0];
        assertEquals(42, messageResult);
    }

    @Test
    public void parseSingleStreamInvocationMessage() {
        byte[] messageBytes = {0x12, (byte) 0x96, 0x04, (byte) 0x80, (byte) 0xA6, 0x6D, 0x65, 0x74, 0x68, 0x6F, 0x64,
            (byte) 0xA4, 0x74, 0x65, 0x73, 0x74, (byte) 0x91, 0x2A, (byte) 0x90};
        ByteBuffer message = ByteBuffer.wrap(messageBytes);
        TestBinder binder = new TestBinder(new Type[] { int.class }, null);

        List<HubMessage> messages = messagePackHubProtocol.parseMessages(message, binder);

        //We know it's only one message
        assertNotNull(messages);
        assertEquals(1, messages.size());

        assertEquals(HubMessageType.STREAM_INVOCATION, messages.get(0).getMessageType());

        //We can safely cast here because we know that it's a streaminvocation message.
        StreamInvocationMessage streamInvocationMessage = (StreamInvocationMessage) messages.get(0);

        assertEquals("test", streamInvocationMessage.getTarget());
        assertEquals("method", streamInvocationMessage.getInvocationId());
        assertEquals(null, streamInvocationMessage.getHeaders());
        assertEquals(null, streamInvocationMessage.getStreamIds());

        int messageResult = (int)streamInvocationMessage.getArguments()[0];
        assertEquals(42, messageResult);
    }

    @Test
    public void parseSingleCancelInvocationMessage() {
        byte[] messageBytes = {0x0A, (byte) 0x93, 0x05, (byte) 0x80, (byte) 0xA6, 0x6D, 0x65, 0x74, 0x68, 0x6F, 0x64};
        ByteBuffer message = ByteBuffer.wrap(messageBytes);
        TestBinder binder = new TestBinder(null, null);

        List<HubMessage> messages = messagePackHubProtocol.parseMessages(message, binder);

        //We know it's only one message
        assertNotNull(messages);
        assertEquals(1, messages.size());

        assertEquals(HubMessageType.CANCEL_INVOCATION, messages.get(0).getMessageType());

        //We can safely cast here because we know that it's a cancelinvocation message.
        CancelInvocationMessage cancelInvocationMessage = (CancelInvocationMessage) messages.get(0);

        assertEquals("method", cancelInvocationMessage.getInvocationId());
        assertEquals(null, cancelInvocationMessage.getHeaders());
    }

    @Test
    public void parseTwoMessages() {
        byte[] messageBytes = {0x0B, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA3, 0x6F, 0x6E, 0x65, (byte) 0x91, 0x2A,
            (byte) 0x90, 0x0B, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA3, 0x74, 0x77, 0x6F, (byte) 0x91, 0x2B, (byte) 0x90};
        ByteBuffer message = ByteBuffer.wrap(messageBytes);
        TestBinder binder = new TestBinder(new Type[] { int.class }, null);

        List<HubMessage> messages = messagePackHubProtocol.parseMessages(message, binder);

        assertNotNull(messages);
        assertEquals(2, messages.size());

        // Check the first message
        assertEquals(HubMessageType.INVOCATION, messages.get(0).getMessageType());

        //Now that we know we have an invocation message we can cast the hubMessage.
        InvocationMessage invocationMessage = (InvocationMessage) messages.get(0);

        assertEquals("one", invocationMessage.getTarget());
        assertEquals(null, invocationMessage.getInvocationId());
        assertEquals(null, invocationMessage.getHeaders());
        assertEquals(null, invocationMessage.getStreamIds());

        int messageResult = (int)invocationMessage.getArguments()[0];
        assertEquals(42, messageResult);

        // Check the second message
        assertEquals(HubMessageType.INVOCATION, messages.get(1).getMessageType());

        //Now that we know we have an invocation message we can cast the hubMessage.
        InvocationMessage invocationMessage2 = (InvocationMessage) messages.get(1);

        assertEquals("two", invocationMessage2.getTarget());
        assertEquals(null, invocationMessage2.getInvocationId());
        assertEquals(null, invocationMessage2.getHeaders());
        assertEquals(null, invocationMessage2.getStreamIds());

        int secondMessageResult = (int)invocationMessage2.getArguments()[0];
        assertEquals(43, secondMessageResult);
    }

    @Test
    public void parseSingleMessageMutipleArgs() {
        byte[] messageBytes = {0x0F, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA4, 0x74, 0x65, 0x73, 0x74, (byte) 0x92,
            0x2A, (byte) 0xA2, 0x34, 0x32, (byte) 0x90};
        ByteBuffer message = ByteBuffer.wrap(messageBytes);
        TestBinder binder = new TestBinder(new Type[] { int.class, String.class }, null);

        List<HubMessage> messages = messagePackHubProtocol.parseMessages(message, binder);

        assertNotNull(messages);
        assertEquals(1, messages.size());

        //We know it's only one message
        assertEquals(HubMessageType.INVOCATION, messages.get(0).getMessageType());

        InvocationMessage invocationMessage = (InvocationMessage)messages.get(0);
        assertEquals("test", invocationMessage.getTarget());
        assertEquals(null, invocationMessage.getInvocationId());
        int messageResult = (int) invocationMessage.getArguments()[0];
        String messageResult2 = (String) invocationMessage.getArguments()[1];
        assertEquals(42, messageResult);
        assertEquals("42", messageResult2);
    }

    @Test
    public void invocationBindingFailureWhileParsingTooManyArguments() {
        byte[] messageBytes = {0x0F, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA4, 0x74, 0x65, 0x73, 0x74, (byte) 0x92,
            0x2A, (byte) 0xA2, 0x34, 0x32, (byte) 0x90};
        ByteBuffer message = ByteBuffer.wrap(messageBytes);
        TestBinder binder = new TestBinder(new Type[] { int.class }, null);

        List<HubMessage> messages = messagePackHubProtocol.parseMessages(message, binder);

        assertNotNull(messages);
        assertEquals(1, messages.size());

        assertEquals(HubMessageType.INVOCATION_BINDING_FAILURE, messages.get(0).getMessageType());
        InvocationBindingFailureMessage invocationBindingFailureMessage = (InvocationBindingFailureMessage)messages.get(0);
        assertEquals("Invocation provides 2 argument(s) but target expects 1.", invocationBindingFailureMessage.getException().getMessage());
    }

    @Test
    public void invocationBindingFailureWhileParsingTooFewArguments() {
        byte[] messageBytes = {0x0C, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA4, 0x74, 0x65, 0x73, 0x74, (byte) 0x91, 0x2A,
            (byte) 0x90};
        ByteBuffer message = ByteBuffer.wrap(messageBytes);
        TestBinder binder = new TestBinder(new Type[] { int.class, int.class }, null);

        List<HubMessage> messages = messagePackHubProtocol.parseMessages(message, binder);

        assertNotNull(messages);
        assertEquals(1, messages.size());

        assertEquals(HubMessageType.INVOCATION_BINDING_FAILURE, messages.get(0).getMessageType());
        InvocationBindingFailureMessage invocationBindingFailureMessage = (InvocationBindingFailureMessage) messages.get(0);
        assertEquals("Invocation provides 1 argument(s) but target expects 2.", invocationBindingFailureMessage.getException().getMessage());
    }

    @Test
    public void invocationBindingFailureWhenParsingIncorrectType() {
        byte[] messageBytes = {0x0C, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA4, 0x74, 0x65, 0x73, 0x74, (byte) 0x91,
            (byte) 0xC3, (byte) 0x90};
        ByteBuffer message = ByteBuffer.wrap(messageBytes);
        TestBinder binder = new TestBinder(new Type[] { int.class }, null);

        List<HubMessage> messages = messagePackHubProtocol.parseMessages(message, binder);

        assertNotNull(messages);
        assertEquals(1, messages.size());

        assertEquals(HubMessageType.INVOCATION_BINDING_FAILURE, messages.get(0).getMessageType());
        InvocationBindingFailureMessage invocationBindingFailureMessage = (InvocationBindingFailureMessage) messages.get(0);
        // We get different exception messages on different platforms, so use a regex
        assertTrue(invocationBindingFailureMessage.getException().getMessage().matches("^.*Boolean.*cannot be cast to.*Integer.*"));
    }

    @Test
    public void invocationBindingFailureReadsNextMessageAfterTooManyArguments() {
        byte[] messageBytes = {0x0F, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA4, 0x74, 0x65, 0x73, 0x74, (byte) 0x92,
            0x2A, (byte) 0xA2, 0x34, 0x32, (byte) 0x90, 0x0B, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA3, 0x74,
            0x77, 0x6F, (byte) 0x91, 0x2B, (byte) 0x90};
        ByteBuffer message = ByteBuffer.wrap(messageBytes);
        TestBinder binder = new TestBinder(new Type[] { int.class }, null);

        List<HubMessage> messages = messagePackHubProtocol.parseMessages(message, binder);

        assertNotNull(messages);
        assertEquals(2, messages.size());

        assertEquals(HubMessageType.INVOCATION_BINDING_FAILURE, messages.get(0).getMessageType());
        InvocationBindingFailureMessage invocationBindingFailureMessage = (InvocationBindingFailureMessage) messages.get(0);
        assertEquals("Invocation provides 2 argument(s) but target expects 1.", invocationBindingFailureMessage.getException().getMessage());

        // Check the second message
        assertEquals(HubMessageType.INVOCATION, messages.get(1).getMessageType());

        //Now that we know we have an invocation message we can cast the hubMessage.
        InvocationMessage invocationMessage2 = (InvocationMessage) messages.get(1);

        assertEquals("two", invocationMessage2.getTarget());
        assertEquals(null, invocationMessage2.getInvocationId());
        assertEquals(null, invocationMessage2.getHeaders());
        assertEquals(null, invocationMessage2.getStreamIds());

        int secondMessageResult = (int)invocationMessage2.getArguments()[0];
        assertEquals(43, secondMessageResult);
    }

    @Test
    public void invocationBindingFailureReadsNextMessageAfterTooFewArguments() {
        byte[] messageBytes = {0x0C, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA4, 0x74, 0x65, 0x73, 0x74, (byte) 0x91, 0x2A,
            (byte) 0x90, 0x0C, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA3, 0x74, 0x77, 0x6F, (byte) 0x92, 0x2A, 0x2B, (byte) 0x90};
        ByteBuffer message = ByteBuffer.wrap(messageBytes);
        TestBinder binder = new TestBinder(new Type[] { int.class, int.class }, null);

        List<HubMessage> messages = messagePackHubProtocol.parseMessages(message, binder);

        assertNotNull(messages);
        assertEquals(2, messages.size());

        assertEquals(HubMessageType.INVOCATION_BINDING_FAILURE, messages.get(0).getMessageType());
        InvocationBindingFailureMessage invocationBindingFailureMessage = (InvocationBindingFailureMessage) messages.get(0);
        assertEquals("Invocation provides 1 argument(s) but target expects 2.", invocationBindingFailureMessage.getException().getMessage());

        // Check the second message
        assertEquals(HubMessageType.INVOCATION, messages.get(1).getMessageType());

        //Now that we know we have an invocation message we can cast the hubMessage.
        InvocationMessage invocationMessage2 = (InvocationMessage) messages.get(1);

        assertEquals("two", invocationMessage2.getTarget());
        assertEquals(null, invocationMessage2.getInvocationId());
        assertEquals(null, invocationMessage2.getHeaders());
        assertEquals(null, invocationMessage2.getStreamIds());

        int secondMessageResult1 = (int)invocationMessage2.getArguments()[0];
        int secondMessageResult2 = (int)invocationMessage2.getArguments()[1];
        assertEquals(42, secondMessageResult1);
        assertEquals(43, secondMessageResult2);
    }

    @Test
    public void invocationBindingFailureReadsNextMessageAfterIncorrectArgument() {
        byte[] messageBytes = {0x0C, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA4, 0x74, 0x65, 0x73, 0x74, (byte) 0x91,
            (byte) 0xC3, (byte) 0x90, 0x0C, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA4, 0x74, 0x65, 0x73, 0x74,
            (byte) 0x91, 0x2A, (byte) 0x90};
        ByteBuffer message = ByteBuffer.wrap(messageBytes);
        TestBinder binder = new TestBinder(new Type[] { int.class }, null);

        List<HubMessage> messages = messagePackHubProtocol.parseMessages(message, binder);

        assertNotNull(messages);
        assertEquals(2, messages.size());

        assertEquals(HubMessageType.INVOCATION_BINDING_FAILURE, messages.get(0).getMessageType());
        InvocationBindingFailureMessage invocationBindingFailureMessage = (InvocationBindingFailureMessage) messages.get(0);
        // We get different exception messages on different platforms, so use a regex
        assertTrue(invocationBindingFailureMessage.getException().getMessage().matches("^.*Boolean.*cannot be cast to.*Integer.*"));

        // Check the second message
        assertEquals(HubMessageType.INVOCATION, messages.get(1).getMessageType());

        //Now that we know we have an invocation message we can cast the hubMessage.
        InvocationMessage invocationMessage2 = (InvocationMessage) messages.get(1);

        assertEquals("test", invocationMessage2.getTarget());
        assertEquals(null, invocationMessage2.getInvocationId());
        assertEquals(null, invocationMessage2.getHeaders());
        assertEquals(null, invocationMessage2.getStreamIds());

        int secondMessageResult = (int)invocationMessage2.getArguments()[0];
        assertEquals(42, secondMessageResult);
    }

    @Test
    public void errorWhenLengthHeaderTooLong() {
        byte[] messageBytes = {0x0D, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA4, 0x74, 0x65, 0x73, 0x74, (byte) 0x91,
            0x2A, (byte) 0x90};
        ByteBuffer message = ByteBuffer.wrap(messageBytes);
        TestBinder binder = new TestBinder(new Type[] { int.class }, null);

        RuntimeException exception = assertThrows(RuntimeException.class,
                () -> messagePackHubProtocol.parseMessages(message, binder));
        assertEquals("MessagePack message was length 12 but claimed to be length 13.", exception.getMessage());
    }

    @Test
    public void errorWhenLengthHeaderTooShort() {
        byte[] messageBytes = {0x0B, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA4, 0x74, 0x65, 0x73, 0x74, (byte) 0x91,
            0x2A, (byte) 0x90};
        ByteBuffer message = ByteBuffer.wrap(messageBytes);
        TestBinder binder = new TestBinder(new Type[] { int.class }, null);

        RuntimeException exception = assertThrows(RuntimeException.class,
                () -> messagePackHubProtocol.parseMessages(message, binder));
        assertEquals("MessagePack message was length 12 but claimed to be length 11.", exception.getMessage());
    }

    @Test
    public void parseMessageWithTwoByteLengthHeader() {
        // Test that a long message w/ a 2-byte length header is still parsed correctly
        byte[] messageBytes = {(byte) 0x87, 0x01, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA4, 0x74, 0x65, 0x73, 0x74,
            (byte) 0x91, (byte) 0xD9, 0x7A, 0x54, 0x68, 0x69, 0x73, 0x20, 0x69, 0x73, 0x20, 0x61, 0x20, 0x72, 0x65, 0x61, 0x6C, 0x6C,
            0x79, 0x20, 0x6C, 0x6F, 0x6E, 0x67, 0x20, 0x61, 0x72, 0x67, 0x75, 0x6D, 0x65, 0x6E, 0x74, 0x20, 0x74, 0x6F, 0x20, 0x6D,
            0x61, 0x6B, 0x65, 0x20, 0x74, 0x68, 0x65, 0x20, 0x6C, 0x65, 0x6E, 0x67, 0x74, 0x68, 0x20, 0x6F, 0x66, 0x20, 0x74, 0x68,
            0x69, 0x73, 0x20, 0x6D, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, 0x20, 0x6D, 0x6F, 0x72, 0x65, 0x20, 0x74, 0x68, 0x61, 0x6E,
            0x20, 0x31, 0x32, 0x37, 0x20, 0x62, 0x79, 0x74, 0x65, 0x73, 0x2E, 0x20, 0x57, 0x65, 0x20, 0x6A, 0x75, 0x73, 0x74, 0x20,
            0x6E, 0x65, 0x65, 0x64, 0x20, 0x61, 0x20, 0x66, 0x65, 0x77, 0x20, 0x6D, 0x6F, 0x72, 0x65, 0x20, 0x63, 0x68, 0x61, 0x72,
            0x61, 0x63, 0x74, 0x65, 0x72, 0x73, 0x2E, (byte) 0x90};
        ByteBuffer message = ByteBuffer.wrap(messageBytes);
        TestBinder binder = new TestBinder(new Type[] { String.class }, null);

        List<HubMessage> messages = messagePackHubProtocol.parseMessages(message, binder);

        //We know it's only one message
        assertNotNull(messages);
        assertEquals(1, messages.size());

        assertEquals(HubMessageType.INVOCATION, messages.get(0).getMessageType());

        //We can safely cast here because we know that it's an invocation message.
        InvocationMessage invocationMessage = (InvocationMessage) messages.get(0);

        assertEquals("test", invocationMessage.getTarget());
        assertEquals(null, invocationMessage.getInvocationId());
        assertEquals(null, invocationMessage.getHeaders());
        assertEquals(null, invocationMessage.getStreamIds());

        String messageResult = (String)invocationMessage.getArguments()[0];
        assertEquals("This is a really long argument to make the length of this message more than "
            + "127 bytes. We just need a few more characters.", messageResult);
    }

    @Test
    public void verifyWriteInvocationMessageWithTwoByteLengthHeader() {
        InvocationMessage invocationMessage = new InvocationMessage(null, null, "test", new Object[] { "This is a really long argument to make "
                + "the length of this message more than 127 bytes. We just need a few more characters." }, null);
        ByteBuffer result = messagePackHubProtocol.writeMessage(invocationMessage);
        byte[] expectedBytes = {(byte) 0x87, 0x01, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA4, 0x74, 0x65, 0x73, 0x74,
            (byte) 0x91, (byte) 0xD9, 0x7A, 0x54, 0x68, 0x69, 0x73, 0x20, 0x69, 0x73, 0x20, 0x61, 0x20, 0x72, 0x65, 0x61, 0x6C, 0x6C,
            0x79, 0x20, 0x6C, 0x6F, 0x6E, 0x67, 0x20, 0x61, 0x72, 0x67, 0x75, 0x6D, 0x65, 0x6E, 0x74, 0x20, 0x74, 0x6F, 0x20, 0x6D,
            0x61, 0x6B, 0x65, 0x20, 0x74, 0x68, 0x65, 0x20, 0x6C, 0x65, 0x6E, 0x67, 0x74, 0x68, 0x20, 0x6F, 0x66, 0x20, 0x74, 0x68,
            0x69, 0x73, 0x20, 0x6D, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, 0x20, 0x6D, 0x6F, 0x72, 0x65, 0x20, 0x74, 0x68, 0x61, 0x6E,
            0x20, 0x31, 0x32, 0x37, 0x20, 0x62, 0x79, 0x74, 0x65, 0x73, 0x2E, 0x20, 0x57, 0x65, 0x20, 0x6A, 0x75, 0x73, 0x74, 0x20,
            0x6E, 0x65, 0x65, 0x64, 0x20, 0x61, 0x20, 0x66, 0x65, 0x77, 0x20, 0x6D, 0x6F, 0x72, 0x65, 0x20, 0x63, 0x68, 0x61, 0x72,
            0x61, 0x63, 0x74, 0x65, 0x72, 0x73, 0x2E, (byte) 0x90};
        ByteString expectedResult = ByteString.of(expectedBytes);
        assertEquals(expectedResult, ByteString.of(result));
    }

    @Test
    public void parseInvocationMessageWithPrimitiveArgs() {
        byte[] messageBytes = {0x1E, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA4, 0x74, 0x65, 0x73, 0x74, (byte) 0x96, 0x01, (byte) 0xCB,
            0x40, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, (byte) 0xC3, 0x11, (byte) 0xA1, 0x63, (byte) 0xCE, (byte) 0xC6, (byte) 0xAE, (byte) 0xA1,
            0x55, (byte) 0x90};
        ByteBuffer message = ByteBuffer.wrap(messageBytes);
        int i = 1;
        double d = 2.5d;
        boolean bool = true;
        byte bite = 0x11;
        char c = 'c';
        long l = 3333333333l;
        TestBinder binder = new TestBinder(new Type[] { int.class, double.class, boolean.class, byte.class, char.class, long.class }, null);

        List<HubMessage> messages = messagePackHubProtocol.parseMessages(message, binder);

        //We know it's only one message
        assertNotNull(messages);
        assertEquals(1, messages.size());

        assertEquals(HubMessageType.INVOCATION, messages.get(0).getMessageType());

        //We can safely cast here because we know that it's an invocation message.
        InvocationMessage invocationMessage = (InvocationMessage) messages.get(0);

        assertEquals("test", invocationMessage.getTarget());
        assertEquals(null, invocationMessage.getInvocationId());
        assertEquals(null, invocationMessage.getHeaders());
        assertEquals(null, invocationMessage.getStreamIds());

        Object[] args = invocationMessage.getArguments();
        assertEquals(6, args.length);
        assertEquals(i, (int)args[0]);
        assertEquals(d, (double)args[1]);
        assertEquals(bool, (boolean)args[2]);
        assertEquals(bite, (byte)args[3]);
        assertEquals(c, (char)args[4]);
        assertEquals(l, (long)args[5]);
    }

    @Test
    public void verifyWriteInvocationMessageWithPrimitiveArgs() {
        int i = 1;
        double d = 2.5d;
        boolean bool = true;
        byte bite = 0x11;
        char c = 'c';
        long l = 3333333333l;
        InvocationMessage invocationMessage = new InvocationMessage(null, null, "test", new Object[] { i, d, bool, bite, c, l }, null);
        ByteBuffer result = messagePackHubProtocol.writeMessage(invocationMessage);
        byte[] expectedBytes = {0x1E, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA4, 0x74, 0x65, 0x73, 0x74, (byte) 0x96, 0x01,
            (byte) 0xCB, 0x40, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, (byte) 0xC3, 0x11, (byte) 0xA1, 0x63, (byte) 0xCE, (byte) 0xC6, (byte) 0xAE,
            (byte) 0xA1, 0x55, (byte) 0x90};
        ByteString expectedResult = ByteString.of(expectedBytes);
        assertEquals(expectedResult, ByteString.of(result));
    }

    @Test
    public void parseInvocationMessageWithArrayArg() {
        // Make sure that the same bytes can be parsed as both an Array and a List
        byte[] messageBytes = {0x10, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA4, 0x74, 0x65, 0x73, 0x74, (byte) 0x91, (byte) 0x94, 0x01,
            0x02, 0x03, 0x04, (byte) 0x90};
        ByteBuffer message = ByteBuffer.wrap(messageBytes);

        TestBinder arrayBinder = new TestBinder(new Type[] { int[].class }, null);
        TestBinder listBinder = new TestBinder(new Type[] { (new TypeReference<ArrayList<Integer>>() { }).getType() }, null);

        List<HubMessage> arrayMessages = messagePackHubProtocol.parseMessages(message, arrayBinder);
        message.flip();
        List<HubMessage> listMessages = messagePackHubProtocol.parseMessages(message, listBinder);

        //We know it's only one message
        assertNotNull(arrayMessages);
        assertEquals(1, arrayMessages.size());

        assertNotNull(listMessages);
        assertEquals(1, listMessages.size());

        assertEquals(HubMessageType.INVOCATION, arrayMessages.get(0).getMessageType());
        assertEquals(HubMessageType.INVOCATION, listMessages.get(0).getMessageType());

        //We can safely cast here because we know that it's an invocation message.
        InvocationMessage arrayInvocationMessage = (InvocationMessage) arrayMessages.get(0);
        InvocationMessage listInvocationMessage = (InvocationMessage) listMessages.get(0);

        assertEquals("test", arrayInvocationMessage.getTarget());
        assertEquals(null, arrayInvocationMessage.getInvocationId());
        assertEquals(null, arrayInvocationMessage.getHeaders());
        assertEquals(null, arrayInvocationMessage.getStreamIds());

        assertEquals("test", listInvocationMessage.getTarget());
        assertEquals(null, listInvocationMessage.getInvocationId());
        assertEquals(null, listInvocationMessage.getHeaders());
        assertEquals(null, listInvocationMessage.getStreamIds());

        int[] arrayArg = (int[])arrayInvocationMessage.getArguments()[0];
        @SuppressWarnings("unchecked")
        List<Integer> listArg = (ArrayList<Integer>)listInvocationMessage.getArguments()[0];

        assertEquals(4, arrayArg.length);
        assertEquals(4, listArg.size());
        for (int i = 0; i < arrayArg.length; i++) {
            assertEquals(i + 1, arrayArg[i]);
            assertEquals(i + 1, (int) listArg.get(i));
        }
    }

    @Test
    public void verifyWriteInvocationMessageWithArrayArg() {
        InvocationMessage invocationMessage = new InvocationMessage(null, null, "test", new Object[] { new int[] { 1, 2, 3, 4 } }, null);
        ByteBuffer result = messagePackHubProtocol.writeMessage(invocationMessage);
        byte[] expectedBytes = {0x10, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA4, 0x74, 0x65, 0x73, 0x74, (byte) 0x91, (byte) 0x94, 0x01,
                0x02, 0x03, 0x04, (byte) 0x90};
        ByteString expectedResult = ByteString.of(expectedBytes);
        assertEquals(expectedResult, ByteString.of(result));
    }

    @Test
    public void parseInvocationMessageWithMapArg() {
        byte[] messageBytes = {0x23, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA4, 0x74, 0x65, 0x73, 0x74, (byte) 0x91, (byte) 0x82, (byte) 0xA5,
            0x61, 0x70, 0x70, 0x6C, 0x65, (byte) 0xA6, 0x62, 0x61, 0x6E, 0x61, 0x6E, 0x61, (byte) 0xA3, 0x6B, 0x65, 0x79, (byte) 0xA5, 0x76, 0x61, 0x6C, 0x75,
            0x65, (byte) 0x90};
        ByteBuffer message = ByteBuffer.wrap(messageBytes);

        TestBinder binder = new TestBinder(new Type[] { (new TypeReference<HashMap<String, String>>() { }).getType() }, null);

        List<HubMessage> messages = messagePackHubProtocol.parseMessages(message, binder);

        //We know it's only one message
        assertNotNull(messages);
        assertEquals(1, messages.size());

        assertEquals(HubMessageType.INVOCATION, messages.get(0).getMessageType());

        //We can safely cast here because we know that it's an invocation message.
        InvocationMessage invocationMessage = (InvocationMessage) messages.get(0);

        assertEquals("test", invocationMessage.getTarget());
        assertEquals(null, invocationMessage.getInvocationId());
        assertEquals(null, invocationMessage.getHeaders());
        assertEquals(null, invocationMessage.getStreamIds());

        @SuppressWarnings("unchecked")
        Map<String, String> result = (HashMap<String, String>)invocationMessage.getArguments()[0];
        assertEquals(2, result.size());
        assertEquals("value", result.get("key"));
        assertEquals("banana", result.get("apple"));
    }

    @Test
    public void verifyWriteInvocationMessageWithMapArg() {
        SortedMap<String, String> argument = new TreeMap<String, String>();
        argument.put("apple", "banana");
        argument.put("key", "value");
        InvocationMessage invocationMessage = new InvocationMessage(null, null, "test", new Object[] { argument }, null);
        ByteBuffer result = messagePackHubProtocol.writeMessage(invocationMessage);
        byte[] expectedBytes = {0x23, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA4, 0x74, 0x65, 0x73, 0x74, (byte) 0x91, (byte) 0x82, (byte) 0xA5,
            0x61, 0x70, 0x70, 0x6C, 0x65, (byte) 0xA6, 0x62, 0x61, 0x6E, 0x61, 0x6E, 0x61, (byte) 0xA3, 0x6B, 0x65, 0x79, (byte) 0xA5, 0x76, 0x61, 0x6C, 0x75,
            0x65, (byte) 0x90};
        ByteString expectedResult = ByteString.of(expectedBytes);
        assertEquals(expectedResult, ByteString.of(result));
    }

    @Test
    public void parseInvocationMessageWithNestedCollection() {
        byte[] messageBytes = {0x39, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA4, 0x74, 0x65, 0x73, 0x74, (byte) 0x91, (byte) 0x92,
            (byte) 0x82, (byte) 0xA3, 0x6F, 0x6E, 0x65, (byte) 0x92, (byte) 0xA1, 0x61, (byte) 0xA1, 0x62, (byte) 0xA3, 0x74, 0x77, 0x6F, (byte) 0x92,
            (byte) 0xA3, (byte) 0xEB, (byte) 0xBB, (byte) 0xAF, (byte) 0xA3, (byte) 0xEA, (byte) 0xAF, (byte) 0x8D, (byte) 0x82, (byte) 0xA4, 0x66,
            0x6F, 0x75, 0x72, (byte) 0x92, (byte) 0xA1, 0x5E, (byte) 0xA1, 0x2A, (byte) 0xA5, 0x74, 0x68, 0x72, 0x65, 0x65, (byte) 0x92, (byte) 0xA1,
            0x35, (byte) 0xA1, 0x39, (byte) 0x90};
        ByteBuffer message = ByteBuffer.wrap(messageBytes);

        TestBinder binder = new TestBinder(new Type[] { (new TypeReference<ArrayList<HashMap<String, ArrayList<Character>>>>() { }).getType() }, null);

        List<HubMessage> messages = messagePackHubProtocol.parseMessages(message, binder);

        //We know it's only one message
        assertNotNull(messages);
        assertEquals(1, messages.size());

        assertEquals(HubMessageType.INVOCATION, messages.get(0).getMessageType());

        //We can safely cast here because we know that it's an invocation message.
        InvocationMessage invocationMessage = (InvocationMessage) messages.get(0);

        assertEquals("test", invocationMessage.getTarget());
        assertEquals(null, invocationMessage.getInvocationId());
        assertEquals(null, invocationMessage.getHeaders());
        assertEquals(null, invocationMessage.getStreamIds());

        @SuppressWarnings("unchecked")
        ArrayList<HashMap<String, ArrayList<Character>>> result = (ArrayList<HashMap<String, ArrayList<Character>>>)invocationMessage.getArguments()[0];
        assertEquals(2, result.size());

        HashMap<String, ArrayList<Character>> firstMap = result.get(0);
        HashMap<String, ArrayList<Character>> secondMap = result.get(1);

        assertEquals(2, firstMap.keySet().size());
        assertEquals(2, secondMap.keySet().size());

        ArrayList<Character> firstList = firstMap.get("one");
        ArrayList<Character> secondList = firstMap.get("two");

        ArrayList<Character> thirdList = secondMap.get("three");
        ArrayList<Character> fourthList = secondMap.get("four");

        assertEquals(2, firstList.size());
        assertEquals(2, secondList.size());
        assertEquals(2, thirdList.size());
        assertEquals(2, fourthList.size());

        assertEquals('a', (char) firstList.get(0));
        assertEquals('b', (char) firstList.get(1));

        assertEquals('\ubeef', (char) secondList.get(0));
        assertEquals('\uabcd', (char) secondList.get(1));

        assertEquals('5', (char) thirdList.get(0));
        assertEquals('9', (char) thirdList.get(1));

        assertEquals('^', (char) fourthList.get(0));
        assertEquals('*', (char) fourthList.get(1));
    }

    @Test
    public void verifyWriteInvocationMessageWithNestedCollection() {
        ArrayList<Character> clist1 = new ArrayList<Character>();
        ArrayList<Character> clist2 = new ArrayList<Character>();
        ArrayList<Character> clist3 = new ArrayList<Character>();
        ArrayList<Character> clist4 = new ArrayList<Character>();

        clist1.add('a');
        clist1.add('b');

        clist2.add('\ubeef');
        clist2.add('\uabcd');

        clist3.add('5');
        clist3.add('9');

        clist4.add('^');
        clist4.add('*');

        TreeMap<String, ArrayList<Character>> map1 = new TreeMap<String, ArrayList<Character>>();
        TreeMap<String, ArrayList<Character>> map2 = new TreeMap<String, ArrayList<Character>>();

        map1.put("one", clist1);
        map1.put("two", clist2);

        map2.put("three", clist3);
        map2.put("four", clist4);

        ArrayList<TreeMap<String, ArrayList<Character>>> argument = new ArrayList<TreeMap<String, ArrayList<Character>>>();
        argument.add(map1);
        argument.add(map2);
        InvocationMessage invocationMessage = new InvocationMessage(null, null, "test", new Object[] { argument }, null);
        ByteBuffer result = messagePackHubProtocol.writeMessage(invocationMessage);
        byte[] expectedBytes = {0x39, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA4, 0x74, 0x65, 0x73, 0x74, (byte) 0x91, (byte) 0x92,
            (byte) 0x82, (byte) 0xA3, 0x6F, 0x6E, 0x65, (byte) 0x92, (byte) 0xA1, 0x61, (byte) 0xA1, 0x62, (byte) 0xA3, 0x74, 0x77, 0x6F, (byte) 0x92,
            (byte) 0xA3, (byte) 0xEB, (byte) 0xBB, (byte) 0xAF, (byte) 0xA3, (byte) 0xEA, (byte) 0xAF, (byte) 0x8D, (byte) 0x82, (byte) 0xA4, 0x66,
            0x6F, 0x75, 0x72, (byte) 0x92, (byte) 0xA1, 0x5E, (byte) 0xA1, 0x2A, (byte) 0xA5, 0x74, 0x68, 0x72, 0x65, 0x65, (byte) 0x92, (byte) 0xA1,
            0x35, (byte) 0xA1, 0x39, (byte) 0x90};
        ByteString expectedResult = ByteString.of(expectedBytes);
        assertEquals(expectedResult, ByteString.of(result));
    }

    @Test
    public void parseInvocationMessageWithCustomPojoArg() {
        byte[] messageBytes = {0x32, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA4, 0x74, 0x65, 0x73, 0x74, (byte) 0x91, (byte) 0x84, (byte) 0xA9,
            0x66, 0x69, 0x72, 0x73, 0x74, 0x4E, 0x61, 0x6D, 0x65, (byte) 0xA4, 0x4A, 0x6F, 0x68, 0x6E, (byte) 0xA8, 0x6C, 0x61, 0x73, 0x74, 0x4E, 0x61,
            0x6D, 0x65, (byte) 0xA3, 0x44, 0x6F, 0x65, (byte) 0xA3, 0x61, 0x67, 0x65, 0x1E, (byte) 0xA1, 0x74, (byte) 0x92, 0x05, 0x08, (byte) 0x90};
        ByteBuffer message = ByteBuffer.wrap(messageBytes);

        TestBinder binder = new TestBinder(new Type[] { (new TypeReference<PersonPojo<ArrayList<Short>>>() { }).getType() }, null);

        List<HubMessage> messages = messagePackHubProtocol.parseMessages(message, binder);

        //We know it's only one message
        assertNotNull(messages);
        assertEquals(1, messages.size());

        assertEquals(HubMessageType.INVOCATION, messages.get(0).getMessageType());

        //We can safely cast here because we know that it's an invocation message.
        InvocationMessage invocationMessage = (InvocationMessage) messages.get(0);

        assertEquals("test", invocationMessage.getTarget());
        assertEquals(null, invocationMessage.getInvocationId());
        assertEquals(null, invocationMessage.getHeaders());
        assertEquals(null, invocationMessage.getStreamIds());

        @SuppressWarnings("unchecked")
        PersonPojo<ArrayList<Short>> result = (PersonPojo<ArrayList<Short>>)invocationMessage.getArguments()[0];
        assertEquals("John", result.getFirstName());
        assertEquals("Doe", result.getLastName());
        assertEquals(30, result.getAge());

        ArrayList<Short> generic = result.getT();
        assertEquals(2, generic.size());
        assertEquals((short)5, (short)generic.get(0));
        assertEquals((short)8, (short)generic.get(1));
    }

    @Test
    public void verifyWriteInvocationMessageWithCustomPojoArg() {
        ArrayList<Short> shorts = new ArrayList<Short>();
        shorts.add((short) 5);
        shorts.add((short) 8);

        PersonPojo<ArrayList<Short>> person = new PersonPojo<ArrayList<Short>>("John", "Doe", 30, shorts);

        InvocationMessage invocationMessage = new InvocationMessage(null, null, "test", new Object[] { person }, null);
        ByteBuffer result = messagePackHubProtocol.writeMessage(invocationMessage);
        byte[] expectedBytes = {0x32, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA4, 0x74, 0x65, 0x73, 0x74, (byte) 0x91, (byte) 0x84, (byte) 0xA9,
            0x66, 0x69, 0x72, 0x73, 0x74, 0x4E, 0x61, 0x6D, 0x65, (byte) 0xA4, 0x4A, 0x6F, 0x68, 0x6E, (byte) 0xA8, 0x6C, 0x61, 0x73, 0x74, 0x4E, 0x61,
            0x6D, 0x65, (byte) 0xA3, 0x44, 0x6F, 0x65, (byte) 0xA3, 0x61, 0x67, 0x65, 0x1E, (byte) 0xA1, 0x74, (byte) 0x92, 0x05, 0x08, (byte) 0x90};
        ByteString expectedResult = ByteString.of(expectedBytes);
        assertEquals(expectedResult, ByteString.of(result));
    }
}
