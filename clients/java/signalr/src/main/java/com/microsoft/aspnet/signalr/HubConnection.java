// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.aspnet.signalr;

import com.google.gson.Gson;
import com.google.gson.JsonArray;
import java.net.URISyntaxException;
import java.util.ArrayList;
import java.util.List;

public class HubConnection {
    private String url;
    private Transport transport;
    private OnReceiveCallBack callback;
    private CallbackMap handlers = new CallbackMap();
    private HubProtocol protocol;
    private Gson gson = new Gson();
    private Boolean handshakeReceived = false;
    private static final String RECORD_SEPARATOR = "\u001e";
    private HubConnectionState connectionState = HubConnectionState.DISCONNECTED;
    private Logger logger;

    public HubConnection(String url, Transport transport, Logger logger){
        this.url = url;
        this.protocol = new JsonHubProtocol();
        this.logger = logger;
        this.callback = (payload) -> {

            if (!handshakeReceived) {
                int handshakeLength = payload.indexOf(RECORD_SEPARATOR) + 1;
                String handshakeResponseString = payload.substring(0, handshakeLength - 1);
                HandshakeResponseMessage handshakeResponse = HandshakeProtocol.parseHandshakeResponse(handshakeResponseString);
                if (handshakeResponse.error != null) {
                    String errorMessage = "Error in handshake " + handshakeResponse.error;
                    logger.log(LogLevel.Error, errorMessage);
                    throw new Exception(errorMessage);
                }
                handshakeReceived = true;

                payload = payload.substring(handshakeLength);
                // The payload only contained the handshake response so we can return.
                if (payload.length() == 0) {
                    return;
                }
            }

            HubMessage[] messages = protocol.parseMessages(payload);

            for (HubMessage message : messages) {
                logger.log(LogLevel.Debug,"Received message of type %s", message.getMessageType());
                switch (message.getMessageType()) {
                    case INVOCATION:
                        InvocationMessage invocationMessage = (InvocationMessage)message;
                        if (handlers.containsKey(invocationMessage.target)) {
                            ArrayList<Object> args = gson.fromJson((JsonArray)invocationMessage.arguments[0], (new ArrayList<>()).getClass());
                            List<ActionBase> actions = handlers.get(invocationMessage.target);
                            if (actions != null) {
                                logger.log(LogLevel.Debug, "Invoking handlers for target %s", invocationMessage.target);
                                for (ActionBase action: actions) {
                                    action.invoke(args.toArray());
                                }
                            }
                        } else {
                            logger.log(LogLevel.Warning, "Failed to find handler for %s method", invocationMessage.target);
                        }
                        break;
                    case STREAM_INVOCATION:
                    case STREAM_ITEM:
                    case CLOSE:
                    case CANCEL_INVOCATION:
                    case COMPLETION:
                        logger.log(LogLevel.Error, "This client does not support %s messages", message.getMessageType());

                        throw new UnsupportedOperationException(String.format("The message type %s is not supported yet.", message.getMessageType()));
                    case PING:
                        // We don't need to do anything in the case of a ping message.
                        break;
                }
            }
        };

        if (transport == null){
            try {
                this.transport = new WebSocketTransport(this.url, this.logger);
            } catch (URISyntaxException e) {
                e.printStackTrace();
            }
        } else {
            this.transport = transport;
        }
    }

    /**
     * Initializes a new instance of the {@link HubConnection} class.
     * @param url The url of the SignalR server to connect to.
     * @param transport The {@link Transport} that the client will use to communicate with the server.
     */
    public HubConnection(String url, Transport transport) {
        this(url, transport, new NullLogger());
    }

    /**
     * Initializes a new instance of the {@link HubConnection} class.
     * @param url The url of the SignalR server to connect to.
     */
    public HubConnection(String url) {
        this(url, null, new NullLogger());
    }

    /**
     * Initializes a new instance of the {@link HubConnection} class.
     * @param url The url of the SignalR server to connect to.
     * @param logLevel The minimum level of messages to log.
     */
    public HubConnection(String url, LogLevel logLevel){
        this(url, null, new ConsoleLogger(logLevel));
    }

    /**
     * Indicates the state of the {@link HubConnection} to the server.
     * @return HubConnection state enum.
     */
    public HubConnectionState getConnectionState() {
        return connectionState;
    }

    /**
     * Starts a connection to the server.
     * @throws Exception An error occurred while connecting.
     */
    public void start() throws Exception {
        logger.log(LogLevel.Debug, "Starting HubConnection");
        transport.setOnReceive(this.callback);
        transport.start();
        String handshake = HandshakeProtocol.createHandshakeRequestMessage(new HandshakeRequestMessage(protocol.getName(), protocol.getVersion()));
        transport.send(handshake);
        connectionState = HubConnectionState.CONNECTED;
        logger.log(LogLevel.Information, "HubConnected started");
    }

    /**
     * Stops a connection to the server.
     */
    public void stop(){
        logger.log(LogLevel.Debug, "Stopping HubConnection");
        transport.stop();
        connectionState = HubConnectionState.DISCONNECTED;
        logger.log(LogLevel.Information, "HubConnection stopped");
    }

