// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import java.io.IOException;
import java.math.BigInteger;
import java.nio.ByteBuffer;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collection;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

import org.msgpack.core.MessageBufferPacker;
import org.msgpack.core.MessageFormat;
import org.msgpack.core.MessagePack;
import org.msgpack.core.MessagePackException;
import org.msgpack.core.MessagePacker;
import org.msgpack.core.MessageUnpacker;
import org.msgpack.value.ValueType;

class MessagePackHubProtocol implements HubProtocol {
    
    private static final int ERROR_RESULT = 1;
    private static final int VOID_RESULT = 2;
    private static final int NON_VOID_RESULT = 3;

    @Override
    public String getName() {
        return "messagepack";
    }

    @Override
    public int getVersion() {
        return 1;
    }

    @Override
    public TransferFormat getTransferFormat() {
        return TransferFormat.BINARY;
    }

    @Override
    public List<HubMessage> parseMessages(ByteBuffer payload, InvocationBinder binder) {
        if (payload.remaining() == 0) {
            return null;
        }

        List<HubMessage> hubMessages = new ArrayList<>();
        
        try {
            while (payload.hasRemaining()) {
                int length = Utils.readLengthHeader(payload);
                // Throw if remaining buffer is shorter than length header
                if (payload.remaining() < length) {
                    throw new RuntimeException(String.format("MessagePack message was length %d but claimed to be length %d.", payload.remaining(), length));
                }
                // Instantiate MessageUnpacker
                MessageUnpacker unpacker = MessagePack.newDefaultUnpacker(payload);
                
                int itemCount = unpacker.unpackArrayHeader();
                HubMessageType messageType = HubMessageType.values()[unpacker.unpackInt() - 1];
                
                switch (messageType) {
                    case INVOCATION:
                        hubMessages.add(createInvocationMessage(unpacker, binder, itemCount));
                        break;
                    case STREAM_ITEM:
                        hubMessages.add(createStreamItemMessage(unpacker, binder));
                        break;
                    case COMPLETION:
                        hubMessages.add(createCompletionMessage(unpacker, binder));
                        break;
                    case STREAM_INVOCATION:
                        hubMessages.add(createStreamInvocationMessage(unpacker, binder, itemCount));
                        break;
                    case CANCEL_INVOCATION:
                        hubMessages.add(createCancelInvocationMessage(unpacker));
                        break;
                    case PING:
                        hubMessages.add(PingMessage.getInstance());
                        break;
                    case CLOSE:
                        hubMessages.add(createCloseMessage(unpacker, itemCount));
                        break;
                    default:
                        break;
                }
                // Make sure that we actually read the right number of bytes
                int readBytes = (int) unpacker.getTotalReadBytes();
                if (readBytes != length) {
                    throw new RuntimeException(String.format("MessagePack message was length %d but claimed to be length %d.", readBytes, length));
                }
                unpacker.close();
                payload.position(payload.position() + readBytes);
            }
        } catch (MessagePackException | IOException ex) {
            throw new RuntimeException("Error reading MessagePack data.", ex);
        }

        return hubMessages;
    }
    
