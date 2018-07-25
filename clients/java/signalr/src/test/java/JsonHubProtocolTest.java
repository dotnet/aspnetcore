// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import com.google.gson.JsonArray;
import org.junit.Rule;
import org.junit.Test;
import org.junit.rules.ExpectedException;

import static org.junit.Assert.*;

public class JsonHubProtocolTest {
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
        assertEquals(TransferFormat.Text, jsonHubProtocol.getTransferFormat());
    }

    @Test
    public void VerifyWriteMessage() {
        InvocationMessage invocationMessage = new InvocationMessage("test", new Object[] {"42"});
        String result = jsonHubProtocol.writeMessage(invocationMessage);
        String expectedResult = "{\"type\":1,\"target\":\"test\",\"arguments\":[\"42\"]}\u001E";
        assertEquals(expectedResult, result);
    }

    @Test
    public void ParsePingMessage() {
        String stringifiedMessage = "{\"type\":6}\u001E";
        HubMessage[] messages = jsonHubProtocol.parseMessages(stringifiedMessage);

        //We know it's only one message
        assertEquals(1, messages.length);
        assertEquals(HubMessageType.PING, messages[0].getMessageType());
    }

    @Test
    public void ParseSingleMessage() {
        String stringifiedMessage = "{\"type\":1,\"target\":\"test\",\"arguments\":[42]}\u001E";
        HubMessage[] messages = jsonHubProtocol.parseMessages(stringifiedMessage);

        //We know it's only one message
        assertEquals(1, messages.length);

        assertEquals(HubMessageType.INVOCATION, messages[0].getMessageType());

        //We can safely cast here because we know that it's an invocation message.
        InvocationMessage invocationMessage = (InvocationMessage) messages[0];

        assertEquals("test", invocationMessage.target);
        assertEquals(null, invocationMessage.invocationId);

        JsonArray messageResult = (JsonArray) invocationMessage.arguments[0];
        assertEquals(42, messageResult.getAsInt());
    }

    @Rule
    public ExpectedException exceptionRule = ExpectedException.none();

    @Test
    public void ParseSingleUnsupportedStreamItemMessage() {
        exceptionRule.expect(UnsupportedOperationException.class);
        exceptionRule.expectMessage("Support for streaming is not yet available");
        String stringifiedMessage = "{\"type\":2,\"Id\":1,\"Item\":42}\u001E";
        HubMessage[] messages = jsonHubProtocol.parseMessages(stringifiedMessage);
    }

    @Test
    public void ParseSingleUnsupportedStreamInvocationMessage() {
        exceptionRule.expect(UnsupportedOperationException.class);
        exceptionRule.expectMessage("Support for streaming is not yet available");
        String stringifiedMessage = "{\"type\":4,\"Id\":1,\"target\":\"test\",\"arguments\":[42]}\u001E";
        HubMessage[] messages = jsonHubProtocol.parseMessages(stringifiedMessage);
    }

    @Test
    public void ParseTwoMessages() {
        String twoMessages = "{\"type\":1,\"target\":\"one\",\"arguments\":[42]}\u001E{\"type\":1,\"target\":\"two\",\"arguments\":[43]}\u001E";
        HubMessage[] messages = jsonHubProtocol.parseMessages(twoMessages);
        assertEquals(2, messages.length);

        // Check the first message
        assertEquals(HubMessageType.INVOCATION, messages[0].getMessageType());

        //Now that we know we have an invocation message we can cast the hubMessage.
        InvocationMessage invocationMessage = (InvocationMessage) messages[0];

        assertEquals("one", invocationMessage.target);
        assertEquals(null, invocationMessage.invocationId);
        JsonArray messageResult = (JsonArray) invocationMessage.arguments[0];
        assertEquals(42, messageResult.getAsInt());

        // Check the second message
        assertEquals(HubMessageType.INVOCATION, messages[1].getMessageType());

        //Now that we know we have an invocation message we can cast the hubMessage.
        InvocationMessage invocationMessage2 = (InvocationMessage) messages[1];

        assertEquals("two", invocationMessage2.target);
        assertEquals(null, invocationMessage2.invocationId);
        JsonArray secondMessageResult = (JsonArray) invocationMessage2.arguments[0];
        assertEquals(43, secondMessageResult.getAsInt());
    }

    @Test
    public void ParseSingleMessageMutipleArgs() {
        String stringifiedMessage = "{\"type\":1,\"target\":\"test\",\"arguments\":[42, 24]}\u001E";
        HubMessage[] messages = jsonHubProtocol.parseMessages(stringifiedMessage);

        //We know it's only one message
        assertEquals(HubMessageType.INVOCATION, messages[0].getMessageType());

        InvocationMessage message = (InvocationMessage)messages[0];
        assertEquals("test", message.target);
        assertEquals(null, message.invocationId);
        JsonArray messageResult = ((JsonArray) message.arguments[0]);
        assertEquals(42, messageResult.get(0).getAsInt());
        assertEquals(24, messageResult.get(1).getAsInt());
    }
}