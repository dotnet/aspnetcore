// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.aspnet.signalr;

import java.io.IOException;
import java.util.*;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.concurrent.locks.Lock;
import java.util.concurrent.locks.ReentrantLock;
import java.util.function.Consumer;

import okhttp3.Cookie;
import okhttp3.CookieJar;
import okhttp3.HttpUrl;
import okhttp3.OkHttpClient;

public class HubConnection {
    private String url;
    private Transport transport;
    private OnReceiveCallBack callback;
    private CallbackMap handlers = new CallbackMap();
    private HubProtocol protocol;
    private Boolean handshakeReceived = false;
    private static final String RECORD_SEPARATOR = "\u001e";
    private HubConnectionState hubConnectionState = HubConnectionState.DISCONNECTED;
    private Lock hubConnectionStateLock = new ReentrantLock();
    private Logger logger;
    private List<Consumer<Exception>> onClosedCallbackList;
    private boolean skipNegotiate = false;
    private NegotiateResponse negotiateResponse;
    private String accessToken;
    private Map<String, String> headers = new HashMap<>();
    private ConnectionState connectionState = null;
    private OkHttpClient httpClient;

    private static ArrayList<Class<?>> emptyArray = new ArrayList<>();
    private static int MAX_NEGOTIATE_ATTEMPTS = 100;

    public HubConnection(String url, Transport transport, Logger logger, boolean skipNegotiate) {
        if (url == null || url.isEmpty()) {
            throw new IllegalArgumentException("A valid url is required.");
        }

        this.url = url;
        this.protocol = new JsonHubProtocol();

        if (logger != null) {
            this.logger = logger;
        } else {
            this.logger = new NullLogger();
        }

        if (transport != null) {
            this.transport = transport;
        }

        this.skipNegotiate = skipNegotiate;

        this.httpClient = new OkHttpClient.Builder()
                .cookieJar(new CookieJar() {
                    private List<Cookie> cookieList = new ArrayList<>();
                    private Lock cookieLock = new ReentrantLock();

                    @Override
                    public void saveFromResponse(HttpUrl url, List<Cookie> cookies) {
                        cookieLock.lock();
                        try {
                            for (Cookie cookie : cookies) {
                                boolean replacedCookie = false;
                                for (int i = 0; i < cookieList.size(); i++) {
                                    Cookie innerCookie = cookieList.get(i);
                                    if (cookie.name().equals(innerCookie.name()) && innerCookie.matches(url)) {
                                        // We have a new cookie that matches an older one so we replace the older one.
                                        cookieList.set(i, innerCookie);
                                        replacedCookie = true;
                                        break;
                                    }
                                }
                                if (!replacedCookie) {
                                    cookieList.add(cookie);
                                }
                            }
                        } finally {
                            cookieLock.unlock();
                        }
                    }

                    @Override
                    public List<Cookie> loadForRequest(HttpUrl url) {
                        cookieLock.lock();
                        try {
                            List<Cookie> matchedCookies = new ArrayList<>();
                            List<Cookie> expiredCookies = new ArrayList<>();
                            for (Cookie cookie : cookieList) {
                                if (cookie.expiresAt() < System.currentTimeMillis()) {
                                    expiredCookies.add(cookie);
                                } else if (cookie.matches(url)) {
                                    matchedCookies.add(cookie);
                                }
                            }

                            cookieList.removeAll(expiredCookies);
                            return matchedCookies;
                        } finally {
                            cookieLock.unlock();
                        }
                    }
                })
                .build();

        this.callback = (payload) -> {

            if (!handshakeReceived) {
                int handshakeLength = payload.indexOf(RECORD_SEPARATOR) + 1;
                String handshakeResponseString = payload.substring(0, handshakeLength - 1);
                HandshakeResponseMessage handshakeResponse = HandshakeProtocol.parseHandshakeResponse(handshakeResponseString);
                if (handshakeResponse.error != null) {
                    String errorMessage = "Error in handshake " + handshakeResponse.error;
                    logger.log(LogLevel.Error, errorMessage);
                    throw new HubException(errorMessage);
                }
                handshakeReceived = true;

                payload = payload.substring(handshakeLength);
                // The payload only contained the handshake response so we can return.
                if (payload.length() == 0) {
                    return;
                }
            }

            HubMessage[] messages = protocol.parseMessages(payload, connectionState);

            for (HubMessage message : messages) {
                logger.log(LogLevel.Debug, "Received message of type %s.", message.getMessageType());
                switch (message.getMessageType()) {
                    case INVOCATION:
                        InvocationMessage invocationMessage = (InvocationMessage) message;
                        List<InvocationHandler> handlers = this.handlers.get(invocationMessage.getTarget());
                        if (handlers != null) {
                            for (InvocationHandler handler : handlers) {
                                handler.getAction().invoke(invocationMessage.getArguments());
                            }
                        } else {
                            logger.log(LogLevel.Warning, "Failed to find handler for %s method.", invocationMessage.getMessageType());
                        }
                        break;
                    case CLOSE:
                        logger.log(LogLevel.Information, "Close message received from server.");
                        CloseMessage closeMessage = (CloseMessage) message;
                        stop(closeMessage.getError());
                        break;
                    case PING:
                        // We don't need to do anything in the case of a ping message.
                        break;
                    case COMPLETION:
                        CompletionMessage completionMessage = (CompletionMessage)message;
                        InvocationRequest irq = connectionState.tryRemoveInvocation(completionMessage.getInvocationId());
                        if (irq == null) {
                            logger.log(LogLevel.Warning, "Dropped unsolicited Completion message for invocation '%s'.", completionMessage.getInvocationId());
                            continue;
                        }
                        irq.complete(completionMessage);
                        break;
                    case STREAM_INVOCATION:
                    case STREAM_ITEM:
                    case CANCEL_INVOCATION:
                        logger.log(LogLevel.Error, "This client does not support %s messages.", message.getMessageType());

                        throw new UnsupportedOperationException(String.format("The message type %s is not supported yet.", message.getMessageType()));
                }
            }
        };
    }