    @Override
    public ByteBuffer writeMessage(HubMessage hubMessage) {
        HubMessageType messageType = hubMessage.getMessageType();
        
        try {
            byte[] message;
            switch (messageType) {
                case INVOCATION:
                    message = writeInvocationMessage((InvocationMessage) hubMessage);
                    break;
                case STREAM_ITEM:
                    message = writeStreamItemMessage((StreamItem) hubMessage);
                    break;
                case COMPLETION:
                    message = writeCompletionMessage((CompletionMessage) hubMessage);
                    break;
                case STREAM_INVOCATION:
                    message = writeStreamInvocationMessage((StreamInvocationMessage) hubMessage);
                    break;
                case CANCEL_INVOCATION:
                    message = writeCancelInvocationMessage((CancelInvocationMessage) hubMessage);
                    break;
                case PING:
                    message = writePingMessage((PingMessage) hubMessage);
                    break;
                case CLOSE:
                    message = writeCloseMessage((CloseMessage) hubMessage);
                    break;
                default:
                    throw new RuntimeException(String.format("Unexpected message type: %d", messageType.value));
            }
            int length = message.length;
            List<Byte> header = Utils.getLengthHeader(length);
            byte[] messageWithHeader = new byte[header.size() + length];
            int headerSize = header.size();
            
            // Write the length header, then all of the bytes of the original message
            for (int i = 0; i < headerSize; i++) {
                messageWithHeader[i] = header.get(i);
            }
            for (int i = 0; i < length; i++) {
                messageWithHeader[i + headerSize] = message[i];
            }
        
            return ByteBuffer.wrap(messageWithHeader);
        } catch (MessagePackException | IOException ex) {
            throw new RuntimeException("Error writing MessagePack data.", ex);
        }
    }
        
    private HubMessage createInvocationMessage(MessageUnpacker unpacker, InvocationBinder binder, int itemCount) throws IOException {
        Map<String, String> headers = readHeaders(unpacker);
        
        // invocationId may be nil
        String invocationId = null;
        if (!unpacker.tryUnpackNil()) {
            invocationId = unpacker.unpackString();
        }
        
        // For MsgPack, we represent an empty invocation ID as an empty string,
        // so we need to normalize that to "null", which is what indicates a non-blocking invocation.
        if (invocationId == null || invocationId.isEmpty()) {
            invocationId = null;
        }
            
        String target = unpacker.unpackString();
        
        Object[] arguments = null;
        try {
            List<Class<?>> types = binder.getParameterTypes(target);
            arguments = bindArguments(unpacker, types);
        } catch (Exception ex) {
            return new InvocationBindingFailureMessage(invocationId, target, ex);
        }
        
        Collection<String> streams = null;
        // Older implementations may not send the streamID array
        if (itemCount > 5) {
            streams = readStreamIds(unpacker);
        }
        
        return new InvocationMessage(headers, invocationId, target, arguments, streams);
    }
    
    private HubMessage createStreamItemMessage(MessageUnpacker unpacker, InvocationBinder binder) throws IOException {
        Map<String, String> headers = readHeaders(unpacker);
        String invocationId = unpacker.unpackString();
        Object value;
        try {
            Class<?> itemType = binder.getReturnType(invocationId);
            value = readValue(unpacker, itemType);
        } catch (Exception ex) {
            return new StreamBindingFailureMessage(invocationId, ex);
        }
        
        return new StreamItem(headers, invocationId, value);
    }
    
    private HubMessage createCompletionMessage(MessageUnpacker unpacker, InvocationBinder binder) throws IOException {
        Map<String, String> headers = readHeaders(unpacker);
        String invocationId = unpacker.unpackString();
        int resultKind = unpacker.unpackInt();
        
        String error = null;
        Object result = null;
        
        switch (resultKind) {
            case ERROR_RESULT:
                error = unpacker.unpackString();
                break;
            case VOID_RESULT:
                break;
            case NON_VOID_RESULT:
                Class<?> itemType = binder.getReturnType(invocationId);
                result = readValue(unpacker, itemType);
                break;
            default:
                throw new RuntimeException("Invalid invocation result kind.");
        }
        
        return new CompletionMessage(headers, invocationId, result, error);
    }
    
    private HubMessage createStreamInvocationMessage(MessageUnpacker unpacker, InvocationBinder binder, int itemCount) throws IOException {
        Map<String, String> headers = readHeaders(unpacker);
        String invocationId = unpacker.unpackString();
        String target = unpacker.unpackString();
        
        Object[] arguments = null;
        try {
            List<Class<?>> types = binder.getParameterTypes(target);
            arguments = bindArguments(unpacker, types);
        } catch (Exception ex) {
            return new InvocationBindingFailureMessage(invocationId, target, ex);
        }
        
        Collection<String> streams = readStreamIds(unpacker);
        
        return new StreamInvocationMessage(headers, invocationId, target, arguments, streams);
    }
    
