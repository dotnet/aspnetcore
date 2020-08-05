// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import static org.junit.jupiter.api.Assertions.*;

import java.nio.ByteBuffer;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

import org.junit.jupiter.api.Test;


class JsonHubProtocolTest {
    private JsonHubProtocol jsonHubProtocol = new JsonHubProtocol();

    @Test
    public void checkProtocolName() {
        assertEquals("json", jsonHubProtocol.getName());
    }

    @Test
    public void checkVersionNumber() {
        assertEquals(1, jsonHubProtocol.getVersion());
    }

    @Test
    public void checkTransferFormat() {
        assertEquals(TransferFormat.TEXT, jsonHubProtocol.getTransferFormat());
    }

    @Test
    public void verifyWriteMessage() {
        InvocationMessage invocationMessage = new InvocationMessage(null, null, "test", new Object[] {"42"}, null);
        String result = TestUtils.ByteBufferToString(jsonHubProtocol.writeMessage(invocationMessage));
        String expectedResult = "{\"type\":1,\"target\":\"test\",\"arguments\":[\"42\"]}\u001E";
        assertEquals(expectedResult, result);
    }

    @Test
    public void parsePingMessage() {
        String stringifiedMessage = "{\"type\":6}\u001E";
        ByteBuffer message = TestUtils.StringToByteBuffer(stringifiedMessage);
        TestBinder binder = new TestBinder(PingMessage.getInstance());

        List<HubMessage> messages = jsonHubProtocol.parseMessages(message, binder);

        //We know it's only one message
        assertNotNull(messages);
        assertEquals(1, messages.size());
        assertEquals(HubMessageType.PING, messages.get(0).getMessageType());
    }

    @Test
    public void parseCloseMessage() {
        String stringifiedMessage = "{\"type\":7}\u001E";
        ByteBuffer message = TestUtils.StringToByteBuffer(stringifiedMessage);
        TestBinder binder = new TestBinder(new CloseMessage());

        List<HubMessage> messages = jsonHubProtocol.parseMessages(message, binder);

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
        String stringifiedMessage = "{\"type\":7,\"error\": \"There was an error\"}\u001E";
        ByteBuffer message = TestUtils.StringToByteBuffer(stringifiedMessage);
        TestBinder binder = new TestBinder(new CloseMessage("There was an error"));

        List<HubMessage> messages = jsonHubProtocol.parseMessages(message, binder);

        //We know it's only one message
        assertNotNull(messages);
        assertEquals(1, messages.size());

        assertEquals(HubMessageType.CLOSE, messages.get(0).getMessageType());

        //We can safely cast here because we know that it's a close message.
        CloseMessage closeMessage = (CloseMessage) messages.get(0);

        assertEquals("There was an error", closeMessage.getError());
    }

    @Test
    public void parseSingleMessage() {
        String stringifiedMessage = "{\"type\":1,\"target\":\"test\",\"arguments\":[42]}\u001E";
        ByteBuffer message = TestUtils.StringToByteBuffer(stringifiedMessage);
        TestBinder binder = new TestBinder(new InvocationMessage(null, "1", "test", new Object[] { 42 }, null));

        List<HubMessage> messages = jsonHubProtocol.parseMessages(message, binder);

        //We know it's only one message
        assertNotNull(messages);
        assertEquals(1, messages.size());

        assertEquals(HubMessageType.INVOCATION, messages.get(0).getMessageType());

        //We can safely cast here because we know that it's an invocation message.
        InvocationMessage invocationMessage = (InvocationMessage) messages.get(0);

        assertEquals("test", invocationMessage.getTarget());
        assertEquals(null, invocationMessage.getInvocationId());

        int messageResult = (int)invocationMessage.getArguments()[0];
        assertEquals(42, messageResult);
    }

    @Test
    public void parseSingleUnsupportedStreamInvocationMessage() {
        String stringifiedMessage = "{\"type\":4,\"Id\":1,\"target\":\"test\",\"arguments\":[42]}\u001E";
        ByteBuffer message = TestUtils.StringToByteBuffer(stringifiedMessage);
        TestBinder binder = new TestBinder(new StreamInvocationMessage(null, "1", "test", new Object[] { 42 }, null));

        Throwable exception = assertThrows(UnsupportedOperationException.class, () -> jsonHubProtocol.parseMessages(message, binder));
        assertEquals("The message type STREAM_INVOCATION is not supported yet.", exception.getMessage());
    }

