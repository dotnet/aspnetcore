// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

    public HubConnection(String url, Transport transport) {
        this.url = url;
        this.protocol = new JsonHubProtocol();
        this.callback = (payload) -> {

            if (!handshakeReceived) {
                int handshakeLength = payload.indexOf(RECORD_SEPARATOR) + 1;
                String handshakeResponseString = payload.substring(0, handshakeLength - 1);
                HandshakeResponseMessage handshakeResponse = HandshakeProtocol.parseHandshakeResponse(handshakeResponseString);
                if (handshakeResponse.error != null) {
                    throw new Exception("Error in handshake " + handshakeResponse.error);
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
                switch (message.getMessageType()) {
                    case INVOCATION:
                        InvocationMessage invocationMessage = (InvocationMessage)message;
                        if (message != null && handlers.containsKey(invocationMessage.target)) {
                            ArrayList<Object> args = gson.fromJson((JsonArray)invocationMessage.arguments[0], (new ArrayList<>()).getClass());
                            List<ActionBase> actions = handlers.get(invocationMessage.target);
                            if (actions != null) {
                                for (ActionBase action: actions) {
                                    action.invoke(args.toArray());
                                }
                            }
                        }
                        break;
                    case STREAM_INVOCATION:
                    case STREAM_ITEM:
                    case CLOSE:
                    case CANCEL_INVOCATION:
                    case COMPLETION:
                        throw new UnsupportedOperationException("The message type " + message.getMessageType() + " is not supported yet.");
                    case PING:
                        // We don't need to do anything in the case of a ping message.
                        // The other message types aren't supported
                        break;
                }
            }
        };

        if (transport == null){
            try {
                this.transport = new WebSocketTransport(this.url);
            } catch (URISyntaxException e) {
                e.printStackTrace();
            }
        } else {
            this.transport = transport;
        }
    }

    public HubConnection(String url) {
        this(url, null);
    }

    public HubConnectionState getConnectionState() {
        return connectionState;
    }

    public void start() throws Exception {
        transport.setOnReceive(this.callback);
        transport.start();
        String handshake = HandshakeProtocol.createHandshakeRequestMessage(new HandshakeRequestMessage(protocol.getName(), protocol.getVersion()));
        transport.send(handshake);
        connectionState = HubConnectionState.CONNECTED;
    }

    public void stop(){
        transport.stop();
        connectionState = HubConnectionState.DISCONNECTED;
    }

    public void send(String method, Object... args) throws Exception {
        InvocationMessage invocationMessage = new InvocationMessage(method, args);
        String message = protocol.writeMessage(invocationMessage);
        transport.send(message);
    }

    public Subscription on(String target, Action callback) {
        ActionBase action = args -> callback.invoke();
        handlers.put(target, action);
        return new Subscription(handlers, action, target);
    }

    public <T1> Subscription on(String target, Action1<T1> callback, Class<T1> param1) {
        ActionBase action = params -> callback.invoke(param1.cast(params[0]));
        handlers.put(target, action);
        return new Subscription(handlers, action, target);
    }

    public <T1, T2> Subscription on(String target, Action2<T1, T2> callback, Class<T1> param1, Class<T2> param2) {
        ActionBase action = params -> {
            callback.invoke(param1.cast(params[0]), param2.cast(params[1]));
        };
        handlers.put(target, action);
        return new Subscription(handlers, action, target);
    }

    public <T1, T2, T3> Subscription on(String target, Action3<T1, T2, T3> callback,
                                        Class<T1> param1, Class<T2> param2, Class<T3> param3) {
        ActionBase action = params -> {
            callback.invoke(param1.cast(params[0]), param2.cast(params[1]), param3.cast(params[2]));
        };
        handlers.put(target, action);
        return new Subscription(handlers, action, target);
    }

    public <T1, T2, T3, T4> Subscription on(String target, Action4<T1, T2, T3, T4> callback,
                                            Class<T1> param1, Class<T2> param2, Class<T3> param3, Class<T4> param4) {
        ActionBase action = params -> {
            callback.invoke(param1.cast(params[0]), param2.cast(params[1]), param3.cast(params[2]), param4.cast(params[3]));
        };
        handlers.put(target, action);
        return new Subscription(handlers, action, target);
    }

    public <T1, T2, T3, T4, T5> Subscription on(String target, Action5<T1, T2, T3, T4, T5> callback,
                                                Class<T1> param1, Class<T2> param2, Class<T3> param3, Class<T4> param4, Class<T5> param5) {
        ActionBase action = params -> {
            callback.invoke(param1.cast(params[0]), param2.cast(params[1]), param3.cast(params[2]), param4.cast(params[3]),
                    param5.cast(params[4]));
        };
        handlers.put(target, action);
        return new Subscription(handlers, action, target);
    }

    public <T1, T2, T3, T4, T5, T6> Subscription on(String target, Action6<T1, T2, T3, T4, T5, T6> callback,
                                                    Class<T1> param1, Class<T2> param2, Class<T3> param3, Class<T4> param4, Class<T5> param5, Class<T6> param6) {
        ActionBase action = params -> {
            callback.invoke(param1.cast(params[0]), param2.cast(params[1]), param3.cast(params[2]), param4.cast(params[3]),
                    param5.cast(params[4]) ,param6.cast(params[5]));
        };
        handlers.put(target, action);
        return new Subscription(handlers, action, target);
    }

    public <T1, T2, T3, T4, T5, T6, T7> Subscription on(String target, Action7<T1, T2, T3, T4, T5, T6, T7> callback,
                                                        Class<T1> param1, Class<T2> param2, Class<T3> param3, Class<T4> param4, Class<T5> param5, Class<T6> param6, Class<T7> param7) {
        ActionBase action = params -> {
            callback.invoke(param1.cast(params[0]), param2.cast(params[1]), param3.cast(params[2]), param4.cast(params[3]),
                    param5.cast(params[4]) ,param6.cast(params[5]), param7.cast(params[6]));
        };
        handlers.put(target, action);
        return new Subscription(handlers, action, target);
    }

    public <T1, T2, T3, T4, T5, T6, T7, T8> Subscription on(String target, Action8<T1, T2, T3, T4, T5, T6, T7, T8> callback,
                                                        Class<T1> param1, Class<T2> param2, Class<T3> param3, Class<T4> param4, Class<T5> param5, Class<T6> param6, Class<T7> param7, Class<T8> param8) {
        ActionBase action = params -> {
            callback.invoke(param1.cast(params[0]), param2.cast(params[1]), param3.cast(params[2]), param4.cast(params[3]),
                    param5.cast(params[4]) ,param6.cast(params[5]), param7.cast(params[6]), param8.cast(params[7]));
        };
        handlers.put(target, action);
        return new Subscription(handlers, action, target);
    }

    public void remove(String name) {
        handlers.remove(name);
    }
}