    private HubMessage createCancelInvocationMessage(MessageUnpacker unpacker) throws IOException {
        Map<String, String> headers = readHeaders(unpacker);
        String invocationId = unpacker.unpackString();
        
        return new CancelInvocationMessage(headers, invocationId);
    }
    
    private HubMessage createCloseMessage(MessageUnpacker unpacker, int itemCount) throws IOException {
        String error = unpacker.unpackString();
        boolean allowReconnect = false;
        
        if (itemCount > 2) {
            allowReconnect = unpacker.unpackBoolean();
        }
        
        return new CloseMessage(error, allowReconnect);
    }
    
    private byte[] writeInvocationMessage(InvocationMessage message) throws IOException {
        MessageBufferPacker packer = MessagePack.newDefaultBufferPacker();
        
        packer.packArrayHeader(6);
        packer.packInt(message.getMessageType().value);
        
        writeHeaders(message.getHeaders(), packer);
        
        String invocationId = message.getInvocationId();
        if (invocationId != null && !invocationId.isEmpty()) {
            packer.packString(invocationId);
        } else {
            packer.packNil();
        }
        
        packer.packString(message.getTarget());
        
        Object[] arguments = message.getArguments();
        packer.packArrayHeader(arguments.length);
        
        for (Object o: arguments) {
            writeValue(o, packer);
        }
        
        writeStreamIds(message.getStreamIds(), packer);
        
        packer.flush();
        byte[] content = packer.toByteArray();
        packer.close();
        return content;
    }
    
    private byte[] writeStreamItemMessage(StreamItem message) throws IOException {
        MessageBufferPacker packer = MessagePack.newDefaultBufferPacker();
        
        packer.packArrayHeader(4);
        packer.packInt(message.getMessageType().value);
        
        writeHeaders(message.getHeaders(), packer);
        
        packer.packString(message.getInvocationId());
        
        writeValue(message.getItem(), packer);
        
        packer.flush();
        byte[] content = packer.toByteArray();
        packer.close();
        return content;
    }
    
    private byte[] writeCompletionMessage(CompletionMessage message) throws IOException {
        MessageBufferPacker packer = MessagePack.newDefaultBufferPacker();
        int resultKind =
            message.getError() != null ? ERROR_RESULT :
            message.getResult() != null ? NON_VOID_RESULT :
            VOID_RESULT;
        
        packer.packArrayHeader(4 + (resultKind != VOID_RESULT ? 1: 0));
        packer.packInt(message.getMessageType().value);
        
        writeHeaders(message.getHeaders(), packer);
        
        packer.packString(message.getInvocationId());
        packer.packInt(resultKind);
        
        switch (resultKind) {
        case ERROR_RESULT:
            packer.packString(message.getError());
            break;
        case NON_VOID_RESULT:
            writeValue(message.getResult(), packer);
            break;
        }
        
        packer.flush();
        byte[] content = packer.toByteArray();
        packer.close();
        return content;
    }
    
    private byte[] writeStreamInvocationMessage(StreamInvocationMessage message) throws IOException {
        MessageBufferPacker packer = MessagePack.newDefaultBufferPacker();
        
        packer.packArrayHeader(6);
        packer.packInt(message.getMessageType().value);
        
        writeHeaders(message.getHeaders(), packer);
        
        packer.packString(message.getInvocationId());
        packer.packString(message.getTarget());
        
        Object[] arguments = message.getArguments();
        packer.packArrayHeader(arguments.length);
        
        for (Object o: arguments) {
            writeValue(o, packer);
        }
        
        writeStreamIds(message.getStreamIds(), packer);
        
        packer.flush();
        byte[] content = packer.toByteArray();
        packer.close();
        return content;
    }
    
