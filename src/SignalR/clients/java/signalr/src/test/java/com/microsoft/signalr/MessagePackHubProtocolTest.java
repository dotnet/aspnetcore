package com.microsoft.signalr;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertThrows;

import java.nio.ByteBuffer;
import java.util.List;
import okio.ByteString;

import org.junit.jupiter.api.Test;

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
    public void checkTransferFormat() {
        assertEquals(TransferFormat.BINARY, messagePackHubProtocol.getTransferFormat());
    }

    @Test
    public void verifyWriteMessage() {
        InvocationMessage invocationMessage = new InvocationMessage(null, null, "test", new Object[] { 42 }, null);
        ByteBuffer result = messagePackHubProtocol.writeMessage(invocationMessage);
        byte[] expectedBytes = {0x0C, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA4, 0x74, 0x65, 0x73, 
            0x74, (byte) 0x91, 0x2A, (byte) 0x90};
        ByteString expectedResult = ByteString.of(expectedBytes);
        assertEquals(expectedResult, ByteString.of(result));
    }

    @Test
    public void parsePingMessage() {
        byte[] messageBytes = {0x02, (byte) 0x91, 0x06};
        ByteBuffer message = ByteBuffer.wrap(messageBytes);
        TestBinder binder = new TestBinder(PingMessage.getInstance());

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
        TestBinder binder = new TestBinder(new CloseMessage());

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
        TestBinder binder = new TestBinder(new CloseMessage("Error"));

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
        TestBinder binder = new TestBinder(new InvocationMessage(null, "1", "test", new Object[] { 42 }, null));

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
    public void parseSingleStreamInvocationMessage() {
        byte[] messageBytes = {0x12, (byte) 0x96, 0x04, (byte) 0x80, (byte) 0xA6, 0x6D, 0x65, 0x74, 0x68, 0x6F, 0x64, 
            (byte) 0xA4, 0x74, 0x65, 0x73, 0x74, (byte) 0x91, 0x2A, (byte) 0x90};
        ByteBuffer message = ByteBuffer.wrap(messageBytes);
        TestBinder binder = new TestBinder(new StreamInvocationMessage(null, "method", "test", new Object[] { 42 }, null));

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
        TestBinder binder = new TestBinder(new CancelInvocationMessage(null, "method"));

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
        TestBinder binder = new TestBinder(new InvocationMessage(null, null, "one", new Object[] { 42 }, null));

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
        TestBinder binder = new TestBinder(new InvocationMessage(null, null, "test", new Object[] { 42, "42" }, null));

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
        TestBinder binder = new TestBinder(new InvocationMessage(null, null, "test", new Object[] { 42 }, null));

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
        TestBinder binder = new TestBinder(new InvocationMessage(null, null, "test", new Object[] { 42, 24 }, null));

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
        TestBinder binder = new TestBinder(new InvocationMessage(null, null, "test", new Object[] { 42 }, null));

        List<HubMessage> messages = messagePackHubProtocol.parseMessages(message, binder);
        
        assertNotNull(messages);
        assertEquals(1, messages.size());
        
        assertEquals(HubMessageType.INVOCATION_BINDING_FAILURE, messages.get(0).getMessageType());
        InvocationBindingFailureMessage invocationBindingFailureMessage = (InvocationBindingFailureMessage) messages.get(0);
        assertEquals("Cannot cast java.lang.Boolean to java.lang.Integer", invocationBindingFailureMessage.getException().getMessage());
    }

    @Test
    public void invocationBindingFailureReadsNextMessageAfterTooManyArguments() {
        byte[] messageBytes = {0x0F, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA4, 0x74, 0x65, 0x73, 0x74, (byte) 0x92, 
            0x2A, (byte) 0xA2, 0x34, 0x32, (byte) 0x90, 0x0B, (byte) 0x96, 0x01, (byte) 0x80, (byte) 0xC0, (byte) 0xA3, 0x74, 
            0x77, 0x6F, (byte) 0x91, 0x2B, (byte) 0x90};
        ByteBuffer message = ByteBuffer.wrap(messageBytes);
        TestBinder binder = new TestBinder(new InvocationMessage(null, null, "test", new Object[] { 42 }, null));

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
        TestBinder binder = new TestBinder(new InvocationMessage(null, null, "test", new Object[] { 42, 24 }, null));

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
        TestBinder binder = new TestBinder(new InvocationMessage(null, null, "test", new Object[] { 42 }, null));

        List<HubMessage> messages = messagePackHubProtocol.parseMessages(message, binder);
        
        assertNotNull(messages);
        assertEquals(2, messages.size());
        
        assertEquals(HubMessageType.INVOCATION_BINDING_FAILURE, messages.get(0).getMessageType());
        InvocationBindingFailureMessage invocationBindingFailureMessage = (InvocationBindingFailureMessage) messages.get(0);
        assertEquals("Cannot cast java.lang.Boolean to java.lang.Integer", invocationBindingFailureMessage.getException().getMessage());
        
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

}