    private NegotiateResponse handleNegotiate() throws IOException, HubException {
        accessToken = (negotiateResponse == null) ? null : negotiateResponse.getAccessToken();
        negotiateResponse = Negotiate.processNegotiate(url, httpClient, accessToken);

        if (negotiateResponse.getError() != null) {
            throw new HubException(negotiateResponse.getError());
        }
        if (negotiateResponse.getConnectionId() != null) {
            if (url.contains("?")) {
                url = url + "&id=" + negotiateResponse.getConnectionId();
            } else {
                url = url + "?id=" + negotiateResponse.getConnectionId();
            }
        }

        if (negotiateResponse.getAccessToken() != null) {
            this.headers.put("Authorization", "Bearer " + negotiateResponse.getAccessToken());
        }

        if (negotiateResponse.getRedirectUrl() != null) {
            this.url = this.negotiateResponse.getRedirectUrl();
        }

        return negotiateResponse;
    }

    /**
     * Indicates the state of the {@link HubConnection} to the server.
     *
     * @return HubConnection state enum.
     */
    public HubConnectionState getConnectionState() {
        return hubConnectionState;
    }

    /**
     * Starts a connection to the server.
     *
     * @throws Exception An error occurred while connecting.
     */
    public CompletableFuture start() throws Exception {
        if (hubConnectionState != HubConnectionState.DISCONNECTED) {
            return CompletableFuture.completedFuture(null);
        }
        if (!skipNegotiate) {
            int negotiateAttempts = 0;
            do {
                accessToken = (negotiateResponse == null) ? null : negotiateResponse.getAccessToken();
                negotiateResponse = handleNegotiate();
                negotiateAttempts++;
            } while (negotiateResponse.getRedirectUrl() != null && negotiateAttempts < MAX_NEGOTIATE_ATTEMPTS);
            if (!negotiateResponse.getAvailableTransports().contains("WebSockets")) {
                throw new HubException("There were no compatible transports on the server.");
            }
        }

        logger.log(LogLevel.Debug, "Starting HubConnection");
        if (transport == null) {
            transport = new WebSocketTransport(url, logger, headers, httpClient);
        }

        transport.setOnReceive(this.callback);
        return transport.start().thenCompose((future) -> {
            String handshake = HandshakeProtocol.createHandshakeRequestMessage(new HandshakeRequestMessage(protocol.getName(), protocol.getVersion()));
            return transport.send(handshake).thenRun(() -> {
                hubConnectionStateLock.lock();
                try {
                    hubConnectionState = HubConnectionState.CONNECTED;
                    connectionState = new ConnectionState(this);
                    logger.log(LogLevel.Information, "HubConnected started.");
                } finally {
                    hubConnectionStateLock.unlock();
                }
            });
        });

    }