    @Test
    public void parseSingleUnsupportedCancelInvocationMessage() {
        String stringifiedMessage = "{\"type\":5,\"invocationId\":123}\u001E";
        ByteBuffer message = TestUtils.StringToByteBuffer(stringifiedMessage);
        TestBinder binder = new TestBinder(null);

        Throwable exception = assertThrows(UnsupportedOperationException.class, () -> jsonHubProtocol.parseMessages(message, binder));
        assertEquals("The message type CANCEL_INVOCATION is not supported yet.", exception.getMessage());
    }

    @Test
    public void parseTwoMessages() {
        String stringifiedMessage = "{\"type\":1,\"target\":\"one\",\"arguments\":[42]}\u001E{\"type\":1,\"target\":\"two\",\"arguments\":[43]}\u001E";
        ByteBuffer message = TestUtils.StringToByteBuffer(stringifiedMessage);
        TestBinder binder = new TestBinder(new InvocationMessage(null, "1", "one", new Object[] { 42 }, null));

        List<HubMessage> messages = jsonHubProtocol.parseMessages(message, binder);
        
        assertNotNull(messages);
        assertEquals(2, messages.size());

        // Check the first message
        assertEquals(HubMessageType.INVOCATION, messages.get(0).getMessageType());

        //Now that we know we have an invocation message we can cast the hubMessage.
        InvocationMessage invocationMessage = (InvocationMessage) messages.get(0);

        assertEquals("one", invocationMessage.getTarget());
        assertEquals(null, invocationMessage.getInvocationId());
        int messageResult = (int)invocationMessage.getArguments()[0];
        assertEquals(42, messageResult);

        // Check the second message
        assertEquals(HubMessageType.INVOCATION, messages.get(1).getMessageType());

        //Now that we know we have an invocation message we can cast the hubMessage.
        InvocationMessage invocationMessage2 = (InvocationMessage) messages.get(1);

        assertEquals("two", invocationMessage2.getTarget());
        assertEquals(null, invocationMessage2.getInvocationId());
        int secondMessageResult = (int)invocationMessage2.getArguments()[0];
        assertEquals(43, secondMessageResult);
    }

    @Test
    public void parseSingleMessageMutipleArgs() {
        String stringifiedMessage = "{\"type\":1,\"target\":\"test\",\"arguments\":[42, 24]}\u001E";
        ByteBuffer message = TestUtils.StringToByteBuffer(stringifiedMessage);
        TestBinder binder = new TestBinder(new InvocationMessage(null, "1", "test", new Object[] { 42, 24 }, null));

        List<HubMessage> messages = jsonHubProtocol.parseMessages(message, binder);
        
        assertNotNull(messages);
        assertEquals(1, messages.size());

        //We know it's only one message
        assertEquals(HubMessageType.INVOCATION, messages.get(0).getMessageType());

        InvocationMessage invocationMessage = (InvocationMessage)messages.get(0);
        assertEquals("test", invocationMessage.getTarget());
        assertEquals(null, invocationMessage.getInvocationId());
        int messageResult = (int) invocationMessage.getArguments()[0];
        int messageResult2 = (int) invocationMessage.getArguments()[1];
        assertEquals(42, messageResult);
        assertEquals(24, messageResult2);
    }

    @Test
    public void parseMessageWithOutOfOrderProperties() {
        String stringifiedMessage = "{\"arguments\":[42, 24],\"type\":1,\"target\":\"test\"}\u001E";
        ByteBuffer message = TestUtils.StringToByteBuffer(stringifiedMessage);
        TestBinder binder = new TestBinder(new InvocationMessage(null, "1", "test", new Object[] { 42, 24 }, null));

        List<HubMessage> messages = jsonHubProtocol.parseMessages(message, binder);
        
        assertNotNull(messages);
        assertEquals(1, messages.size());

        // We know it's only one message
        assertEquals(HubMessageType.INVOCATION, messages.get(0).getMessageType());

        InvocationMessage invocationMessage = (InvocationMessage) messages.get(0);
        assertEquals("test", invocationMessage.getTarget());
        assertEquals(null, invocationMessage.getInvocationId());
        int messageResult = (int) invocationMessage.getArguments()[0];
        int messageResult2 = (int) invocationMessage.getArguments()[1];
        assertEquals(42, messageResult);
        assertEquals(24, messageResult2);
    }

