// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import java.io.IOException;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

import org.msgpack.core.ExtensionTypeHeader;
import org.msgpack.core.MessageFormat;
import org.msgpack.core.MessagePack;
import org.msgpack.core.MessagePackException;
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
    public HubMessage[] parseMessages(String payload, InvocationBinder binder) {
        if (payload.length() == 0) {
            return new HubMessage[]{};
        }

        List<HubMessage> hubMessages = new ArrayList<>();
        
        try {
			MessageUnpacker unpacker = MessagePack.newDefaultUnpacker(payload.getBytes());
			while (unpacker.hasNext()) {
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
			}
		} catch (MessagePackException | IOException ex) {
			throw new RuntimeException("Error reading MessagePack data.", ex);
		}

        return hubMessages.toArray(new HubMessage[hubMessages.size()]);
    }
    
    @Override
    public String writeMessage(HubMessage hubMessage) {
        return null;
    }
        
    private HubMessage createInvocationMessage(MessageUnpacker unpacker, InvocationBinder binder, int itemCount) throws IOException {
    	Map<String, String> headers = readHeaders(unpacker);
    	String invocationId = unpacker.unpackString();
    	
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
    	
    	// NOTE - C# client accounts for possibility of streamId array being absent, while spec claims it's required - which?
    	List<String> streams = readStreamIds(unpacker);
    	
    	// NOTE - C# client applies headers by assigning Message.Headers directly, rather than adding new constructor
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
    	
    	List<String> streams = readStreamIds(unpacker);
    	
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
    
    private List<String> readStreamIds(MessageUnpacker unpacker) throws IOException {
    	int streamCount = unpacker.unpackArrayHeader();
    	List<String> streams = null;
    	
    	if (streamCount > 0) {
    		streams = new ArrayList<String>();
    		for (int i = 0; i < streamCount; i++) {
    			streams.add(unpacker.unpackString());
    		}
    	}
    	
    	return streams;
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
                ExtensionTypeHeader extension = unpacker.unpackExtensionTypeHeader();
                byte[] extensionValue = new byte[extension.getLength()];
                unpacker.readPayload(extensionValue);
                item = extensionValue;
                break;
        	default:
        		break;
        }
        return itemType.cast(item);
    }
}
