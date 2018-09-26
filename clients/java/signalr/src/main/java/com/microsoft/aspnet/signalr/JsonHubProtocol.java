// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.aspnet.signalr;

import java.io.IOException;
import java.io.StringReader;
import java.util.ArrayList;
import java.util.List;

import com.google.gson.Gson;
import com.google.gson.JsonArray;
import com.google.gson.JsonElement;
import com.google.gson.JsonParser;
import com.google.gson.stream.JsonReader;
import com.google.gson.stream.JsonToken;

class JsonHubProtocol implements HubProtocol {
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
        return TransferFormat.TEXT;
    }

    @Override
    public HubMessage[] parseMessages(String payload, InvocationBinder binder) throws Exception {
        String[] messages = payload.split(RECORD_SEPARATOR);
        List<HubMessage> hubMessages = new ArrayList<>();
        for (String str : messages) {
            HubMessageType messageType = null;
            String invocationId = null;
            String target = null;
            String error = null;
            ArrayList<Object> arguments = null;
            JsonArray argumentsToken = null;
            Object result = null;
            JsonElement resultToken = null;

            JsonReader reader = new JsonReader(new StringReader(str));
            reader.beginObject();

            do {
                String name = reader.nextName();
                switch (name) {
                    case "type":
                        messageType = HubMessageType.values()[reader.nextInt() - 1];
                        break;
                    case "invocationId":
                        invocationId = reader.nextString();
                        break;
                    case "target":
                        target = reader.nextString();
                        break;
                    case "error":
                        error = reader.nextString();
                        break;
                    case "result":
                        if (invocationId == null) {
                            resultToken = jsonParser.parse(reader);
                        } else {
                            result = gson.fromJson(reader, binder.getReturnType(invocationId));
                        }
                        break;
                    case "item":
                        reader.skipValue();
                        break;
                    case "arguments":
                        if (target != null) {
                            List<Class<?>> types = binder.getParameterTypes(target);
                            arguments = bindArguments(reader, types);
                        } else {
                            argumentsToken = (JsonArray)jsonParser.parse(reader);
                        }
                        break;
                    case "headers":
                        throw new HubException("Headers not implemented yet.");
                    default:
                        // Skip unknown property, allows new clients to still work with old protocols
                        reader.skipValue();
                        break;
                }
            } while (reader.hasNext());

            reader.endObject();
            reader.close();

            switch (messageType) {
                case INVOCATION:
                    if (argumentsToken != null) {
                        List<Class<?>> types = binder.getParameterTypes(target);
                        arguments = bindArguments(argumentsToken, types);
                    }
                    if (arguments == null) {
                        hubMessages.add(new InvocationMessage(invocationId, target, new Object[0]));
                    } else {
                        hubMessages.add(new InvocationMessage(invocationId, target, arguments.toArray()));
                    }
                    break;
                case COMPLETION:
                    if (resultToken != null) {
                        result = gson.fromJson(resultToken, binder.getReturnType(invocationId));
                    }
                    hubMessages.add(new CompletionMessage(invocationId, result, error));
                    break;
                case STREAM_INVOCATION:
                case STREAM_ITEM:
                case CANCEL_INVOCATION:
                    throw new UnsupportedOperationException(String.format("The message type %s is not supported yet.", messageType));
                case PING:
                    hubMessages.add(PingMessage.getInstance());
                    break;
                case CLOSE:
                    if (error != null) {
                        hubMessages.add(new CloseMessage(error));
                    } else {
                        hubMessages.add(new CloseMessage());
                    }
                    break;
                default:
                    break;
            }
        }

        return hubMessages.toArray(new HubMessage[hubMessages.size()]);
    }

    @Override
    public String writeMessage(HubMessage hubMessage) {
        return gson.toJson(hubMessage) + RECORD_SEPARATOR;
    }

    private ArrayList<Object> bindArguments(JsonArray argumentsToken, List<Class<?>> paramTypes) {
        if (argumentsToken.size() != paramTypes.size()) {
            throw new RuntimeException(String.format("Invocation provides %d argument(s) but target expects %d.", argumentsToken.size(), paramTypes.size()));
        }

        ArrayList<Object> arguments = null;
        if (paramTypes.size() >= 1) {
            arguments = new ArrayList<>();
            for (int i = 0; i < paramTypes.size(); i++) {
                arguments.add(gson.fromJson(argumentsToken.get(i), paramTypes.get(i)));
            }
        }

        return arguments;
    }

    private ArrayList<Object> bindArguments(JsonReader reader, List<Class<?>> paramTypes) throws IOException {
        reader.beginArray();
        int paramCount = paramTypes.size();
        int argCount = 0;
        ArrayList<Object> arguments = new ArrayList<>();
        while (reader.peek() != JsonToken.END_ARRAY) {
            if (argCount < paramCount) {
                arguments.add(gson.fromJson(reader, paramTypes.get(argCount)));
            } else {
                reader.skipValue();
            }
            argCount++;
        }

        if (paramCount != argCount) {
            throw new RuntimeException(String.format("Invocation provides %d argument(s) but target expects %d.", argCount, paramCount));
        }

        reader.endArray();
        return arguments;
    }
}