    @Test
    public void parseCompletionMessageWithOutOfOrderProperties() {
        String stringifiedMessage = "{\"type\":3,\"result\":42,\"invocationId\":\"1\"}\u001E";
        ByteBuffer message = TestUtils.StringToByteBuffer(stringifiedMessage);
        TestBinder binder = new TestBinder(new CompletionMessage(null, "1", 42, null));

        List<HubMessage> messages = jsonHubProtocol.parseMessages(message, binder);
        
        assertNotNull(messages);
        assertEquals(1, messages.size());

        // We know it's only one message
        assertEquals(HubMessageType.COMPLETION, messages.get(0).getMessageType());

        CompletionMessage completionMessage = (CompletionMessage) messages.get(0);
        assertEquals(null, completionMessage.getError());
        assertEquals(42 , completionMessage.getResult());
    }

    @Test
    public void invocationBindingFailureWhileParsingTooManyArgumentsWithOutOfOrderProperties() {
        String stringifiedMessage = "{\"arguments\":[42, 24],\"type\":1,\"target\":\"test\"}\u001E";
        ByteBuffer message = TestUtils.StringToByteBuffer(stringifiedMessage);
        TestBinder binder = new TestBinder(new InvocationMessage(null, null, "test", new Object[] { 42 }, null));

        List<HubMessage> messages = jsonHubProtocol.parseMessages(message, binder);
        
        assertNotNull(messages);
        assertEquals(1, messages.size());
        
        assertEquals(InvocationBindingFailureMessage.class, messages.get(0).getClass());
        InvocationBindingFailureMessage invocationBindingFailureMessage = (InvocationBindingFailureMessage)messages.get(0);
        assertEquals("Invocation provides 2 argument(s) but target expects 1.", invocationBindingFailureMessage.getException().getMessage());
    }

    @Test
    public void invocationBindingFailureWhileParsingTooManyArguments() {
        String stringifiedMessage = "{\"type\":1,\"target\":\"test\",\"arguments\":[42, 24]}\u001E";
        ByteBuffer message = TestUtils.StringToByteBuffer(stringifiedMessage);
        TestBinder binder = new TestBinder(new InvocationMessage(null, null, "test", new Object[] { 42 }, null));

        List<HubMessage> messages = jsonHubProtocol.parseMessages(message, binder);
        
        assertNotNull(messages);
        assertEquals(1, messages.size());
        
        assertEquals(InvocationBindingFailureMessage.class, messages.get(0).getClass());
        InvocationBindingFailureMessage invocationBindingFailureMessage = (InvocationBindingFailureMessage) messages.get(0);
        assertEquals("Invocation provides 2 argument(s) but target expects 1.", invocationBindingFailureMessage.getException().getMessage());
    }

    @Test
    public void invocationBindingFailureWhileParsingTooFewArguments() {
        String stringifiedMessage = "{\"type\":1,\"target\":\"test\",\"arguments\":[42]}\u001E";
        ByteBuffer message = TestUtils.StringToByteBuffer(stringifiedMessage);
        TestBinder binder = new TestBinder(new InvocationMessage(null, null, "test", new Object[] { 42, 24 }, null));

        List<HubMessage> messages = jsonHubProtocol.parseMessages(message, binder);
        
        assertNotNull(messages);
        assertEquals(1, messages.size());
        
        assertEquals(InvocationBindingFailureMessage.class, messages.get(0).getClass());
        InvocationBindingFailureMessage invocationBindingFailureMessage = (InvocationBindingFailureMessage) messages.get(0);
        assertEquals("Invocation provides 1 argument(s) but target expects 2.", invocationBindingFailureMessage.getException().getMessage());
    }

