// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import java.net.URISyntaxException;
import java.util.HashMap;

public class HubConnection {
    private String _url;
    private Transport _transport;
    private OnReceiveCallBack callback;
    private HashMap<String, Action> handlers = new HashMap<>();
    private HubProtocol protocol;

    public Boolean connected = false;

    public HubConnection(String url) {
        _url = url;
        protocol = new JsonHubProtocol();
        callback = (payload) -> {

            InvocationMessage[] messages = protocol.parseMessages(payload);

            // message will be null if we receive any message other than an invocation.
            // Adding this to avoid getting error messages on pings for now.
            for (InvocationMessage message : messages) {
                if (message != null && handlers.containsKey(message.target)) {
                    handlers.get(message.target).invoke(message.arguments[0]);
                }
            }
        };

        try {
            _transport = new WebSocketTransport(_url);
        } catch (URISyntaxException e) {
            e.printStackTrace();
        }
    }

    public void start() throws InterruptedException {
        _transport.setOnReceive(this.callback);
        _transport.start();
        connected = true;
    }

    public void stop(){
        _transport.stop();
        connected = false;
    }

    public void send(String method, Object... args) {
        InvocationMessage invocationMessage = new InvocationMessage(method, args);
        String message = protocol.writeMessage(invocationMessage);
        _transport.send(message);
    }

    public void On(String target, Action callback) {
        handlers.put(target, callback);
    }
}