// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import static org.junit.jupiter.api.Assertions.*;

import java.lang.reflect.Type;
import java.nio.ByteBuffer;
import java.util.ArrayList;
import java.util.HashMap;
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
    public void verifyWriteMessage() {
        InvocationMessage invocationMessage = new InvocationMessage(null, null, "test", new Object[] {"42"}, null);
        String result = TestUtils.byteBufferToString(jsonHubProtocol.writeMessage(invocationMessage));
        String expectedResult = "{\"type\":1,\"target\":\"test\",\"arguments\":[\"42\"]}\u001E";
        assertEquals(expectedResult, result);
    }

    @Test
    public void parsePingMessage() {
        String stringifiedMessage = "{\"type\":6}\u001E";
        ByteBuffer message = TestUtils.stringToByteBuffer(stringifiedMessage);
        TestBinder binder = new TestBinder(null, null);

        List<HubMessage> messages = jsonHubProtocol.parseMessages(message, binder);

        //We know it's only one message
        assertNotNull(messages);
        assertEquals(1, messages.size());
        assertEquals(HubMessageType.PING, messages.get(0).getMessageType());
    }

    @Test
    public void parseCloseMessage() {
        String stringifiedMessage = "{\"type\":7}\u001E";
        ByteBuffer message = TestUtils.stringToByteBuffer(stringifiedMessage);
        TestBinder binder = new TestBinder(null, null);

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
        ByteBuffer message = TestUtils.stringToByteBuffer(stringifiedMessage);
        TestBinder binder = new TestBinder(new Type[] { int.class }, null);

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
        ByteBuffer message = TestUtils.stringToByteBuffer(stringifiedMessage);
        TestBinder binder = new TestBinder(new Type[] { int.class }, null);

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
        ByteBuffer message = TestUtils.stringToByteBuffer(stringifiedMessage);
        TestBinder binder = new TestBinder(new Type[] { int.class }, null);

        Throwable exception = assertThrows(UnsupportedOperationException.class, () -> jsonHubProtocol.parseMessages(message, binder));
        assertEquals("The message type STREAM_INVOCATION is not supported yet.", exception.getMessage());
    }

    @Test
    public void parseSingleUnsupportedCancelInvocationMessage() {
        String stringifiedMessage = "{\"type\":5,\"invocationId\":123}\u001E";
        ByteBuffer message = TestUtils.stringToByteBuffer(stringifiedMessage);
        TestBinder binder = new TestBinder(null, null);

        Throwable exception = assertThrows(UnsupportedOperationException.class, () -> jsonHubProtocol.parseMessages(message, binder));
        assertEquals("The message type CANCEL_INVOCATION is not supported yet.", exception.getMessage());
    }

    @Test
    public void parseTwoMessages() {
        String stringifiedMessage = "{\"type\":1,\"target\":\"one\",\"arguments\":[42]}\u001E{\"type\":1,\"target\":\"two\",\"arguments\":[43]}\u001E";
        ByteBuffer message = TestUtils.stringToByteBuffer(stringifiedMessage);
        TestBinder binder = new TestBinder(new Type[] { int.class }, null);

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
        ByteBuffer message = TestUtils.stringToByteBuffer(stringifiedMessage);
        TestBinder binder = new TestBinder(new Type[] { int.class, int.class }, null);

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
    public void parseSingleMessageNestedCollection() {
        String stringifiedMessage = "{\"type\":1,\"target\":\"test\",\"arguments\":[[{\"one\":[\"a\",\"b\"],\"two\":[\"\uBEEF\",\"\uABCD\"]},{\"four\":[\"^\",\"*\"],\"three\":[\"5\",\"9\"]}]]}\u001E";
        ByteBuffer message = TestUtils.stringToByteBuffer(stringifiedMessage);
        TestBinder binder = new TestBinder(new Type[] { (new TypeReference<ArrayList<HashMap<String, ArrayList<Character>>>>() { }).getType() }, null);

        List<HubMessage> messages = jsonHubProtocol.parseMessages(message, binder);
        
        assertNotNull(messages);
        assertEquals(1, messages.size());

        //We know it's only one message
        assertEquals(HubMessageType.INVOCATION, messages.get(0).getMessageType());

        InvocationMessage invocationMessage = (InvocationMessage)messages.get(0);
		
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
    public void parseSingleMessageCustomPojoArg() {
        String stringifiedMessage = "{\"type\":1,\"target\":\"test\",\"arguments\":[{\"firstName\":\"John\",\"lastName\":\"Doe\",\"age\":30,\"t\":[5,8]}]}\u001E";
        ByteBuffer message = TestUtils.stringToByteBuffer(stringifiedMessage);
        
        TestBinder binder = new TestBinder(new Type[] { (new TypeReference<PersonPojo<ArrayList<Short>>>() { }).getType() }, null);

        List<HubMessage> messages = jsonHubProtocol.parseMessages(message, binder);

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
    public void parseMessageWithOutOfOrderProperties() {
        String stringifiedMessage = "{\"arguments\":[42, 24],\"type\":1,\"target\":\"test\"}\u001E";
        ByteBuffer message = TestUtils.stringToByteBuffer(stringifiedMessage);
        TestBinder binder = new TestBinder(new Type[] { int.class, int.class }, null);

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
        ByteBuffer message = TestUtils.stringToByteBuffer(stringifiedMessage);
        TestBinder binder = new TestBinder(null, int.class);

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
        ByteBuffer message = TestUtils.stringToByteBuffer(stringifiedMessage);
        TestBinder binder = new TestBinder(new Type[] { int.class }, null);

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
        ByteBuffer message = TestUtils.stringToByteBuffer(stringifiedMessage);
        TestBinder binder = new TestBinder(new Type[] { int.class }, null);

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
        ByteBuffer message = TestUtils.stringToByteBuffer(stringifiedMessage);
        TestBinder binder = new TestBinder(new Type[] { int.class, int.class }, null);

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
        ByteBuffer message = TestUtils.stringToByteBuffer(stringifiedMessage);
        TestBinder binder = new TestBinder(new Type[] { int.class }, null);

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
        ByteBuffer message = TestUtils.stringToByteBuffer(stringifiedMessage);
        TestBinder binder = new TestBinder(new Type[] { int.class }, null);

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
        ByteBuffer message = TestUtils.stringToByteBuffer(stringifiedMessage);
        TestBinder binder = new TestBinder(new Type[] { int.class, int.class }, null);

        RuntimeException exception = assertThrows(RuntimeException.class,
                () -> jsonHubProtocol.parseMessages(message, binder));
        assertEquals("Message is incomplete.", exception.getMessage());
    }
}