    /**
     * Stops a connection to the server.
     */
    private void stop(String errorMessage) {
        HubException hubException = null;
        hubConnectionStateLock.lock();
        try {
            if (hubConnectionState == HubConnectionState.DISCONNECTED) {
                return;
            }

            if (errorMessage != null) {
                logger.log(LogLevel.Error, "HubConnection disconnected with an error %s.", errorMessage);
            } else {
                logger.log(LogLevel.Debug, "Stopping HubConnection.");
            }

            transport.stop();
            hubConnectionState = HubConnectionState.DISCONNECTED;

            if (errorMessage != null) {
                hubException = new HubException(errorMessage);
            }
            connectionState.cancelOutstandingInvocations(hubException);
            connectionState = null;
            logger.log(LogLevel.Information, "HubConnection stopped.");
        } finally {
            hubConnectionStateLock.unlock();
        }

        if (onClosedCallbackList != null) {
            for (Consumer<Exception> callback : onClosedCallbackList) {
                callback.accept(hubException);
            }
        }
    }

    /**
     * Stops a connection to the server.
     */
    public void stop() {
        stop(null);
    }

    /**
     * Invokes a hub method on the server using the specified method name.
     * Does not wait for a response from the receiver.
     *
     * @param method The name of the server method to invoke.
     * @param args   The arguments to be passed to the method.
     * @throws Exception If there was an error while sending.
     */
    public void send(String method, Object... args) throws Exception {
        if (hubConnectionState != HubConnectionState.CONNECTED) {
            throw new HubException("The 'send' method cannot be called if the connection is not active");
        }

        InvocationMessage invocationMessage = new InvocationMessage(null, method, args);
        sendHubMessage(invocationMessage);
    }

    public <T> CompletableFuture<T> invoke(Class<T> returnType, String method, Object... args) throws Exception {
        String id = connectionState.getNextInvocationId();
        InvocationMessage invocationMessage = new InvocationMessage(id, method, args);

        CompletableFuture<T> future = new CompletableFuture<>();
        InvocationRequest irq = new InvocationRequest(returnType, id);
        connectionState.addInvocation(irq);

        // forward the invocation result or error to the user
        // run continuations on a separate thread
        CompletableFuture<Object> pendingCall = irq.getPendingCall();
        pendingCall.whenCompleteAsync((result, error) -> {
            if (error == null) {
                // Primitive types can't be cast with the Class cast function
                if (returnType.isPrimitive()) {
                    future.complete((T)result);
                } else {
                    future.complete(returnType.cast(result));
                }
            } else {
                future.completeExceptionally(error);
            }
        });

        // Make sure the actual send is after setting up the future otherwise there is a race
        // where the map doesn't have the future yet when the response is returned
        sendHubMessage(invocationMessage);

        return future;
    }

    private void sendHubMessage(HubMessage message) throws Exception {
        String serializedMessage = protocol.writeMessage(message);
        if (message.getMessageType() == HubMessageType.INVOCATION) {
            logger.log(LogLevel.Debug, "Sending %d message '%s'.", message.getMessageType().value, ((InvocationMessage)message).getInvocationId());
        } else {
            logger.log(LogLevel.Debug, "Sending %d message.", message.getMessageType().value);
        }
        transport.send(serializedMessage);
    }

    /**
     * Removes all handlers associated with the method with the specified method name.
     *
     * @param name The name of the hub method from which handlers are being removed.
     */
    public void remove(String name) {
        handlers.remove(name);
        logger.log(LogLevel.Trace, "Removing handlers for client method %s", name);
    }

