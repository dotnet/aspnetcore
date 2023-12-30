// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import java.io.IOException;
import java.io.StringReader;
import java.lang.reflect.Type;
import java.nio.ByteBuffer;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.List;

import com.google.gson.Gson;
import com.google.gson.JsonArray;
import com.google.gson.JsonElement;
import com.google.gson.JsonParser;
import com.google.gson.stream.JsonReader;
import com.google.gson.stream.JsonToken;

public final class GsonHubProtocol implements HubProtocol {
    private final Gson gson;
    private static final String RECORD_SEPARATOR = "\u001e";

    public GsonHubProtocol() {
        this(new Gson());
    }

    public GsonHubProtocol(Gson gson) {
        this.gson = gson;
    }

    @Override
    public String getName() {
        return "json";
    }

    @Override
    public int getVersion() {
        return 1;
    }

    @Override
    public List<HubMessage> parseMessages(ByteBuffer payload, InvocationBinder binder) {
        String payloadStr;
        // If the payload is readOnly, we have to copy the bytes from its array to make the payload string
        if (payload.isReadOnly()) {
            byte[] payloadBytes = new byte[payload.remaining()];
            payload.get(payloadBytes, 0, payloadBytes.length);
            payloadStr = new String(payloadBytes, StandardCharsets.UTF_8);
        // Otherwise we can allocate directly from its array
        } else {
            // The position of the ByteBuffer may have been incremented - make sure we only grab the remaining bytes
            payloadStr = new String(payload.array(), payload.position(), payload.remaining(), StandardCharsets.UTF_8);
        }
        if (payloadStr.length() == 0) {
            return null;
        }
        if (!(payloadStr.substring(payloadStr.length() - 1).equals(RECORD_SEPARATOR))) {
            throw new RuntimeException("Message is incomplete.");
        }

        String[] messages = payloadStr.split(RECORD_SEPARATOR);
        List<HubMessage> hubMessages = new ArrayList<>();
        try {
            for (String str : messages) {
                HubMessageType messageType = null;
                String invocationId = null;
                String target = null;
                String error = null;
                ArrayList<Object> arguments = null;
                JsonArray argumentsToken = null;
                Object result = null;
                Exception argumentBindingException = null;
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
                        case "item":
                            if (invocationId == null || binder.getReturnType(invocationId) == null) {
                                resultToken = JsonParser.parseReader(reader);
                            } else {
                                result = gson.fromJson(reader, binder.getReturnType(invocationId));
                            }
                            break;
                        case "arguments":
                            if (target != null) {
                                boolean startedArray = false;
                                try {
                                    List<Type> types = binder.getParameterTypes(target);
                                    startedArray = true;
                                    arguments = bindArguments(reader, types);
                                } catch (Exception ex) {
                                    argumentBindingException = ex;

                                    // Could be at any point in argument array JSON when an error is thrown
                                    // Read until the end of the argument JSON array
                                    if (!startedArray) {
                                        reader.beginArray();
                                    }
                                    while (reader.hasNext()) {
                                        reader.skipValue();
                                    }
                                    if (reader.peek() == JsonToken.END_ARRAY) {
                                        reader.endArray();
                                    }
                                }
                            } else {
                                argumentsToken = (JsonArray)JsonParser.parseReader(reader);
                            }
                            break;
                        case "headers":
                            throw new RuntimeException("Headers not implemented yet.");
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
                            try {
                                List<Type> types = binder.getParameterTypes(target);
                                arguments = bindArguments(argumentsToken, types);
                            } catch (Exception ex) {
                                argumentBindingException = ex;
                            }
                        }
                        if (argumentBindingException != null) {
                            hubMessages.add(new InvocationBindingFailureMessage(invocationId, target, argumentBindingException));
                        } else {
                            if (arguments == null) {
                                hubMessages.add(new InvocationMessage(null, invocationId, target, new Object[0], null));
                            } else {
                                hubMessages.add(new InvocationMessage(null, invocationId, target, arguments.toArray(), null));
                            }
                        }
                        break;
                    case COMPLETION:
                        if (resultToken != null) {
                            Type returnType = binder.getReturnType(invocationId);
                            result = gson.fromJson(resultToken, returnType != null ? returnType : Object.class);
                        }
                        hubMessages.add(new CompletionMessage(null, invocationId, result, error));
                        break;
                    case STREAM_ITEM:
                        if (resultToken != null) {
                            Type returnType = binder.getReturnType(invocationId);
                            result = gson.fromJson(resultToken, returnType != null ? returnType : Object.class);
                        }
                        hubMessages.add(new StreamItem(null, invocationId, result));
                        break;
                    case STREAM_INVOCATION:
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
        } catch (IOException ex) {
            throw new RuntimeException("Error reading JSON.", ex);
        }

        return hubMessages;
    }

    @Override
    public ByteBuffer writeMessage(HubMessage hubMessage) {
        return ByteBuffer.wrap((gson.toJson(hubMessage) + RECORD_SEPARATOR).getBytes(StandardCharsets.UTF_8));
    }

    private ArrayList<Object> bindArguments(JsonArray argumentsToken, List<Type> paramTypes) {
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

    private ArrayList<Object> bindArguments(JsonReader reader, List<Type> paramTypes) throws IOException {
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

        // Do this at the very end, because if we throw for any reason above, we catch at the call site
        // And manually consume the rest of the array, if we called endArray before throwing the RuntimeException
        // Then we can't correctly consume the rest of the json object
        reader.endArray();

        return arguments;
    }
}