    @Test
    public void invocationBindingFailureWhenParsingIncorrectType() {
        String stringifiedMessage = "{\"type\":1,\"target\":\"test\",\"arguments\":[\"true\"]}\u001E";
        ByteBuffer message = TestUtils.StringToByteBuffer(stringifiedMessage);
        TestBinder binder = new TestBinder(new InvocationMessage(null, null, "test", new Object[] { 42 }, null));

        List<HubMessage> messages = jsonHubProtocol.parseMessages(message, binder);
        
        assertNotNull(messages);
        assertEquals(1, messages.size());
        
        assertEquals(InvocationBindingFailureMessage.class, messages.get(0).getClass());
        InvocationBindingFailureMessage invocationBindingFailureMessage = (InvocationBindingFailureMessage) messages.get(0);
        assertEquals("java.lang.NumberFormatException: For input string: \"true\"", invocationBindingFailureMessage.getException().getMessage());
    }

    @Test
    public void invocationBindingFailureStillReadsJsonPayloadAfterFailure() {
        String stringifiedMessage = "{\"type\":1,\"target\":\"test\",\"arguments\":[\"true\"],\"invocationId\":\"123\"}\u001E";
        ByteBuffer message = TestUtils.StringToByteBuffer(stringifiedMessage);
        TestBinder binder = new TestBinder(new InvocationMessage(null, null, "test", new Object[] { 42 }, null));

        List<HubMessage> messages = jsonHubProtocol.parseMessages(message, binder);
        
        assertNotNull(messages);
        assertEquals(1, messages.size());
        
        assertEquals(InvocationBindingFailureMessage.class, messages.get(0).getClass());
        InvocationBindingFailureMessage invocationBindingFailureMessage = (InvocationBindingFailureMessage) messages.get(0);
        assertEquals("java.lang.NumberFormatException: For input string: \"true\"", invocationBindingFailureMessage.getException().getMessage());
        assertEquals("123", invocationBindingFailureMessage.getInvocationId());
    }

    @Test
    public void errorWhileParsingIncompleteMessage() {
        String stringifiedMessage = "{\"type\":1,\"target\":\"test\",\"arguments\":";
        ByteBuffer message = TestUtils.StringToByteBuffer(stringifiedMessage);
        TestBinder binder = new TestBinder(new InvocationMessage(null, null, "test", new Object[] { 42, 24 }, null));

        RuntimeException exception = assertThrows(RuntimeException.class,
                () -> jsonHubProtocol.parseMessages(message, binder));
        assertEquals("Message is incomplete.", exception.getMessage());
    }

    private class TestBinder implements InvocationBinder {
        private Class<?>[] paramTypes = null;
        private Class<?> returnType = null;

        public TestBinder(HubMessage expectedMessage) {
            if (expectedMessage == null) {
                return;
            }

            switch (expectedMessage.getMessageType()) {
                case STREAM_INVOCATION:
                    ArrayList<Class<?>> streamTypes = new ArrayList<>();
                    for (Object obj : ((StreamInvocationMessage) expectedMessage).getArguments()) {
                        streamTypes.add(obj.getClass());
                    }
                    paramTypes = streamTypes.toArray(new Class<?>[streamTypes.size()]);
                    break;
                case INVOCATION:
                    ArrayList<Class<?>> types = new ArrayList<>();
                    for (Object obj : ((InvocationMessage) expectedMessage).getArguments()) {
                        types.add(obj.getClass());
                    }
                    paramTypes = types.toArray(new Class<?>[types.size()]);
                    break;
                case STREAM_ITEM:
                    break;
                case COMPLETION:
                    returnType = ((CompletionMessage)expectedMessage).getResult().getClass();
                    break;
                default:
                    break;
            }
        }

        @Override
        public Class<?> getReturnType(String invocationId) {
            return returnType;
        }

        @Override
        public List<Class<?>> getParameterTypes(String methodName) {
            if (paramTypes == null) {
                return new ArrayList<>();
            }
            return new ArrayList<Class<?>>(Arrays.asList(paramTypes));
        }
    }
}