    public void onClosed(Consumer<Exception> callback) {
        if (onClosedCallbackList == null) {
            onClosedCallbackList = new ArrayList<>();
        }

        onClosedCallbackList.add(callback);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     *
     * @param target   The name of the hub method to define.
     * @param callback The handler that will be raised when the hub method is invoked.
     * @return A {@link Subscription} that can be disposed to unsubscribe from the hub method.
     */
    public Subscription on(String target, Action callback) {
        ActionBase action = args -> callback.invoke();
        InvocationHandler handler = handlers.put(target, action, emptyArray);
        logger.log(LogLevel.Trace, "Registering handler for client method: %s", target);
        return new Subscription(handlers, handler, target);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     *
     * @param target   The name of the hub method to define.
     * @param callback The handler that will be raised when the hub method is invoked.
     * @param param1   The first parameter.
     * @param <T1>     The first argument type.
     * @return A {@link Subscription} that can be disposed to unsubscribe from the hub method.
     */
    public <T1> Subscription on(String target, Action1<T1> callback, Class<T1> param1) {
        ActionBase action = params -> callback.invoke(param1.cast(params[0]));
        ArrayList<Class<?>> classes = new ArrayList<>(1);
        classes.add(param1);
        InvocationHandler handler = handlers.put(target, action, classes);
        logger.log(LogLevel.Trace, "Registering handler for client method: %s", target);
        return new Subscription(handlers, handler, target);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     *
     * @param target   The name of the hub method to define.
     * @param callback The handler that will be raised when the hub method is invoked.
     * @param param1   The first parameter.
     * @param param2   The second parameter.
     * @param <T1>     The first parameter type.
     * @param <T2>     The second parameter type.
     * @return A {@link Subscription} that can be disposed to unsubscribe from the hub method.
     */
    public <T1, T2> Subscription on(String target, Action2<T1, T2> callback, Class<T1> param1, Class<T2> param2) {
        ActionBase action = params -> {
            callback.invoke(param1.cast(params[0]), param2.cast(params[1]));
        };
        ArrayList<Class<?>> classes = new ArrayList<>(2);
        classes.add(param1);
        classes.add(param2);
        InvocationHandler handler = handlers.put(target, action, classes);
        logger.log(LogLevel.Trace, "Registering handler for client method: %s", target);
        return new Subscription(handlers, handler, target);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     *
     * @param target   The name of the hub method to define.
     * @param callback The handler that will be raised when the hub method is invoked.
     * @param param1   The first parameter.
     * @param param2   The second parameter.
     * @param param3   The third parameter.
     * @param <T1>     The first parameter type.
     * @param <T2>     The second parameter type.
     * @param <T3>     The third parameter type.
     * @return A {@link Subscription} that can be disposed to unsubscribe from the hub method.
     */
    public <T1, T2, T3> Subscription on(String target, Action3<T1, T2, T3> callback,
                                        Class<T1> param1, Class<T2> param2, Class<T3> param3) {
        ActionBase action = params -> {
            callback.invoke(param1.cast(params[0]), param2.cast(params[1]), param3.cast(params[2]));
        };
        ArrayList<Class<?>> classes = new ArrayList<>(3);
        classes.add(param1);
        classes.add(param2);
        classes.add(param3);
        InvocationHandler handler = handlers.put(target, action, classes);
        logger.log(LogLevel.Trace, "Registering handler for client method: %s", target);
        return new Subscription(handlers, handler, target);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     *
     * @param target   The name of the hub method to define.
     * @param callback The handler that will be raised when the hub method is invoked.
     * @param param1   The first parameter.
     * @param param2   The second parameter.
     * @param param3   The third parameter.
     * @param param4   The fourth parameter.
     * @param <T1>     The first parameter type.
     * @param <T2>     The second parameter type.
     * @param <T3>     The third parameter type.
     * @param <T4>     The fourth parameter type.
     * @return A {@link Subscription} that can be disposed to unsubscribe from the hub method.
     */
    public <T1, T2, T3, T4> Subscription on(String target, Action4<T1, T2, T3, T4> callback,
                                            Class<T1> param1, Class<T2> param2, Class<T3> param3, Class<T4> param4) {
        ActionBase action = params -> {
            callback.invoke(param1.cast(params[0]), param2.cast(params[1]), param3.cast(params[2]), param4.cast(params[3]));
        };
        ArrayList<Class<?>> classes = new ArrayList<>(4);
        classes.add(param1);
        classes.add(param2);
        classes.add(param3);
        classes.add(param4);
        InvocationHandler handler = handlers.put(target, action, classes);
        logger.log(LogLevel.Trace, "Registering handler for client method: %s", target);
        return new Subscription(handlers, handler, target);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     *
     * @param target   The name of the hub method to define.
     * @param callback The handler that will be raised when the hub method is invoked.
     * @param param1   The first parameter.
     * @param param2   The second parameter.
     * @param param3   The third parameter.
     * @param param4   The fourth parameter.
     * @param param5   The fifth parameter.
     * @param <T1>     The first parameter type.
     * @param <T2>     The second parameter type.
     * @param <T3>     The third parameter type.
     * @param <T4>     The fourth parameter type.
     * @param <T5>     The fifth parameter type.
     * @return A {@link Subscription} that can be disposed to unsubscribe from the hub method.
     */
    public <T1, T2, T3, T4, T5> Subscription on(String target, Action5<T1, T2, T3, T4, T5> callback,
                                                Class<T1> param1, Class<T2> param2, Class<T3> param3, Class<T4> param4, Class<T5> param5) {
        ActionBase action = params -> {
            callback.invoke(param1.cast(params[0]), param2.cast(params[1]), param3.cast(params[2]), param4.cast(params[3]),
                    param5.cast(params[4]));
        };
        ArrayList<Class<?>> classes = new ArrayList<>(5);
        classes.add(param1);
        classes.add(param2);
        classes.add(param3);
        classes.add(param4);
        classes.add(param5);
        InvocationHandler handler = handlers.put(target, action, classes);
        logger.log(LogLevel.Trace, "Registering handler for client method: %s", target);
        return new Subscription(handlers, handler, target);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     *
     * @param target   The name of the hub method to define.
     * @param callback The handler that will be raised when the hub method is invoked.
     * @param param1   The first parameter.
     * @param param2   The second parameter.
     * @param param3   The third parameter.
     * @param param4   The fourth parameter.
     * @param param5   The fifth parameter.
     * @param param6   The sixth parameter.
     * @param <T1>     The first parameter type.
     * @param <T2>     The second parameter type.
     * @param <T3>     The third parameter type.
     * @param <T4>     The fourth parameter type.
     * @param <T5>     The fifth parameter type.
     * @param <T6>     The sixth parameter type.
     * @return A {@link Subscription} that can be disposed to unsubscribe from the hub method.
     */
    public <T1, T2, T3, T4, T5, T6> Subscription on(String target, Action6<T1, T2, T3, T4, T5, T6> callback,
                                                    Class<T1> param1, Class<T2> param2, Class<T3> param3, Class<T4> param4, Class<T5> param5, Class<T6> param6) {
        ActionBase action = params -> {
            callback.invoke(param1.cast(params[0]), param2.cast(params[1]), param3.cast(params[2]), param4.cast(params[3]),
                    param5.cast(params[4]), param6.cast(params[5]));
        };
        ArrayList<Class<?>> classes = new ArrayList<>(6);
        classes.add(param1);
        classes.add(param2);
        classes.add(param3);
        classes.add(param4);
        classes.add(param5);
        classes.add(param6);
        InvocationHandler handler = handlers.put(target, action, classes);
        logger.log(LogLevel.Trace, "Registering handler for client method: %s", target);
        return new Subscription(handlers, handler, target);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     *
     * @param target   The name of the hub method to define.
     * @param callback The handler that will be raised when the hub method is invoked.
     * @param param1   The first parameter.
     * @param param2   The second parameter.
     * @param param3   The third parameter.
     * @param param4   The fourth parameter.
     * @param param5   The fifth parameter.
     * @param param6   The sixth parameter.
     * @param param7   The seventh parameter.
     * @param <T1>     The first parameter type.
     * @param <T2>     The second parameter type.
     * @param <T3>     The third parameter type.
     * @param <T4>     The fourth parameter type.
     * @param <T5>     The fifth parameter type.
     * @param <T6>     The sixth parameter type.
     * @param <T7>     The seventh parameter type.
     * @return A {@link Subscription} that can be disposed to unsubscribe from the hub method.
     */
    public <T1, T2, T3, T4, T5, T6, T7> Subscription on(String target, Action7<T1, T2, T3, T4, T5, T6, T7> callback,
                                                        Class<T1> param1, Class<T2> param2, Class<T3> param3, Class<T4> param4, Class<T5> param5, Class<T6> param6, Class<T7> param7) {
        ActionBase action = params -> {
            callback.invoke(param1.cast(params[0]), param2.cast(params[1]), param3.cast(params[2]), param4.cast(params[3]),
                    param5.cast(params[4]), param6.cast(params[5]), param7.cast(params[6]));
        };
        ArrayList<Class<?>> classes = new ArrayList<>(7);
        classes.add(param1);
        classes.add(param2);
        classes.add(param3);
        classes.add(param4);
        classes.add(param5);
        classes.add(param6);
        classes.add(param7);
        InvocationHandler handler = handlers.put(target, action, classes);
        logger.log(LogLevel.Trace, "Registering handler for client method: %s", target);
        return new Subscription(handlers, handler, target);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     *
     * @param target   The name of the hub method to define.
     * @param callback The handler that will be raised when the hub method is invoked.
     * @param param1   The first parameter.
     * @param param2   The second parameter.
     * @param param3   The third parameter.
     * @param param4   The fourth parameter.
     * @param param5   The fifth parameter.
     * @param param6   The sixth parameter.
     * @param param7   The seventh parameter.
     * @param param8   The eighth parameter
     * @param <T1>     The first parameter type.
     * @param <T2>     The second parameter type.
     * @param <T3>     The third parameter type.
     * @param <T4>     The fourth parameter type.
     * @param <T5>     The fifth parameter type.
     * @param <T6>     The sixth parameter type.
     * @param <T7>     The seventh parameter type.
     * @param <T8>     The eighth parameter type.
     * @return A {@link Subscription} that can be disposed to unsubscribe from the hub method.
     */
    public <T1, T2, T3, T4, T5, T6, T7, T8> Subscription on(String target, Action8<T1, T2, T3, T4, T5, T6, T7, T8> callback,
                                                            Class<T1> param1, Class<T2> param2, Class<T3> param3, Class<T4> param4, Class<T5> param5, Class<T6> param6, Class<T7> param7, Class<T8> param8) {
        ActionBase action = params -> {
            callback.invoke(param1.cast(params[0]), param2.cast(params[1]), param3.cast(params[2]), param4.cast(params[3]),
                    param5.cast(params[4]), param6.cast(params[5]), param7.cast(params[6]), param8.cast(params[7]));
        };
        ArrayList<Class<?>> classes = new ArrayList<>(8);
        classes.add(param1);
        classes.add(param2);
        classes.add(param3);
        classes.add(param4);
        classes.add(param5);
        classes.add(param6);
        classes.add(param7);
        classes.add(param8);
        InvocationHandler handler = handlers.put(target, action, classes);
        logger.log(LogLevel.Trace, "Registering handler for client method: %s", target);
        return new Subscription(handlers, handler, target);
    }

    private class ConnectionState implements InvocationBinder {
        private HubConnection connection;
        private AtomicInteger nextId = new AtomicInteger(0);
        private HashMap<String, InvocationRequest> pendingInvocations = new HashMap<>();
        private Lock lock = new ReentrantLock();

        public ConnectionState(HubConnection connection) {
            this.connection = connection;
        }

        public String getNextInvocationId() {
            int i = nextId.incrementAndGet();
            return Integer.toString(i);
        }

        public void cancelOutstandingInvocations(Exception ex) {
            lock.lock();
            try {
                pendingInvocations.forEach((key, irq) -> {
                    if (ex == null) {
                        irq.cancel();
                    } else {
                        irq.fail(ex);
                    }
                });

                pendingInvocations.clear();
            } finally {
                lock.unlock();
            }
        }

        public void addInvocation(InvocationRequest irq) {
            lock.lock();
            try {
                pendingInvocations.compute(irq.getInvocationId(), (key, value) -> {
                    if (value != null) {
                        // This should never happen
                        throw new IllegalStateException("Invocation Id is already used");
                    }

                    return irq;
                });
            } finally {
                lock.unlock();
            }
        }

        public InvocationRequest getInvocation(String id) {
            lock.lock();
            try {
                return pendingInvocations.get(id);
            } finally {
                lock.unlock();
            }
        }

        public InvocationRequest tryRemoveInvocation(String id) {
            lock.lock();
            try {
                return pendingInvocations.remove(id);
            } finally {
                lock.unlock();
            }
        }

        @Override
        public Class<?> getReturnType(String invocationId) {
            InvocationRequest irq = getInvocation(invocationId);
            if (irq == null) {
                return null;
            }

            return irq.getReturnType();
        }

        @Override
        public List<Class<?>> getParameterTypes(String methodName) throws Exception {
            List<InvocationHandler> handlers = connection.handlers.get(methodName);
            if (handlers == null) {
                logger.log(LogLevel.Warning, "Failed to find handler for '%s' method.", methodName);
                return emptyArray;
            }

            if (handlers.size() == 0) {
                throw new Exception(String.format("There are no callbacks registered for the method '%s'.", methodName));
            }

            return handlers.get(0).getClasses();
        }
    }
}