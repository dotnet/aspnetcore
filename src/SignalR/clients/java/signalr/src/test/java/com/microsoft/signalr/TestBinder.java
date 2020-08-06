package com.microsoft.signalr;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

public class TestBinder implements InvocationBinder {
    private Class<?>[] paramTypes = null;
    private Class<?> returnType = null;

    public TestBinder(HubMessage expectedMessage) {
        if (expectedMessage == null) {
            return;
        }

        switch (expectedMessage.getMessageType()) {
            case STREAM_INVOCATION:
                ArrayList<Class<?>> streamTypes = new ArrayList<>();
                for (Object obj : ((StreamInvocationMessage) expectedMessage).getArguments()) {
                    streamTypes.add(obj.getClass());
                }
                paramTypes = streamTypes.toArray(new Class<?>[streamTypes.size()]);
                break;
            case INVOCATION:
                ArrayList<Class<?>> types = new ArrayList<>();
                for (Object obj : ((InvocationMessage) expectedMessage).getArguments()) {
                    types.add(obj.getClass());
                }
                paramTypes = types.toArray(new Class<?>[types.size()]);
                break;
            case STREAM_ITEM:
                break;
            case COMPLETION:
                returnType = ((CompletionMessage)expectedMessage).getResult().getClass();
                break;
            default:
                break;
        }
    }

    @Override
    public Class<?> getReturnType(String invocationId) {
        return returnType;
    }

    @Override
    public List<Class<?>> getParameterTypes(String methodName) {
        if (paramTypes == null) {
            return new ArrayList<>();
        }
        return new ArrayList<Class<?>>(Arrays.asList(paramTypes));
    }
}