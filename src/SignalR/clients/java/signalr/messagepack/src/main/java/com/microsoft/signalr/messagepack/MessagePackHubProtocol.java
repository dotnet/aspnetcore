// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr.messagepack;

import java.io.IOException;
import java.lang.reflect.Type;
import java.nio.Buffer;
import java.nio.ByteBuffer;
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
import org.msgpack.jackson.dataformat.MessagePackFactory;
import org.msgpack.value.ValueType;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.type.TypeFactory;

import com.microsoft.signalr.CancelInvocationMessage;
import com.microsoft.signalr.CloseMessage;
import com.microsoft.signalr.CompletionMessage;
import com.microsoft.signalr.HubMessage;
import com.microsoft.signalr.HubMessageType;
import com.microsoft.signalr.HubProtocol;
import com.microsoft.signalr.InvocationBinder;
import com.microsoft.signalr.InvocationBindingFailureMessage;
import com.microsoft.signalr.InvocationMessage;
import com.microsoft.signalr.PingMessage;
import com.microsoft.signalr.StreamBindingFailureMessage;
import com.microsoft.signalr.StreamInvocationMessage;
import com.microsoft.signalr.StreamItem;

public class MessagePackHubProtocol implements HubProtocol {

    private static final int ERROR_RESULT = 1;
    private static final int VOID_RESULT = 2;
    private static final int NON_VOID_RESULT = 3;

    private ObjectMapper objectMapper = new ObjectMapper(new MessagePackFactory());
    private TypeFactory typeFactory = objectMapper.getTypeFactory();

    @Override
    public String getName() {
        return "messagepack";
    }

    @Override
    public int getVersion() {
        return 1;
    }