    private byte[] writeCancelInvocationMessage(CancelInvocationMessage message) throws IOException {
        MessageBufferPacker packer = MessagePack.newDefaultBufferPacker();
        
        packer.packArrayHeader(3);
        packer.packInt(message.getMessageType().value);
        
        writeHeaders(message.getHeaders(), packer);
        
        packer.packString(message.getInvocationId());
        
        packer.flush();
        byte[] content = packer.toByteArray();
        packer.close();
        return content;
    }
    
    private byte[] writePingMessage(PingMessage message) throws IOException {
        MessageBufferPacker packer = MessagePack.newDefaultBufferPacker();
        
        packer.packArrayHeader(1);
        packer.packInt(message.getMessageType().value);
        
        packer.flush();
        byte[] content = packer.toByteArray();
        packer.close();
        return content;
    }
    
    private byte[] writeCloseMessage(CloseMessage message) throws IOException {
        MessageBufferPacker packer = MessagePack.newDefaultBufferPacker();
        
        packer.packArrayHeader(3);
        packer.packInt(message.getMessageType().value);
        
        String error = message.getError();
        if (error != null && !error.isEmpty()) {
            packer.packString(error);
        } else {
            packer.packNil();
        }
        
        packer.packBoolean(message.getAllowReconnect());
        
        packer.flush();
        byte[] content = packer.toByteArray();
        packer.close();
        return content;
    }
    
    private Map<String, String> readHeaders(MessageUnpacker unpacker) throws IOException {
        int headerCount = unpacker.unpackMapHeader();
        if (headerCount > 0) {
            Map<String, String> headers = new HashMap<String, String>();
            for (int i = 0; i < headerCount; i++) {
                headers.put(unpacker.unpackString(), unpacker.unpackString());
            }
            return headers;
        } else {
            return null;
        }
    }

    private void writeHeaders(Map<String, String> headers, MessagePacker packer) throws IOException {
        if (headers != null) {
            packer.packMapHeader(headers.size());
            for (String k: headers.keySet()) {
                packer.packString(k);
                packer.packString(headers.get(k));
            }    
        } else {
            packer.packMapHeader(0);
        }
    }
    
    private Collection<String> readStreamIds(MessageUnpacker unpacker) throws IOException {
        int streamCount = unpacker.unpackArrayHeader();
        Collection<String> streams = null;
        
        if (streamCount > 0) {
            streams = new ArrayList<String>();
            for (int i = 0; i < streamCount; i++) {
                streams.add(unpacker.unpackString());
            }
        }
        
        return streams;
    }
    
    private void writeStreamIds(Collection<String> streamIds, MessagePacker packer) throws IOException {
        if (streamIds != null) {
            packer.packArrayHeader(streamIds.size());
            for (String s: streamIds) {
                packer.packString(s);
            }
        } else {
            packer.packArrayHeader(0);
        }
    }
    
    private Object[] bindArguments(MessageUnpacker unpacker, List<Class<?>> paramTypes) throws IOException {
        int argumentCount = unpacker.unpackArrayHeader();
        
        if (paramTypes.size() != argumentCount) {
            throw new RuntimeException(String.format("Invocation provides %d argument(s) but target expects %d.", argumentCount, paramTypes.size()));
        }
        
        Object[] arguments = new Object[argumentCount];
        
        for (int i = 0; i < argumentCount; i++) {
            arguments[i] = readValue(unpacker, paramTypes.get(i));
        }
        
        return arguments;
    }
    
