// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.aspnet.signalr;

import com.google.gson.Gson;
import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;

import java.util.ArrayList;
import java.util.List;

public class JsonHubProtocol implements HubProtocol {
    private final JsonParser jsonParser = new JsonParser();
    private final Gson gson = new Gson();
    private static final String RECORD_SEPARATOR = "\u001e";

    @Override
    public String getName() {
        return "json";
    }

    @Override
    public int getVersion() {
        return 1;
    }

    @Override
    public TransferFormat getTransferFormat() {
        return TransferFormat.Text;
    }

    @Override
    public HubMessage[] parseMessages(String payload) {
        String[] messages = payload.split(RECORD_SEPARATOR);
        List<HubMessage> hubMessages = new ArrayList<>();
        for (String splitMessage : messages) {

            JsonObject jsonMessage = jsonParser.parse(splitMessage).getAsJsonObject();
            HubMessageType messageType = HubMessageType.values()[jsonMessage.get("type").getAsInt() -1];
            switch (messageType) {
                case INVOCATION:
                    //Invocation Message
                    String target = jsonMessage.get("target").getAsString();
                    JsonElement args = jsonMessage.get("arguments");
                    hubMessages.add(new InvocationMessage(target, new Object[] {args}));
                    break;
                case STREAM_ITEM:
                    throw new UnsupportedOperationException("Support for streaming is not yet available");
                case COMPLETION:
                    //Don't care yet
                    break;
                case STREAM_INVOCATION:
                    //Don't care yet;
                    throw new UnsupportedOperationException("Support for streaming is not yet available");
                case CANCEL_INVOCATION:
                    // Not tracking invocations yet
                    break;
                case PING:
                    //Ping
                    hubMessages.add(new PingMessage());
                    break;
                case CLOSE:
                    CloseMessage closeMessage;
                    if (jsonMessage.has("error")){
                        String error = jsonMessage.get("error").getAsString();
                        closeMessage = new CloseMessage(error);
                    } else {
                        closeMessage = new CloseMessage();
                    }
                    hubMessages.add(closeMessage);
                    break;
            }
        }
        return hubMessages.toArray(new HubMessage[hubMessages.size()]);
    }

    @Override
    public String writeMessage(HubMessage hubMessage) {
        return gson.toJson(hubMessage) + RECORD_SEPARATOR;
    }
}