    @Override
    public List<HubMessage> parseMessages(ByteBuffer payload, InvocationBinder binder) {
        if (payload.remaining() == 0) {
            return null;
        }

         // MessagePack library can't handle read-only ByteBuffer - copy into an array-backed ByteBuffer if this is the case
        if (payload.isReadOnly()) {
            byte[] payloadBytes = new byte[payload.remaining()];
            payload.get(payloadBytes, 0, payloadBytes.length);
            payload = ByteBuffer.wrap(payloadBytes);
        }

        List<HubMessage> hubMessages = new ArrayList<>();

        while (payload.hasRemaining()) {
            int length;
            try {
                length = Utils.readLengthHeader(payload);
                // Throw if remaining buffer is shorter than length header
                if (payload.remaining() < length) {
                    throw new RuntimeException(String.format("MessagePack message was length %d but claimed to be length %d.", payload.remaining(), length));
                }
            } catch (IOException ex) {
                throw new RuntimeException("Error reading length header.", ex);
            }
            // Instantiate MessageUnpacker
            try(MessageUnpacker unpacker = MessagePack.newDefaultUnpacker(payload)) {

                int itemCount = unpacker.unpackArrayHeader();
                HubMessageType messageType = HubMessageType.values()[unpacker.unpackInt() - 1];

                switch (messageType) {
                    case INVOCATION:
                        hubMessages.add(createInvocationMessage(unpacker, binder, itemCount, payload));
                        break;
                    case STREAM_ITEM:
                        hubMessages.add(createStreamItemMessage(unpacker, binder, payload));
                        break;
                    case COMPLETION:
                        hubMessages.add(createCompletionMessage(unpacker, binder, payload));
                        break;
                    case STREAM_INVOCATION:
                        hubMessages.add(createStreamInvocationMessage(unpacker, binder, itemCount, payload));
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
                    // Check what the last message was
                    // If it was an invocation binding failure, we have to correct the position of the buffer
                    if (hubMessages.get(hubMessages.size() - 1).getMessageType() == HubMessageType.INVOCATION_BINDING_FAILURE) {
                        // Cast to a Buffer to avoid the Java 9+ behavior where ByteBuffer.position(int) overrides Buffer.position(int),
                        // Returning a ByteBuffer rather than a Buffer. This causes issues on Android - see https://github.com/dotnet/aspnetcore/pull/26614
                        ((Buffer) payload).position(payload.position() + (length - readBytes));
                    } else {
                        throw new RuntimeException(String.format("MessagePack message was length %d but claimed to be length %d.", readBytes, length));
                    }
                }
                unpacker.close();
                // Cast to a Buffer to avoid the Java 9+ behavior where ByteBuffer.position(int) overrides Buffer.position(int),
                // Returning a ByteBuffer rather than a Buffer. This causes issues on Android - see https://github.com/dotnet/aspnetcore/pull/26614
                ((Buffer) payload).position(payload.position() + readBytes);
            } catch (MessagePackException | IOException ex) {
                throw new RuntimeException("Error reading MessagePack data.", ex);
            }
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

    private HubMessage createInvocationMessage(MessageUnpacker unpacker, InvocationBinder binder, int itemCount, ByteBuffer payload) throws IOException {
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
            List<Type> types = binder.getParameterTypes(target);
            arguments = bindArguments(unpacker, types, payload);
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

    private HubMessage createStreamItemMessage(MessageUnpacker unpacker, InvocationBinder binder, ByteBuffer payload) throws IOException {
        Map<String, String> headers = readHeaders(unpacker);
        String invocationId = unpacker.unpackString();
        Object value;
        try {
            Type itemType = binder.getReturnType(invocationId);
            value = readValue(unpacker, itemType, payload, true);
        } catch (Exception ex) {
            return new StreamBindingFailureMessage(invocationId, ex);
        }

        return new StreamItem(headers, invocationId, value);
    }

    private HubMessage createCompletionMessage(MessageUnpacker unpacker, InvocationBinder binder, ByteBuffer payload) throws IOException {
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
                Type itemType = binder.getReturnType(invocationId);
                result = readValue(unpacker, itemType, payload, true);
                break;
            default:
                throw new RuntimeException("Invalid invocation result kind.");
        }

        return new CompletionMessage(headers, invocationId, result, error);
    }

    private HubMessage createStreamInvocationMessage(MessageUnpacker unpacker, InvocationBinder binder, int itemCount, ByteBuffer payload) throws IOException {
        Map<String, String> headers = readHeaders(unpacker);
        String invocationId = unpacker.unpackString();
        String target = unpacker.unpackString();

        Object[] arguments = null;
        try {
            List<Type> types = binder.getParameterTypes(target);
            arguments = bindArguments(unpacker, types, payload);
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
        // error may be nil
        String error = null;
        if (!unpacker.tryUnpackNil()) {
            error = unpacker.unpackString();
        }
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

    private Object[] bindArguments(MessageUnpacker unpacker, List<Type> paramTypes, ByteBuffer payload) throws IOException {
        int argumentCount = unpacker.unpackArrayHeader();

        if (paramTypes.size() != argumentCount) {
            throw new RuntimeException(String.format("Invocation provides %d argument(s) but target expects %d.", argumentCount, paramTypes.size()));
        }

        Object[] arguments = new Object[argumentCount];

        for (int i = 0; i < argumentCount; i++) {
            arguments[i] = readValue(unpacker, paramTypes.get(i), payload, true);
        }

        return arguments;
    }

    private Object readValue(MessageUnpacker unpacker, Type itemType, ByteBuffer payload, boolean outermostCall) throws IOException {
        Class<?> itemClass = Utils.typeToClass(itemType);
        MessageFormat messageFormat = unpacker.getNextFormat();
        ValueType valueType = messageFormat.getValueType();
        int length;
        long readBytesStart;
        Object item = null;

        switch(valueType) {
            case NIL:
                unpacker.unpackNil();
                return null;
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
                    // unpackInt could correspond to an int, short, char, or byte - cast those literally here
                    if (itemClass != null) {
                        if (itemClass.equals(Short.class) || itemClass.equals(short.class)) {
                            item = ((Integer) item).shortValue();
                        } else if (itemClass.equals(Character.class) || itemClass.equals(char.class)) {
                            item = (char) ((Integer) item).shortValue();
                        } else if (itemClass.equals(Byte.class) || itemClass.equals(byte.class)) {
                            item = ((Integer) item).byteValue();
                        }
                    }
                    break;
                }
                break;
            case FLOAT:
                item = unpacker.unpackDouble();
                break;
            case STRING:
                item = unpacker.unpackString();
                // ObjectMapper packs chars as Strings - correct back to char while unpacking if necessary
                if (itemClass != null && (itemClass.equals(char.class) || itemClass.equals(Character.class))) {
                    item = ((String) item).charAt(0);
                }
                break;
            case BINARY:
                length = unpacker.unpackBinaryHeader();
                byte[] binaryValue = new byte[length];
                unpacker.readPayload(binaryValue);
                item = binaryValue;
                break;
            case ARRAY:
                readBytesStart = unpacker.getTotalReadBytes();
                length = unpacker.unpackArrayHeader();
                for (int i = 0; i < length; i++) {
                    readValue(unpacker, Object.class, payload, false);
                }
                if (outermostCall) {
                    // Check how many bytes we've read, grab that from the payload, and deserialize with objectMapper
                    byte[] payloadBytes = payload.array();
                    // If itemType was null, we were just in this method to advance the buffer. return null.
                    if (itemType == null) {
                        return null;
                    }
                    return objectMapper.readValue(payloadBytes, payload.position() + (int) readBytesStart, (int) (unpacker.getTotalReadBytes() - readBytesStart),
                            typeFactory.constructType(itemType));
                } else {
                    // This is an inner call to readValue - we just need to read the right number of bytes
                    // We can return null, and the outermost call will know how many bytes to give to objectMapper.
                    return null;
                }
            case MAP:
                readBytesStart = unpacker.getTotalReadBytes();
                length = unpacker.unpackMapHeader();
                for (int i = 0; i < length; i++) {
                    readValue(unpacker, Object.class, payload, false);
                    readValue(unpacker, Object.class, payload, false);
                }
                if (outermostCall) {
                    // Check how many bytes we've read, grab that from the payload, and deserialize with objectMapper
                    byte[] payloadBytes = payload.array();
                    byte[] mapBytes = Arrays.copyOfRange(payloadBytes, payload.position() + (int) readBytesStart,
                        payload.position() + (int) unpacker.getTotalReadBytes());
                    // If itemType was null, we were just in this method to advance the buffer. return null.
                    if (itemType == null) {
                        return null;
                    }
                    return objectMapper.readValue(payloadBytes, payload.position() + (int) readBytesStart, (int) (unpacker.getTotalReadBytes() - readBytesStart),
                            typeFactory.constructType(itemType));
                } else {
                    // This is an inner call to readValue - we just need to read the right number of bytes
                    // We can return null, and the outermost call will know how many bytes to give to objectMapper.
                    return null;
                }
            case EXTENSION:
                /*
                ExtensionTypeHeader extension = unpacker.unpackExtensionTypeHeader();
                byte[] extensionValue = new byte[extension.getLength()];
                unpacker.readPayload(extensionValue);
                //Convert this to an object?
                item = extensionValue;
                */
                throw new RuntimeException("Extension types are not supported");
            default:
                return null;
        }
        // If itemType was null, we were just in this method to advance the buffer. return null.
        if (itemType == null) {
            return null;
        }
        // If we get here, the item isn't a map or a collection/array, so we use the Class to cast it
        if (itemClass.isPrimitive()) {
            return Utils.toPrimitive(itemClass, item);
        }
        return itemClass.cast(item);
    }

    private void writeValue(Object o, MessagePacker packer) throws IOException {
        packer.addPayload(objectMapper.writeValueAsBytes(o));
    }
}