    private Object readValue(MessageUnpacker unpacker, Class<?> itemType) throws IOException {
        // This shouldn't ever get called with itemType == null, but we return anyways to avoid NullPointerExceptions
        if (itemType == null) {
            return null;
        }
        MessageFormat messageFormat = unpacker.getNextFormat();
        ValueType valueType = messageFormat.getValueType();
        int length;
        Object item = null;
         
        switch(valueType) {
            case NIL:
                unpacker.unpackNil();
                item = null;
                break;
            case BOOLEAN:
                item = unpacker.unpackBoolean();
                break;
            case INTEGER:
                switch (messageFormat) {
                case UINT64:
                    item = unpacker.unpackBigInteger();
                    break;
                case INT64:
                case UINT32:
                    item = unpacker.unpackLong();
                    break;
                default:
                    item = unpacker.unpackInt();
                    break;
                }
                break;
            case FLOAT:
                item = unpacker.unpackDouble();
                break;
            case STRING:
                item = unpacker.unpackString();
                break;
            case BINARY:
                length = unpacker.unpackBinaryHeader();
                byte[] binaryValue = new byte[length];
                unpacker.readPayload(binaryValue);
                item = binaryValue;
                break;
            case ARRAY:
                length = unpacker.unpackArrayHeader();
                Object[] arrayValue = new Object[length];
                for (int i = 0; i < length; i++) {
                    arrayValue[i] = readValue(unpacker, new Object().getClass());
                }
                item = arrayValue;
                //If the itemType is an array, we return an array. Else we convert the array to a list.
                if (!itemType.isArray()) {
                    item = new ArrayList<Object>(Arrays.asList(arrayValue));
                }
                break;
            case MAP:
                length = unpacker.unpackMapHeader();
                Map<Object, Object> mapValue = new HashMap<Object, Object>();
                for (int i = 0; i < length; i++) {
                    Object key = readValue(unpacker, new Object().getClass());
                    Object value = readValue(unpacker, new Object().getClass());
                    mapValue.put(key, value);
                }
                item = mapValue;
                break;
            case EXTENSION:
                /*
                ExtensionTypeHeader extension = unpacker.unpackExtensionTypeHeader();
                byte[] extensionValue = new byte[extension.getLength()];
                unpacker.readPayload(extensionValue);
                //Convert this to an object?
                item = extensionValue;
                */
                throw new RuntimeException("Extension types are not supported yet");
            default:
                break;
        }
        if (itemType.isPrimitive()) {
        return Utils.toPrimitive(itemType, item);
        }
        return itemType.cast(item);
    }

    private void writeValue(Object o, MessagePacker packer) throws IOException {

        if (o == null) {
            packer.packNil();
        } else if (o instanceof Boolean) {
            packer.packBoolean((boolean) o);
        } else if (o instanceof BigInteger) {
            packer.packBigInteger((BigInteger) o);
        } else if (o instanceof Long) {
            packer.packLong((long) o);
        } else if (o instanceof Short) {
            packer.packShort((short) o);
        } else if (o instanceof Integer) {
            packer.packInt((int) o);
        } else if (o instanceof Double) {
            packer.packDouble((double) o);
        } else if (o instanceof Float) {
            packer.packFloat((float) o);
        } else if (o instanceof String) {
            packer.packString((String) o);
        } else if (o instanceof Byte) {
            packer.packByte((byte) o);
        // Unsure about this
        } else if (o instanceof Collection<?>) {
            @SuppressWarnings("unchecked")
            Collection<Object> list = (Collection<Object>) o;
            packer.packArrayHeader(list.size());
            for (Object item: list) {
                writeValue(item, packer);
            }
        } else if (o.getClass().isArray()) {
            Object[] array = (Object[]) o;
            packer.packArrayHeader(array.length);
            for (Object item: array) {
                writeValue(item, packer);
            }
        } else if (o instanceof Map<?, ?>) {
            @SuppressWarnings("unchecked")
            Map<Object, Object> map = (HashMap<Object, Object>) o;
            packer.packMapHeader(map.size());
            for (Object k: map.keySet()) {
                writeValue(k, packer);
                writeValue(map.get(k), packer);
            }
        } else {
            throw new RuntimeException("Only base MessagePack types are currently supported");
        }
    }
}
