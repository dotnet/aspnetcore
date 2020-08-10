package com.microsoft.signalr;

import java.lang.reflect.Type;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

public class TestBinder implements InvocationBinder {
    private TypeAndClass[] paramTypes = null;
    private TypeAndClass returnType = null;

    public TestBinder(HubMessage expectedMessage) {
        if (expectedMessage == null) {
            return;
        }

        switch (expectedMessage.getMessageType()) {
            case STREAM_INVOCATION:
                ArrayList<TypeAndClass> streamTypes = new ArrayList<>();
                for (Object obj : ((StreamInvocationMessage) expectedMessage).getArguments()) {
                    Type type = getType(obj.getClass());
                    streamTypes.add(new TypeAndClass(type, obj.getClass()));
                }
                paramTypes = streamTypes.toArray(new TypeAndClass[streamTypes.size()]);
                break;
            case INVOCATION:
                ArrayList<TypeAndClass> types = new ArrayList<>();
                for (Object obj : ((InvocationMessage) expectedMessage).getArguments()) {
                    Type type = getType(obj.getClass());
                    types.add(new TypeAndClass(type, obj.getClass()));
                }
                paramTypes = types.toArray(new TypeAndClass[types.size()]);
                break;
            case STREAM_ITEM:
                break;
            case COMPLETION:
                Object obj = ((CompletionMessage)expectedMessage).getResult().getClass();
                returnType = new TypeAndClass(getType(((CompletionMessage)expectedMessage).getResult().getClass()), 
                    ((CompletionMessage)expectedMessage).getResult().getClass());
                break;
            default:
                break;
        }
    }
    
    private <T> Type getType(Class<T> param) {
        return (new TypeReference<T>() {}).getType();
    }

    @Override
    public TypeAndClass getReturnType(String invocationId) {
        return returnType;
    }

    @Override
    public List<TypeAndClass> getParameterTypes(String methodName) {
        if (paramTypes == null) {
            return new ArrayList<>();
        }
        return new ArrayList<TypeAndClass>(Arrays.asList(paramTypes));
    }
}