    /**
     * Invokes a hub method on the server using the specified method name.
     * Does not wait for a response from the receiver.
     * @param method The name of the server method to invoke.
     * @param args The arguments to be passed to the method.
     * @throws Exception If there was an error while sending.
     */
    public void send(String method, Object... args) throws Exception {
        InvocationMessage invocationMessage = new InvocationMessage(method, args);
        String message = protocol.writeMessage(invocationMessage);
        logger.log(LogLevel.Debug, "Sending message");
        transport.send(message);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     * @param target The name of the hub method to define.
     * @param callback The handler that will be raised when the hub method is invoked.
     * @return A {@link Subscription} that can be disposed to unsubscribe from the hub method.
     */
    public Subscription on(String target, Action callback) {
        ActionBase action = args -> callback.invoke();
        handlers.put(target, action);
        logger.log(LogLevel.Trace, "Registering handler for client method: %s", target);
        return new Subscription(handlers, action, target);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     * @param target The name of the hub method to define.
     * @param callback The handler that will be raised when the hub method is invoked.
     * @param param1 The first parameter.
     * @param <T1> The first argument type.
     * @return A {@link Subscription} that can be disposed to unsubscribe from the hub method.
     */
    public <T1> Subscription on(String target, Action1<T1> callback, Class<T1> param1) {
        ActionBase action = params -> callback.invoke(param1.cast(params[0]));
        handlers.put(target, action);
        logger.log(LogLevel.Trace, "Registering handler for client method: %s", target);
        return new Subscription(handlers, action, target);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     * @param target The name of the hub method to define.
     * @param callback The handler that will be raised when the hub method is invoked.
     * @param param1 The first parameter.
     * @param param2 The second parameter.
     * @param <T1> The first parameter type.
     * @param <T2> The second parameter type.
     * @return A {@link Subscription} that can be disposed to unsubscribe from the hub method.
     */
    public <T1, T2> Subscription on(String target, Action2<T1, T2> callback, Class<T1> param1, Class<T2> param2) {
        ActionBase action = params -> {
            callback.invoke(param1.cast(params[0]), param2.cast(params[1]));
        };
        handlers.put(target, action);
        logger.log(LogLevel.Trace, "Registering handler for client method: %s", target);
        return new Subscription(handlers, action, target);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     * @param target The name of the hub method to define.
     * @param callback The handler that will be raised when the hub method is invoked.
     * @param param1 The first parameter.
     * @param param2 The second parameter.
     * @param param3 The third parameter.
     * @param <T1> The first parameter type.
     * @param <T2> The second parameter type.
     * @param <T3> The third parameter type.
     * @return A {@link Subscription} that can be disposed to unsubscribe from the hub method.
     */
    public <T1, T2, T3> Subscription on(String target, Action3<T1, T2, T3> callback,
                                        Class<T1> param1, Class<T2> param2, Class<T3> param3) {
        ActionBase action = params -> {
            callback.invoke(param1.cast(params[0]), param2.cast(params[1]), param3.cast(params[2]));
        };
        handlers.put(target, action);
        logger.log(LogLevel.Trace, "Registering handler for client method: %s", target);
        return new Subscription(handlers, action, target);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     * @param target The name of the hub method to define.
     * @param callback The handler that will be raised when the hub method is invoked.
     * @param param1 The first parameter.
     * @param param2 The second parameter.
     * @param param3 The third parameter.
     * @param param4 The fourth parameter.
     * @param <T1> The first parameter type.
     * @param <T2> The second parameter type.
     * @param <T3> The third parameter type.
     * @param <T4> The fourth parameter type.
     * @return A {@link Subscription} that can be disposed to unsubscribe from the hub method.
     */
    public <T1, T2, T3, T4> Subscription on(String target, Action4<T1, T2, T3, T4> callback,
                                            Class<T1> param1, Class<T2> param2, Class<T3> param3, Class<T4> param4) {
        ActionBase action = params -> {
            callback.invoke(param1.cast(params[0]), param2.cast(params[1]), param3.cast(params[2]), param4.cast(params[3]));
        };
        handlers.put(target, action);
        logger.log(LogLevel.Trace, "Registering handler for client method: %s", target);
        return new Subscription(handlers, action, target);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     * @param target The name of the hub method to define.
     * @param callback The handler that will be raised when the hub method is invoked.
     * @param param1 The first parameter.
     * @param param2 The second parameter.
     * @param param3 The third parameter.
     * @param param4 The fourth parameter.
     * @param param5 The fifth parameter.
     * @param <T1> The first parameter type.
     * @param <T2> The second parameter type.
     * @param <T3> The third parameter type.
     * @param <T4> The fourth parameter type.
     * @param <T5> The fifth parameter type.
     * @return A {@link Subscription} that can be disposed to unsubscribe from the hub method.
     */
    public <T1, T2, T3, T4, T5> Subscription on(String target, Action5<T1, T2, T3, T4, T5> callback,
                                                Class<T1> param1, Class<T2> param2, Class<T3> param3, Class<T4> param4, Class<T5> param5) {
        ActionBase action = params -> {
            callback.invoke(param1.cast(params[0]), param2.cast(params[1]), param3.cast(params[2]), param4.cast(params[3]),
                    param5.cast(params[4]));
        };
        handlers.put(target, action);
        logger.log(LogLevel.Trace, "Registering handler for client method: %s", target);
        return new Subscription(handlers, action, target);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     * @param target The name of the hub method to define.
     * @param callback The handler that will be raised when the hub method is invoked.
     * @param param1 The first parameter.
     * @param param2 The second parameter.
     * @param param3 The third parameter.
     * @param param4 The fourth parameter.
     * @param param5 The fifth parameter.
     * @param param6 The sixth parameter.
     * @param <T1> The first parameter type.
     * @param <T2> The second parameter type.
     * @param <T3> The third parameter type.
     * @param <T4> The fourth parameter type.
     * @param <T5> The fifth parameter type.
     * @param <T6> The sixth parameter type.
     * @return A {@link Subscription} that can be disposed to unsubscribe from the hub method.
     */
    public <T1, T2, T3, T4, T5, T6> Subscription on(String target, Action6<T1, T2, T3, T4, T5, T6> callback,
                                                    Class<T1> param1, Class<T2> param2, Class<T3> param3, Class<T4> param4, Class<T5> param5, Class<T6> param6) {
        ActionBase action = params -> {
            callback.invoke(param1.cast(params[0]), param2.cast(params[1]), param3.cast(params[2]), param4.cast(params[3]),
                    param5.cast(params[4]) ,param6.cast(params[5]));
        };
        handlers.put(target, action);
        logger.log(LogLevel.Trace, "Registering handler for client method: %s", target);
        return new Subscription(handlers, action, target);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     * @param target The name of the hub method to define.
     * @param callback The handler that will be raised when the hub method is invoked.
     * @param param1 The first parameter.
     * @param param2 The second parameter.
     * @param param3 The third parameter.
     * @param param4 The fourth parameter.
     * @param param5 The fifth parameter.
     * @param param6 The sixth parameter.
     * @param param7 The seventh parameter.
     * @param <T1> The first parameter type.
     * @param <T2> The second parameter type.
     * @param <T3> The third parameter type.
     * @param <T4> The fourth parameter type.
     * @param <T5> The fifth parameter type.
     * @param <T6> The sixth parameter type.
     * @param <T7> The seventh parameter type.
     * @return A {@link Subscription} that can be disposed to unsubscribe from the hub method.
     */
    public <T1, T2, T3, T4, T5, T6, T7> Subscription on(String target, Action7<T1, T2, T3, T4, T5, T6, T7> callback,
                                                        Class<T1> param1, Class<T2> param2, Class<T3> param3, Class<T4> param4, Class<T5> param5, Class<T6> param6, Class<T7> param7) {
        ActionBase action = params -> {
            callback.invoke(param1.cast(params[0]), param2.cast(params[1]), param3.cast(params[2]), param4.cast(params[3]),
                    param5.cast(params[4]) ,param6.cast(params[5]), param7.cast(params[6]));
        };
        handlers.put(target, action);
        logger.log(LogLevel.Trace, "Registering handler for client method: %s", target);
        return new Subscription(handlers, action, target);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     * @param target The name of the hub method to define.
     * @param callback The handler that will be raised when the hub method is invoked.
     * @param param1 The first parameter.
     * @param param2 The second parameter.
     * @param param3 The third parameter.
     * @param param4 The fourth parameter.
     * @param param5 The fifth parameter.
     * @param param6 The sixth parameter.
     * @param param7 The seventh parameter.
     * @param param8 The eighth parameter
     * @param <T1> The first parameter type.
     * @param <T2> The second parameter type.
     * @param <T3> The third parameter type.
     * @param <T4> The fourth parameter type.
     * @param <T5> The fifth parameter type.
     * @param <T6> The sixth parameter type.
     * @param <T7> The seventh parameter type.
     * @param <T8> The eighth parameter type.
     * @return A {@link Subscription} that can be disposed to unsubscribe from the hub method.
     */
    public <T1, T2, T3, T4, T5, T6, T7, T8> Subscription on(String target, Action8<T1, T2, T3, T4, T5, T6, T7, T8> callback,
                                                        Class<T1> param1, Class<T2> param2, Class<T3> param3, Class<T4> param4, Class<T5> param5, Class<T6> param6, Class<T7> param7, Class<T8> param8) {
        ActionBase action = params -> {
            callback.invoke(param1.cast(params[0]), param2.cast(params[1]), param3.cast(params[2]), param4.cast(params[3]),
                    param5.cast(params[4]) ,param6.cast(params[5]), param7.cast(params[6]), param8.cast(params[7]));
        };
        handlers.put(target, action);
        logger.log(LogLevel.Trace, "Registering handler for client method: %s", target);
        return new Subscription(handlers, action, target);
    }

    /**
     * Removes all handlers associated with the method with the specified method name.
     * @param name The name of the hub method from which handlers are being removed.
     */
    public void remove(String name) {
        handlers.remove(name);
        logger.log(LogLevel.Trace, "Removing handlers for client method %s" , name);
    }
}