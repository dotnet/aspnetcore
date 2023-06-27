// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import java.io.StringReader;
import java.lang.reflect.Type;
import java.nio.ByteBuffer;
import java.nio.charset.StandardCharsets;
import java.util.*;
import java.util.concurrent.*;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.concurrent.atomic.AtomicLong;
import java.util.concurrent.locks.Lock;
import java.util.concurrent.locks.ReentrantLock;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import com.google.gson.stream.JsonReader;

import io.reactivex.rxjava3.core.Completable;
import io.reactivex.rxjava3.core.Observable;
import io.reactivex.rxjava3.core.Single;
import io.reactivex.rxjava3.schedulers.Schedulers;
import io.reactivex.rxjava3.subjects.*;
import okhttp3.OkHttpClient;

/**
 * A connection used to invoke hub methods on a SignalR Server.
 */
public class HubConnection implements AutoCloseable {
    static final long DEFAULT_SERVER_TIMEOUT = 30 * 1000;
    static final long DEFAULT_KEEP_ALIVE_INTERVAL = 15 * 1000;

    private static final byte RECORD_SEPARATOR = 0x1e;
    private static final List<Type> emptyArray = new ArrayList<>();
    private static final int MAX_NEGOTIATE_ATTEMPTS = 100;

    private final CallbackMap handlers = new CallbackMap();
    private final HubProtocol protocol;
    private final boolean skipNegotiate;
    private final Map<String, String> headers;
    private final int negotiateVersion = 1;
    private final Logger logger = LoggerFactory.getLogger(HubConnection.class);
    private final HttpClient httpClient;
    private final Transport customTransport;
    private final OnReceiveCallBack callback;
    private final Single<String> accessTokenProvider;
    private final TransportEnum transportEnum;

    // These are all user-settable properties
    private String baseUrl;
    private List<OnClosedCallback> onClosedCallbackList;
    private long keepAliveInterval = DEFAULT_KEEP_ALIVE_INTERVAL;
    private long serverTimeout = DEFAULT_SERVER_TIMEOUT;
    private long handshakeResponseTimeout = 15 * 1000;

    // Private property, modified for testing
    private long tickRate = 1000;

    // Holds all mutable state other than user-defined handlers and settable properties.
    private final ReconnectingConnectionState state;

    /**
     * Sets the server timeout interval for the connection.
     *
     * @param serverTimeoutInMilliseconds The server timeout duration (specified in milliseconds).
     */
    public void setServerTimeout(long serverTimeoutInMilliseconds) {
        this.serverTimeout = serverTimeoutInMilliseconds;
    }

    /**
     * Gets the server timeout duration.
     *
     * @return The server timeout duration (specified in milliseconds).
     */
    public long getServerTimeout() {
        return this.serverTimeout;
    }

    /**
     * Sets the keep alive interval duration.
     *
     * @param keepAliveIntervalInMilliseconds The interval (specified in milliseconds) at which the connection should send keep alive messages.
     */
    public void setKeepAliveInterval(long keepAliveIntervalInMilliseconds) {
        this.keepAliveInterval = keepAliveIntervalInMilliseconds;
    }

    /**
     * Gets the keep alive interval.
     *
     * @return The interval (specified in milliseconds) between keep alive messages.
     */
    public long getKeepAliveInterval() {
        return this.keepAliveInterval;
    }

    /**
     *  Gets the connections connectionId. This value will be cleared when the connection is stopped and
     *  will have a new value every time the connection is successfully started.
     * @return A string representing the the client's connectionId.
     */
    public String getConnectionId() {
        ConnectionState state = this.state.getConnectionStateUnsynchronized(true);
        if (state != null) {
            return state.connectionId;
        }
        return null;
    }

    // For testing purposes
    void setTickRate(long tickRateInMilliseconds) {
        this.tickRate = tickRateInMilliseconds;
    }

    // For testing purposes
    Transport getTransport() {
        return this.state.getConnectionState().transport;
    }

    HubConnection(String url, Transport transport, boolean skipNegotiate, HttpClient httpClient, HubProtocol protocol,
                  Single<String> accessTokenProvider, long handshakeResponseTimeout, Map<String, String> headers, TransportEnum transportEnum,
                  Action1<OkHttpClient.Builder> configureBuilder, long serverTimeout, long keepAliveInterval) {
        if (url == null || url.isEmpty()) {
            throw new IllegalArgumentException("A valid url is required.");
        }

        this.state = new ReconnectingConnectionState(this.logger);
        this.baseUrl = url;
        this.protocol = protocol;

        if (accessTokenProvider != null) {
            this.accessTokenProvider = accessTokenProvider;
        } else {
            this.accessTokenProvider = Single.just("");
        }

        if (httpClient != null) {
            this.httpClient = httpClient;
        } else {
            this.httpClient = new DefaultHttpClient(configureBuilder);
        }

        if (transport != null) {
            this.transportEnum = TransportEnum.ALL;
            this.customTransport = transport;
        } else if (transportEnum != null) {
            this.transportEnum = transportEnum;
            this.customTransport = null;
        } else {
            this.transportEnum = TransportEnum.ALL;
            this.customTransport = null;
        }

        if (handshakeResponseTimeout > 0) {
            this.handshakeResponseTimeout = handshakeResponseTimeout;
        }

        this.headers = headers;
        this.skipNegotiate = skipNegotiate;

        this.serverTimeout = serverTimeout;
        this.keepAliveInterval = keepAliveInterval;

        this.callback = (payload) -> ReceiveLoop(payload);
    }

    private Single<NegotiateResponse> handleNegotiate(String url, Map<String, String> localHeaders) {
        HttpRequest request = new HttpRequest();
        request.addHeaders(localHeaders);

        return httpClient.post(Negotiate.resolveNegotiateUrl(url, this.negotiateVersion), request).map((response) -> {
            if (response.getStatusCode() != 200) {
                throw new HttpRequestException(String.format("Unexpected status code returned from negotiate: %d %s.",
                        response.getStatusCode(), response.getStatusText()), response.getStatusCode());
            }
            JsonReader reader = new JsonReader(new StringReader(new String(response.getContent().array(), StandardCharsets.UTF_8)));
            NegotiateResponse negotiateResponse = new NegotiateResponse(reader);

            if (negotiateResponse.getError() != null) {
                throw new RuntimeException(negotiateResponse.getError());
            }

            if (negotiateResponse.getAccessToken() != null) {
                localHeaders.put("Authorization", "Bearer " + negotiateResponse.getAccessToken());
            }

            return negotiateResponse;
        });
    }

    /**
     * Indicates the state of the {@link HubConnection} to the server.
     *
     * @return HubConnection state enum.
     */
    public HubConnectionState getConnectionState() {
        return this.state.getHubConnectionState();
    }

    // For testing only
    String getBaseUrl() {
        return this.baseUrl;
    }

    /**
     * Sets a new url for the HubConnection.
     * @param url The url to connect to.
     */
    public void setBaseUrl(String url) {
        if (url == null || url.isEmpty()) {
            throw new IllegalArgumentException("The HubConnection url must be a valid url.");
        }

        if (this.state.getHubConnectionState() != HubConnectionState.DISCONNECTED) {
            throw new IllegalStateException("The HubConnection must be in the disconnected state to change the url.");
        }

        this.baseUrl = url;
    }

    /**
     * Starts a connection to the server.
     *
     * @return A Completable that completes when the connection has been established.
     */
    public Completable start() {
        CompletableSubject localStart = CompletableSubject.create();

        this.state.lock.lock();
        try {
            if (this.state.getHubConnectionState() != HubConnectionState.DISCONNECTED) {
                logger.debug("The connection is in the '{}' state. Waiting for in-progress start to complete or completing this start immediately.", this.state.getHubConnectionState());
                return this.state.getConnectionStateUnsynchronized(false).startTask;
            }

            this.state.changeState(HubConnectionState.DISCONNECTED, HubConnectionState.CONNECTING);

            CompletableSubject tokenCompletable = CompletableSubject.create();
            Map<String, String> localHeaders = new HashMap<>();
            localHeaders.put(UserAgentHelper.getUserAgentName(), UserAgentHelper.createUserAgentString());
            if (headers != null) {
                localHeaders.putAll(headers);
            }
            ConnectionState connectionState = new ConnectionState(this);
            this.state.setConnectionState(connectionState);
            connectionState.startTask = localStart;

            accessTokenProvider.subscribe(token -> {
                if (token != null && !token.isEmpty()) {
                    localHeaders.put("Authorization", "Bearer " + token);
                }
                tokenCompletable.onComplete();
            }, error -> {
                tokenCompletable.onError(error);
            });

            Single<NegotiateResponse> negotiate = null;
            if (!skipNegotiate) {
                negotiate = tokenCompletable.andThen(Single.defer(() -> startNegotiate(baseUrl, 0, localHeaders)));
            } else {
                negotiate = tokenCompletable.andThen(Single.defer(() -> Single.just(new NegotiateResponse(baseUrl))));
            }

            negotiate.flatMapCompletable(negotiateResponse -> {
                logger.debug("Starting HubConnection.");
                Transport transport = customTransport;
                if (transport == null) {
                    Single<String> tokenProvider = negotiateResponse.getAccessToken() != null ? Single.just(negotiateResponse.getAccessToken()) : accessTokenProvider;
                    TransportEnum chosenTransport;
                    if (this.skipNegotiate) {
                        if (this.transportEnum != TransportEnum.WEBSOCKETS) {
                            throw new RuntimeException("Negotiation can only be skipped when using the WebSocket transport directly with '.withTransport(TransportEnum.WEBSOCKETS)' on the 'HubConnectionBuilder'.");
                        }
                        chosenTransport = this.transportEnum;
                    } else {
                        chosenTransport = negotiateResponse.getChosenTransport();
                    }
                    switch (chosenTransport) {
                        case LONG_POLLING:
                            transport = new LongPollingTransport(localHeaders, httpClient, tokenProvider);
                            break;
                        default:
                            transport = new WebSocketTransport(localHeaders, httpClient);
                    }
                }

                connectionState.transport = transport;

                transport.setOnReceive(this.callback);
                transport.setOnClose((message) -> stopConnection(message));

                return transport.start(negotiateResponse.getFinalUrl()).andThen(Completable.defer(() -> {
                    ByteBuffer handshake = HandshakeProtocol.createHandshakeRequestMessage(
                                new HandshakeRequestMessage(protocol.getName(), protocol.getVersion()));

                    this.state.lock();
                    try {
                        if (this.state.hubConnectionState != HubConnectionState.CONNECTING) {
                            return Completable.error(new RuntimeException("Connection closed while trying to connect."));
                        }
                        return connectionState.transport.send(handshake).andThen(Completable.defer(() -> {
                            this.state.lock();
                            try {
                                ConnectionState activeState = this.state.getConnectionStateUnsynchronized(true);
                                if (activeState != null && activeState == connectionState) {
                                    connectionState.timeoutHandshakeResponse(handshakeResponseTimeout, TimeUnit.MILLISECONDS);
                                } else {
                                    return Completable.error(new RuntimeException("Connection closed while sending handshake."));
                                }
                            } finally {
                                this.state.unlock();
                            }
                            return connectionState.handshakeResponseSubject.andThen(Completable.defer(() -> {
                                this.state.lock();
                                try {
                                    ConnectionState activeState = this.state.getConnectionStateUnsynchronized(true);
                                    if (activeState == null || activeState != connectionState) {
                                        return Completable.error(new RuntimeException("Connection closed while waiting for handshake."));
                                    }
                                    this.state.changeState(HubConnectionState.CONNECTING, HubConnectionState.CONNECTED);
                                    logger.info("HubConnection started.");
                                    connectionState.resetServerTimeout();
                                    // Don't send pings if we're using long polling.
                                    if (negotiateResponse.getChosenTransport() != TransportEnum.LONG_POLLING) {
                                        connectionState.activatePingTimer();
                                    }
                                } finally {
                                    this.state.unlock();
                                }

                                return Completable.complete();
                            }));
                        }));
                    } finally {
                        this.state.unlock();
                    }
                }));
            // subscribe makes this a "hot" completable so this runs immediately
            }).subscribe(() -> {
                localStart.onComplete();
            }, error -> {
                this.state.lock();
                try {
                    ConnectionState activeState = this.state.getConnectionStateUnsynchronized(true);
                    if (activeState == connectionState) {
                        this.state.changeState(HubConnectionState.CONNECTING, HubConnectionState.DISCONNECTED);
                    }
                // this error is already logged and we want the user to see the original error
                } catch (Exception ex) {
                } finally {
                    this.state.unlock();
                }

                localStart.onError(error);
            });
        } finally {
            this.state.lock.unlock();
        }

        return localStart;
    }

    private Single<NegotiateResponse> startNegotiate(String url, int negotiateAttempts, Map<String, String> localHeaders) {
        if (this.state.getHubConnectionState() != HubConnectionState.CONNECTING) {
            throw new RuntimeException("HubConnection trying to negotiate when not in the CONNECTING state.");
        }

        return handleNegotiate(url, localHeaders).flatMap(response -> {
            if (response.getRedirectUrl() != null && negotiateAttempts >= MAX_NEGOTIATE_ATTEMPTS) {
                throw new RuntimeException("Negotiate redirection limit exceeded.");
            }

            if (response.getRedirectUrl() == null) {
                Set<String> transports = response.getAvailableTransports();
                if (this.transportEnum == TransportEnum.ALL) {
                    if (transports.contains("WebSockets")) {
                        response.setChosenTransport(TransportEnum.WEBSOCKETS);
                    } else if (transports.contains("LongPolling")) {
                        response.setChosenTransport(TransportEnum.LONG_POLLING);
                    } else {
                        throw new RuntimeException("There were no compatible transports on the server.");
                    }
                } else if (this.transportEnum == TransportEnum.WEBSOCKETS && !transports.contains("WebSockets") ||
                        (this.transportEnum == TransportEnum.LONG_POLLING && !transports.contains("LongPolling"))) {
                    throw new RuntimeException("There were no compatible transports on the server.");
                } else {
                    response.setChosenTransport(this.transportEnum);
                }

                String connectionToken = "";
                if (response.getVersion() > 0) {
                    this.state.getConnectionState().connectionId = response.getConnectionId();
                    connectionToken = response.getConnectionToken();
                } else {
                    connectionToken = response.getConnectionId();
                    this.state.getConnectionState().connectionId = connectionToken;
                }

                String finalUrl = Utils.appendQueryString(url, "id=" + connectionToken);

                response.setFinalUrl(finalUrl);
                return Single.just(response);
            }

            return startNegotiate(response.getRedirectUrl(), negotiateAttempts + 1, localHeaders);
        });
    }

    /**
     * Stops a connection to the server.
     *
     * @param errorMessage An error message if the connected needs to be stopped because of an error.
     * @return A Completable that completes when the connection has been stopped.
     */
    private Completable stop(String errorMessage) {
        ConnectionState connectionState;
        Completable startTask;
        this.state.lock();
        try {
            if (this.state.getHubConnectionState() == HubConnectionState.DISCONNECTED) {
                return Completable.complete();
            }

            connectionState = this.state.getConnectionStateUnsynchronized(false);

            if (errorMessage != null) {
                connectionState.stopError = errorMessage;
                logger.error("HubConnection disconnected with an error: {}.", errorMessage);
            } else {
                if (this.state.getHubConnectionState() == HubConnectionState.CONNECTED) {
                    sendHubMessageWithLock(new CloseMessage());
                }
                logger.debug("Stopping HubConnection.");
            }

            startTask = connectionState.startTask;
        } finally {
            this.state.unlock();
        }

        CompletableSubject subject = CompletableSubject.create();
        startTask.onErrorComplete().subscribe(() ->
        {
            Completable stop = connectionState.transport.stop();
            stop.subscribe(() -> subject.onComplete(), e -> subject.onError(e));
        });

        return subject;
    }

    private void ReceiveLoop(ByteBuffer payload)
    {
        List<HubMessage> messages;
        ConnectionState connectionState;
        this.state.lock();
        try {
            connectionState = this.state.getConnectionState();
            connectionState.resetServerTimeout();
            connectionState.handleHandshake(payload);
            // The payload only contained the handshake response so we can return.
            if (!payload.hasRemaining()) {
                return;
            }

            messages = protocol.parseMessages(payload, connectionState);
        } finally {
            this.state.unlock();
        }

        for (HubMessage message : messages) {
            logger.debug("Received message of type {}.", message.getMessageType());
            switch (message.getMessageType()) {
                case INVOCATION_BINDING_FAILURE:
                    InvocationBindingFailureMessage msg = (InvocationBindingFailureMessage)message;
                    logger.error("Failed to bind arguments received in invocation '{}' of '{}'.", msg.getInvocationId(), msg.getTarget(), msg.getException());

                    if (msg.getInvocationId() != null) {
                        sendHubMessageWithLock(new CompletionMessage(null, msg.getInvocationId(),
                            null, "Client failed to parse argument(s)."));
                    }
                    break;
                case INVOCATION:
                    InvocationMessage invocationMessage = (InvocationMessage) message;
                    connectionState.dispatchInvocation(invocationMessage);
                    break;
                case CLOSE:
                    logger.info("Close message received from server.");
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
                        logger.warn("Dropped unsolicited Completion message for invocation '{}'.", completionMessage.getInvocationId());
                        continue;
                    }
                    irq.complete(completionMessage);
                    break;
                case STREAM_ITEM:
                    StreamItem streamItem = (StreamItem)message;
                    InvocationRequest streamInvocationRequest = connectionState.getInvocation(streamItem.getInvocationId());
                    if (streamInvocationRequest == null) {
                        logger.warn("Dropped unsolicited Completion message for invocation '{}'.", streamItem.getInvocationId());
                        continue;
                    }

                    streamInvocationRequest.addItem(streamItem);
                    break;
                case STREAM_INVOCATION:
                case CANCEL_INVOCATION:
                    logger.error("This client does not support {} messages.", message.getMessageType());

                    throw new UnsupportedOperationException(String.format("The message type %s is not supported yet.", message.getMessageType()));
            }
        }
    }

    /**
     * Stops a connection to the server.
     *
     * @return A Completable that completes when the connection has been stopped.
     */
    public Completable stop() {
        return stop(null);
    }

    private void stopConnection(String errorMessage) {
        RuntimeException exception = null;
        this.state.lock();
        try {
            ConnectionState connectionState = this.state.getConnectionStateUnsynchronized(true);

            if (connectionState == null)
            {
                this.logger.error("'stopConnection' called with a null ConnectionState. This is not expected, please file a bug. https://github.com/dotnet/aspnetcore/issues/new?assignees=&labels=&template=bug_report.md");
                return;
            }

            // errorMessage gets passed in from the transport. An already existing stopError value
            // should take precedence.
            if (connectionState.stopError != null) {
                errorMessage = connectionState.stopError;
            }
            if (errorMessage != null) {
                exception = new RuntimeException(errorMessage);
                logger.error("HubConnection disconnected with an error {}.", errorMessage);
            }

            this.state.setConnectionState(null);
            connectionState.cancelOutstandingInvocations(exception);
            connectionState.close();

            logger.info("HubConnection stopped.");
            // We can be in the CONNECTING or CONNECTED state here, depending on if the handshake response was received or not.
            // connectionState.close() above will exit the Start call with an error if it's still running
            this.state.changeState(HubConnectionState.DISCONNECTED);
        } finally {
            this.state.unlock();
        }

        // Do not run these callbacks inside the hubConnectionStateLock
        if (onClosedCallbackList != null) {
            for (OnClosedCallback callback : onClosedCallbackList) {
                try {
                    callback.invoke(exception);
                } catch (Exception ex) {
                    logger.warn("Invoking 'onClosed' method failed:", ex);
                }
            }
        }
    }

    /**
     * Invokes a hub method on the server using the specified method name.
     * Does not wait for a response from the receiver.
     *
     * @param method The name of the server method to invoke.
     * @param args   The arguments to be passed to the method.
     */
    public void send(String method, Object... args) {
        this.state.lock();
        try {
            if (this.state.getHubConnectionState() != HubConnectionState.CONNECTED) {
                throw new RuntimeException("The 'send' method cannot be called if the connection is not active.");
            }
            sendInvocationMessage(method, args);
        } finally {
            this.state.unlock();
        }
    }

    private void sendInvocationMessage(String method, Object[] args) {
        sendInvocationMessage(method, args, null, false);
    }

    private void sendInvocationMessage(String method, Object[] args, String id, Boolean isStreamInvocation) {
        List<String> streamIds = new ArrayList<>();
        List<Observable> streams = new ArrayList<>();
        args = checkUploadStream(args, streamIds, streams);
        InvocationMessage invocationMessage;
        if (isStreamInvocation) {
            invocationMessage = new StreamInvocationMessage(null, id, method, args, streamIds);
        } else {
            invocationMessage = new InvocationMessage(null, id, method, args, streamIds);
        }

        sendHubMessageWithLock(invocationMessage);
        launchStreams(streamIds, streams);
    }

    void launchStreams(List<String> streamIds, List<Observable> streams) {
        if (streams.isEmpty()) {
            return;
        }

        for (int i = 0; i < streamIds.size(); i++) {
            String streamId = streamIds.get(i);
            Observable stream = streams.get(i);
            stream.subscribe(
                (item) -> sendHubMessageWithLock(new StreamItem(null, streamId, item)),
                (error) -> {
                    sendHubMessageWithLock(new CompletionMessage(null, streamId, null, error.toString()));
                },
                () -> {
                    sendHubMessageWithLock(new CompletionMessage(null, streamId, null, null));
                });
        }
    }

    Object[] checkUploadStream(Object[] args, List<String> streamIds, List<Observable> streams) {
        if (args == null) {
            return new Object[] { null };
        }

        ConnectionState connectionState = this.state.getConnectionState();
        List<Object> params = new ArrayList<>(Arrays.asList(args));
        for (Object arg: args) {
            if (arg instanceof Observable) {
                params.remove(arg);
                Observable stream = (Observable)arg;
                String streamId = connectionState.getNextInvocationId();
                streamIds.add(streamId);
                streams.add(stream);
            }
        }

        return params.toArray();
    }

    /**
     * Invokes a hub method on the server using the specified method name and arguments.
     *
     * @param method The name of the server method to invoke.
     * @param args The arguments used to invoke the server method.
     * @return A Completable that indicates when the invocation has completed.
     */
    public Completable invoke(String method, Object... args) {
        this.state.lock();
        try {
            if (this.state.getHubConnectionState() != HubConnectionState.CONNECTED) {
                throw new RuntimeException("The 'invoke' method cannot be called if the connection is not active.");
            }

            ConnectionState connectionState = this.state.getConnectionStateUnsynchronized(false);
            String id = connectionState.getNextInvocationId();

            CompletableSubject subject = CompletableSubject.create();
            InvocationRequest irq = new InvocationRequest(null, id);
            connectionState.addInvocation(irq);

            Subject<Object> pendingCall = irq.getPendingCall();

            pendingCall.subscribe(result -> subject.onComplete(),
                    error -> subject.onError(error),
                    () -> subject.onComplete());

            // Make sure the actual send is after setting up the callbacks otherwise there is a race
            // where the map doesn't have the callbacks yet when the response is returned
            sendInvocationMessage(method, args, id, false);
            return subject;
        } finally {
            this.state.unlock();
        }
    }

    /**
     * Invokes a hub method on the server using the specified method name and arguments.
     *
     * @param returnType The expected return type.
     * @param method The name of the server method to invoke.
     * @param args The arguments used to invoke the server method.
     * @param <T> The expected return type.
     * @return A Single that yields the return value when the invocation has completed.
     */
    public <T> Single<T> invoke(Class<T> returnType, String method, Object... args) {
        return this.<T>invoke(returnType, returnType, method, args);
    }

    /**
     * Invokes a hub method on the server using the specified method name and arguments.
     * A Type can be retrieved using {@link TypeReference}
     *
     * @param returnType The expected return type.
     * @param method The name of the server method to invoke.
     * @param args The arguments used to invoke the server method.
     * @param <T> The expected return type.
     * @return A Single that yields the return value when the invocation has completed.
     */
    public <T> Single<T> invoke(Type returnType, String method, Object... args) {
        Class<?> returnClass = Utils.typeToClass(returnType);
        return this.<T>invoke(returnType, returnClass, method, args);
    }

    @SuppressWarnings("unchecked")
    private <T> Single<T> invoke(Type returnType, Class<?> returnClass, String method, Object... args) {
        this.state.lock();
        try {
            if (this.state.getHubConnectionState() != HubConnectionState.CONNECTED) {
                throw new RuntimeException("The 'invoke' method cannot be called if the connection is not active.");
            }

            ConnectionState connectionState = this.state.getConnectionStateUnsynchronized(false);
            String id = connectionState.getNextInvocationId();
            InvocationRequest irq = new InvocationRequest(returnType, id);
            connectionState.addInvocation(irq);

            SingleSubject<T> subject = SingleSubject.create();

            // forward the invocation result or error to the user
            // run continuations on a separate thread
            Subject<Object> pendingCall = irq.getPendingCall();
            pendingCall.subscribe(result -> {
                subject.onSuccess(Utils.<T>cast(returnClass, result));
            }, error -> subject.onError(error));

            // Make sure the actual send is after setting up the callbacks otherwise there is a race
            // where the map doesn't have the callbacks yet when the response is returned
            sendInvocationMessage(method, args, id, false);
            return subject;
        } finally {
            this.state.unlock();
        }
    }

    /**
     * Invokes a streaming hub method on the server using the specified name and arguments.
     *
     * @param returnType The expected return type of the stream items.
     * @param method The name of the server method to invoke.
     * @param args The arguments used to invoke the server method.
     * @param <T> The expected return type.
     * @return An observable that yields the streaming results from the server.
     */
    public <T> Observable<T> stream(Class<T> returnType, String method, Object ... args) {
        return this.<T>stream(returnType, returnType, method, args);
    }

    /**
     * Invokes a streaming hub method on the server using the specified name and arguments.
     *
     * @param returnType The expected return type of the stream items.
     * @param method The name of the server method to invoke.
     * @param args The arguments used to invoke the server method.
     * @param <T> The expected return type.
     * @return An observable that yields the streaming results from the server.
     */
    public <T> Observable<T> stream(Type returnType, String method, Object ... args) {
        Class<?> returnClass = Utils.typeToClass(returnType);
        return this.<T>stream(returnType, returnClass, method, args);
    }

    @SuppressWarnings("unchecked")
    private <T> Observable<T> stream(Type returnType, Class<?> returnClass, String method, Object ... args) {
        String invocationId;
        InvocationRequest irq;
        this.state.lock();
        try {
            if (this.state.getHubConnectionState() != HubConnectionState.CONNECTED) {
                throw new RuntimeException("The 'stream' method cannot be called if the connection is not active.");
            }

            ConnectionState connectionState = this.state.getConnectionStateUnsynchronized(false);
            invocationId = connectionState.getNextInvocationId();
            irq = new InvocationRequest(returnType, invocationId);
            connectionState.addInvocation(irq);

            AtomicInteger subscriptionCount = new AtomicInteger();
            ReplaySubject<T> subject = ReplaySubject.create();
            Subject<Object> pendingCall = irq.getPendingCall();
            pendingCall.subscribe(result -> {
                        subject.onNext(Utils.<T>cast(returnClass, result));
                    }, error -> subject.onError(error),
                    () -> subject.onComplete());

            Observable<T> observable = subject.doOnSubscribe((subscriber) -> subscriptionCount.incrementAndGet());
            sendInvocationMessage(method, args, invocationId, true);
            return observable.doOnDispose(() -> {
                if (subscriptionCount.decrementAndGet() == 0) {
                    CancelInvocationMessage cancelInvocationMessage = new CancelInvocationMessage(null, invocationId);
                    sendHubMessageWithLock(cancelInvocationMessage);
                    connectionState.tryRemoveInvocation(invocationId);
                    subject.onComplete();
                }
            });
        } finally {
            this.state.unlock();
        }
    }

    private void sendHubMessageWithLock(HubMessage message) {
        this.state.lock();
        try {
            if (this.state.getHubConnectionState() != HubConnectionState.CONNECTED) {
                throw new RuntimeException("Trying to send and message while the connection is not active.");
            }
            ByteBuffer serializedMessage = protocol.writeMessage(message);
            if (message.getMessageType() == HubMessageType.INVOCATION) {
                logger.debug("Sending {} message '{}'.", message.getMessageType().name(), ((InvocationMessage)message).getInvocationId());
            } else  if (message.getMessageType() == HubMessageType.STREAM_INVOCATION) {
                logger.debug("Sending {} message '{}'.", message.getMessageType().name(), ((StreamInvocationMessage)message).getInvocationId());
            } else {
                logger.debug("Sending {} message.", message.getMessageType().name());
            }

            ConnectionState connectionState = this.state.getConnectionStateUnsynchronized(false);
            connectionState.transport.send(serializedMessage).subscribeWith(CompletableSubject.create());
            connectionState.resetKeepAlive();
        } finally {
            this.state.unlock();
        }
    }

    /**
     * Removes all handlers associated with the method with the specified method name.
     *
     * @param name The name of the hub method from which handlers are being removed.
     */
    public void remove(String name) {
        handlers.remove(name);
        logger.trace("Removing handlers for client method: {}.", name);
    }

    /**
     * Registers a callback to run when the connection is closed.
     *
     * @param callback A callback to run when the connection closes.
     */
    public void onClosed(OnClosedCallback callback) {
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
        ActionBase action = args -> {
            callback.invoke();
            return Completable.complete();
        };
        return registerHandler(target, action);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     * Should be used for primitives and non-generic classes.
     *
     * @param target   The name of the hub method to define.
     * @param callback The handler that will be raised when the hub method is invoked.
     * @param param1   The first parameter.
     * @param <T1>     The first argument type.
     * @return A {@link Subscription} that can be disposed to unsubscribe from the hub method.
     */
    public <T1> Subscription on(String target, Action1<T1> callback, Class<T1> param1) {
        ActionBase action = params -> {
            callback.invoke(Utils.<T1>cast(param1, params[0]));
            return Completable.complete();
        };
        return registerHandler(target, action, param1);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     * Should be used for primitives and non-generic classes.
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
            callback.invoke(Utils.<T1>cast(param1, params[0]), Utils.<T2>cast(param2, params[1]));
            return Completable.complete();
        };
        return registerHandler(target, action, param1, param2);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     * Should be used for primitives and non-generic classes.
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
            callback.invoke(Utils.<T1>cast(param1, params[0]), Utils.<T2>cast(param2, params[1]), Utils.<T3>cast(param3, params[2]));
            return Completable.complete();
        };
        return registerHandler(target, action, param1, param2, param3);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     * Should be used for primitives and non-generic classes.
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
            callback.invoke(Utils.<T1>cast(param1, params[0]), Utils.<T2>cast(param2, params[1]), Utils.<T3>cast(param3, params[2]),
                Utils.<T4>cast(param4, params[3]));
            return Completable.complete();
        };
        return registerHandler(target, action, param1, param2, param3, param4);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     * Should be used for primitives and non-generic classes.
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
            callback.invoke(Utils.<T1>cast(param1, params[0]), Utils.<T2>cast(param2, params[1]), Utils.<T3>cast(param3, params[2]),
                Utils.<T4>cast(param4, params[3]), Utils.<T5>cast(param5, params[4]));
            return Completable.complete();
        };
        return registerHandler(target, action, param1, param2, param3, param4, param5);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     * Should be used for primitives and non-generic classes.
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
            callback.invoke(Utils.<T1>cast(param1, params[0]), Utils.<T2>cast(param2, params[1]), Utils.<T3>cast(param3, params[2]),
                Utils.<T4>cast(param4, params[3]), Utils.<T5>cast(param5, params[4]), Utils.<T6>cast(param6, params[5]));
            return Completable.complete();
        };
        return registerHandler(target, action, param1, param2, param3, param4, param5, param6);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     * Should be used for primitives and non-generic classes.
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
            callback.invoke(Utils.<T1>cast(param1, params[0]), Utils.<T2>cast(param2, params[1]), Utils.<T3>cast(param3, params[2]),
                Utils.<T4>cast(param4, params[3]), Utils.<T5>cast(param5, params[4]), Utils.<T6>cast(param6, params[5]), Utils.<T7>cast(param7, params[6]));
            return Completable.complete();
        };
        return registerHandler(target, action, param1, param2, param3, param4, param5, param6, param7);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     * Should be used for primitives and non-generic classes.
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
            callback.invoke(Utils.<T1>cast(param1, params[0]), Utils.<T2>cast(param2, params[1]), Utils.<T3>cast(param3, params[2]),
                Utils.<T4>cast(param4, params[3]), Utils.<T5>cast(param5, params[4]), Utils.<T6>cast(param6, params[5]), Utils.<T7>cast(param7, params[6]),
                Utils.<T8>cast(param8, params[7]));
            return Completable.complete();
        };
        return registerHandler(target, action, param1, param2, param3, param4, param5, param6, param7, param8);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     * Should be used for generic classes and Parameterized Collections, like List or Map.
     * A Type can be retrieved using {@link TypeReference}
     *
     * @param target   The name of the hub method to define.
     * @param callback The handler that will be raised when the hub method is invoked.
     * @param param1   The first parameter.
     * @param <T1>     The first argument type.
     * @return A {@link Subscription} that can be disposed to unsubscribe from the hub method.
     */
    public <T1> Subscription on(String target, Action1<T1> callback, Type param1) {
        ActionBase action = params -> {
            callback.invoke(Utils.<T1>cast(param1, params[0]));
            return Completable.complete();
        };
        return registerHandler(target, action, param1);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     * Should be used for generic classes and Parameterized Collections, like List or Map.
     * A Type can be retrieved using {@link TypeReference}
     *
     * @param target   The name of the hub method to define.
     * @param callback The handler that will be raised when the hub method is invoked.
     * @param param1   The first parameter.
     * @param param2   The second parameter.
     * @param <T1>     The first parameter type.
     * @param <T2>     The second parameter type.
     * @return A {@link Subscription} that can be disposed to unsubscribe from the hub method.
     */
    public <T1, T2> Subscription on(String target, Action2<T1, T2> callback, Type param1, Type param2) {
        ActionBase action = params -> {
            callback.invoke(Utils.<T1>cast(param1, params[0]), Utils.<T2>cast(param2, params[1]));
            return Completable.complete();
        };
        return registerHandler(target, action, param1, param2);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     * Should be used for generic classes and Parameterized Collections, like List or Map.
     * A Type can be retrieved using {@link TypeReference}
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
                                        Type param1, Type param2, Type param3) {
        ActionBase action = params -> {
            callback.invoke(Utils.<T1>cast(param1, params[0]), Utils.<T2>cast(param2, params[1]), Utils.<T3>cast(param3, params[2]));
            return Completable.complete();
        };
        return registerHandler(target, action, param1, param2, param3);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     * Should be used for generic classes and Parameterized Collections, like List or Map.
     * A Type can be retrieved using {@link TypeReference}
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
                                            Type param1, Type param2, Type param3, Type param4) {
        ActionBase action = params -> {
            callback.invoke(Utils.<T1>cast(param1, params[0]), Utils.<T2>cast(param2, params[1]), Utils.<T3>cast(param3, params[2]),
                Utils.<T4>cast(param4, params[3]));
            return Completable.complete();
        };
        return registerHandler(target, action, param1, param2, param3, param4);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     * Should be used for generic classes and Parameterized Collections, like List or Map.
     * A Type can be retrieved using {@link TypeReference}
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
                                                Type param1, Type param2, Type param3, Type param4, Type param5) {
        ActionBase action = params -> {
            callback.invoke(Utils.<T1>cast(param1, params[0]), Utils.<T2>cast(param2, params[1]), Utils.<T3>cast(param3, params[2]),
                Utils.<T4>cast(param4, params[3]), Utils.<T5>cast(param5, params[4]));
            return Completable.complete();
        };
        return registerHandler(target, action, param1, param2, param3, param4, param5);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     * Should be used for generic classes and Parameterized Collections, like List or Map.
     * A Type can be retrieved using {@link TypeReference}
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
                                                    Type param1, Type param2, Type param3, Type param4, Type param5, Type param6) {
        ActionBase action = params -> {
            callback.invoke(Utils.<T1>cast(param1, params[0]), Utils.<T2>cast(param2, params[1]), Utils.<T3>cast(param3, params[2]),
                Utils.<T4>cast(param4, params[3]), Utils.<T5>cast(param5, params[4]), Utils.<T6>cast(param6, params[5]));
            return Completable.complete();
        };
        return registerHandler(target, action, param1, param2, param3, param4, param5, param6);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     * Should be used for generic classes and Parameterized Collections, like List or Map.
     * A Type can be retrieved using {@link TypeReference}
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
                                                        Type param1, Type param2, Type param3, Type param4, Type param5, Type param6, Type param7) {
        ActionBase action = params -> {
            callback.invoke(Utils.<T1>cast(param1, params[0]), Utils.<T2>cast(param2, params[1]), Utils.<T3>cast(param3, params[2]),
                Utils.<T4>cast(param4, params[3]), Utils.<T5>cast(param5, params[4]), Utils.<T6>cast(param6, params[5]), Utils.<T7>cast(param7, params[6]));
            return Completable.complete();
        };
        return registerHandler(target, action, param1, param2, param3, param4, param5, param6, param7);
    }

    /**
     * Registers a handler that will be invoked when the hub method with the specified method name is invoked.
     * Should be used for generic classes and Parameterized Collections, like List or Map.
     * A Type can be retrieved using {@link TypeReference}
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
                                                            Type param1, Type param2, Type param3, Type param4, Type param5, Type param6, Type param7,
                                                            Type param8) {
        ActionBase action = params -> {
            callback.invoke(Utils.<T1>cast(param1, params[0]), Utils.<T2>cast(param2, params[1]), Utils.<T3>cast(param3, params[2]),
                Utils.<T4>cast(param4, params[3]), Utils.<T5>cast(param5, params[4]), Utils.<T6>cast(param6, params[5]), Utils.<T7>cast(param7, params[6]),
                Utils.<T8>cast(param8, params[7]));
            return Completable.complete();
        };
        return registerHandler(target, action, param1, param2, param3, param4, param5, param6, param7, param8);
    }

    public <TResult> Subscription onWithResult(String target, FunctionSingle<TResult> callback) {
        FunctionBase action = args -> callback.invoke().cast(Object.class);
        return registerHandler(target, action);
    }

    public <T1, TResult> Subscription onWithResult(String target, Function1Single<T1, TResult> callback, Class<T1> param1) {
        FunctionBase action = params -> callback.invoke(Utils.<T1>cast(param1, params[0])).cast(Object.class);
        return registerHandler(target, action, param1);
    }

    public <T1, T2, TResult> Subscription onWithResult(String target, Function2Single<T1, T2, TResult> callback,
                                             Class<T1> param1, Class<T2> param2) {
        FunctionBase action = params -> callback.invoke(Utils.<T1>cast(param1, params[0]),
            Utils.<T2>cast(param2, params[1])).cast(Object.class);
        return registerHandler(target, action, param1, param2);
    }

    public <T1, T2, T3, TResult> Subscription onWithResult(String target, Function3Single<T1, T2, T3, TResult> callback,
                                                 Class<T1> param1, Class<T2> param2, Class<T3> param3) {
        FunctionBase action = params -> callback.invoke(Utils.<T1>cast(param1, params[0]),
            Utils.<T2>cast(param2, params[1]), Utils.<T3>cast(param3, params[2])).cast(Object.class);
        return registerHandler(target, action, param1, param2, param3);
    }

    public <T1, T2, T3, T4, TResult> Subscription onWithResult(String target, Function4Single<T1, T2, T3, T4, TResult> callback,
                                                     Class<T1> param1, Class<T2> param2, Class<T3> param3, Class<T4> param4) {
        FunctionBase action = params -> callback.invoke(Utils.<T1>cast(param1, params[0]), Utils.<T2>cast(param2, params[1]),
            Utils.<T3>cast(param3, params[2]), Utils.<T4>cast(param4, params[3])).cast(Object.class);
        return registerHandler(target, action, param1, param2, param3, param4);
    }

    public <T1, T2, T3, T4, T5, TResult> Subscription onWithResult(String target, Function5Single<T1, T2, T3, T4, T5, TResult> callback,
                                                         Class<T1> param1, Class<T2> param2, Class<T3> param3, Class<T4> param4, Class<T5> param5) {
        FunctionBase action = params -> callback.invoke(Utils.<T1>cast(param1, params[0]), Utils.<T2>cast(param2, params[1]),
            Utils.<T3>cast(param3, params[2]), Utils.<T4>cast(param4, params[3]), Utils.<T5>cast(param5, params[4])).cast(Object.class);
        return registerHandler(target, action, param1, param2, param3, param4, param5);
    }

    public <T1, T2, T3, T4, T5, T6, TResult> Subscription onWithResult(String target, Function6Single<T1, T2, T3, T4, T5, T6, TResult> callback,
                                                             Class<T1> param1, Class<T2> param2, Class<T3> param3,
                                                             Class<T4> param4, Class<T5> param5, Class<T6> param6) {
        FunctionBase action = params -> callback.invoke(Utils.<T1>cast(param1, params[0]), Utils.<T2>cast(param2, params[1]), Utils.<T3>cast(param3, params[2]),
            Utils.<T4>cast(param4, params[3]), Utils.<T5>cast(param5, params[4]), Utils.<T6>cast(param6, params[5])).cast(Object.class);
        return registerHandler(target, action, param1, param2, param3, param4, param5, param6);
    }

    public <T1, T2, T3, T4, T5, T6, T7, TResult> Subscription onWithResult(String target, Function7Single<T1, T2, T3, T4, T5, T6, T7, TResult> callback,
                                                                 Class<T1> param1, Class<T2> param2, Class<T3> param3, Class<T4> param4,
                                                                 Class<T5> param5, Class<T6> param6, Class<T7> param7) {
        FunctionBase action = params -> callback.invoke(Utils.<T1>cast(param1, params[0]), Utils.<T2>cast(param2, params[1]), Utils.<T3>cast(param3, params[2]),
            Utils.<T4>cast(param4, params[3]), Utils.<T5>cast(param5, params[4]), Utils.<T6>cast(param6, params[5]), Utils.<T7>cast(param7, params[6])).cast(Object.class);
        return registerHandler(target, action, param1, param2, param3, param4, param5, param6, param7);
    }

    public <T1, T2, T3, T4, T5, T6, T7, T8, TResult> Subscription onWithResult(String target, Function8Single<T1, T2, T3, T4, T5, T6, T7, T8, TResult> callback,
                                                                     Class<T1> param1, Class<T2> param2, Class<T3> param3, Class<T4> param4, Class<T5> param5,
                                                                     Class<T6> param6, Class<T7> param7, Class<T8> param8) {
        FunctionBase action = params -> callback.invoke(Utils.<T1>cast(param1, params[0]), Utils.<T2>cast(param2, params[1]),
            Utils.<T3>cast(param3, params[2]), Utils.<T4>cast(param4, params[3]), Utils.<T5>cast(param5, params[4]),
            Utils.<T6>cast(param6, params[5]), Utils.<T7>cast(param7, params[6]), Utils.<T8>cast(param8, params[7])).cast(Object.class);
        return registerHandler(target, action, param1, param2, param3, param4, param5, param6, param7, param8);
    }

    private Subscription registerHandler(String target, Object action, Type... types) {
        InvocationHandler handler = handlers.put(target, action, types);
        logger.debug("Registering handler for client method: '{}'.", target);
        return new Subscription(handlers, handler, target);
    }

    private final class ConnectionState implements InvocationBinder {
        private final HubConnection connection;
        private final AtomicInteger nextId = new AtomicInteger(0);
        private final HashMap<String, InvocationRequest> pendingInvocations = new HashMap<>();
        private final AtomicLong nextServerTimeout = new AtomicLong();
        private final AtomicLong nextPingActivation = new AtomicLong();
        private Timer pingTimer = null;
        private Boolean handshakeReceived = false;
        private ScheduledExecutorService handshakeTimeout = null;
        private BehaviorSubject<InvocationMessage> messages = BehaviorSubject.create();
        private ExecutorService resultInvocationPool = null;

        public final Lock lock = new ReentrantLock();
        public final CompletableSubject handshakeResponseSubject = CompletableSubject.create();
        public Transport transport;
        public String connectionId;
        public String stopError;
        public Completable startTask;

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
                Collection<String> keys = pendingInvocations.keySet();
                for (String key : keys) {
                    if (ex == null) {
                        pendingInvocations.get(key).cancel();
                    } else {
                        pendingInvocations.get(key).fail(ex);
                    }
                }

                pendingInvocations.clear();
            } finally {
                lock.unlock();
            }
        }

        public void addInvocation(InvocationRequest irq) {
            lock.lock();
            try {
                if (pendingInvocations.containsKey(irq.getInvocationId())) {
                    throw new IllegalStateException("Invocation Id is already used");
                } else {
                    pendingInvocations.put(irq.getInvocationId(), irq);
                }
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

        public void resetServerTimeout() {
            this.nextServerTimeout.set(System.currentTimeMillis() + serverTimeout);
        }

        public void resetKeepAlive() {
            this.nextPingActivation.set(System.currentTimeMillis() + keepAliveInterval);
        }

        public void activatePingTimer() {
            this.pingTimer = new Timer();
            this.pingTimer.schedule(new TimerTask() {
                @Override
                public void run() {
                    try {
                        if (System.currentTimeMillis() > nextServerTimeout.get()) {
                            stop("Server timeout elapsed without receiving a message from the server.");
                            return;
                        }

                        if (System.currentTimeMillis() > nextPingActivation.get()) {
                            sendHubMessageWithLock(PingMessage.getInstance());
                        }
                    } catch (Exception e) {
                        logger.warn("Error sending ping: {}.", e.getMessage());
                        // The connection is probably in a bad or closed state now, cleanup the timer so
                        // it stops triggering
                        pingTimer.cancel();
                    }
                }
            }, new Date(0), tickRate);
        }

        public void handleHandshake(ByteBuffer payload) {
            if (!handshakeReceived) {
                List<Byte> handshakeByteList = new ArrayList<Byte>();
                byte curr = payload.get();
                // Add the handshake to handshakeBytes, but not the record separator
                while (curr != RECORD_SEPARATOR) {
                    handshakeByteList.add(curr);
                    curr = payload.get();
                }
                int handshakeLength = handshakeByteList.size() + 1;
                byte[] handshakeBytes = new byte[handshakeLength - 1];
                for (int i = 0; i < handshakeLength - 1; i++) {
                    handshakeBytes[i] = handshakeByteList.get(i);
                }
                // The handshake will always be a UTF8 Json string
                String handshakeResponseString = new String(handshakeBytes, StandardCharsets.UTF_8);
                HandshakeResponseMessage handshakeResponse;
                try {
                    handshakeResponse = HandshakeProtocol.parseHandshakeResponse(handshakeResponseString);
                } catch (RuntimeException ex) {
                    RuntimeException exception = new RuntimeException("An invalid handshake response was received from the server.", ex);
                    errorHandshake(exception);
                    throw exception;
                }
                if (handshakeResponse.getHandshakeError() != null) {
                    String errorMessage = "Error in handshake " + handshakeResponse.getHandshakeError();
                    logger.error(errorMessage);
                    RuntimeException exception = new RuntimeException(errorMessage);
                    errorHandshake(exception);
                    throw exception;
                }
                handshakeReceived = true;
                handshakeResponseSubject.onComplete();
                startInvocationProcessing();
            }
        }

        public void timeoutHandshakeResponse(long timeout, TimeUnit unit) {
            handshakeTimeout = Executors.newSingleThreadScheduledExecutor();
            handshakeTimeout.schedule(() -> {
                errorHandshake(new TimeoutException("Timed out waiting for the server to respond to the handshake message."));
            }, timeout, unit);
        }

        public void close() {
            handshakeResponseSubject.onComplete();
            messages.onComplete();

            if (pingTimer != null) {
                pingTimer.cancel();
            }

            if (this.handshakeTimeout != null) {
                this.handshakeTimeout.shutdownNow();
            }

            if (this.resultInvocationPool != null) {
                this.resultInvocationPool.shutdownNow();
            }
        }

        public void dispatchInvocation(InvocationMessage message) {
            messages.onNext(message);
        }

        private void startInvocationProcessing() {
            this.resultInvocationPool = Executors.newCachedThreadPool();
            this.messages.observeOn(Schedulers.io()).subscribe(invocationMessage -> {
                // if client result expected, unblock the invocation processing thread
                if (invocationMessage.getInvocationId() != null) {
                    this.resultInvocationPool.submit(() -> handleInvocation(invocationMessage));
                } else {
                    handleInvocation(invocationMessage);
                }
            }, (e) -> {
                stop(e.getMessage());
            }, () -> {
            });
        }

        private void handleInvocation(InvocationMessage invocationMessage)
        {
            List<InvocationHandler> handlers = this.connection.handlers.get(invocationMessage.getTarget());
            boolean expectsResult = invocationMessage.getInvocationId() != null;
            if (handlers == null) {
                if (expectsResult) {
                    logger.warn("Failed to find a value returning handler for '{}' method. Sending error to server.", invocationMessage.getTarget());
                    sendHubMessageWithLock(new CompletionMessage(null, invocationMessage.getInvocationId(),
                        null, "Client did not provide a result."));
                } else {
                    logger.warn("Failed to find handler for '{}' method.", invocationMessage.getTarget());
                }
                return;
            }
            Object result = null;
            Exception resultException = null;
            Boolean hasResult = false;
            for (InvocationHandler handler : handlers) {
                try {
                    Object action = handler.getAction();
                    if (handler.getHasResult()) {
                        FunctionBase function = (FunctionBase)action;
                        result = function.invoke(invocationMessage.getArguments()).blockingGet();
                        hasResult = true;
                    } else {
                        ((ActionBase)action).invoke(invocationMessage.getArguments()).blockingAwait();
                    }
                } catch (Exception e) {
                    logger.error("Invoking client side method '{}' failed:", invocationMessage.getTarget(), e);
                    if (handler.getHasResult()) {
                        resultException = e;
                    }
                }
            }

            if (expectsResult) {
                if (resultException != null) {
                    sendHubMessageWithLock(new CompletionMessage(null, invocationMessage.getInvocationId(),
                        null, resultException.getMessage()));
                } else if (hasResult) {
                    sendHubMessageWithLock(new CompletionMessage(null, invocationMessage.getInvocationId(),
                        result, null));
                } else {
                    logger.warn("Failed to find a value returning handler for '{}' method. Sending error to server.", invocationMessage.getTarget());
                    sendHubMessageWithLock(new CompletionMessage(null, invocationMessage.getInvocationId(),
                        null, "Client did not provide a result."));
                }
            } else if (hasResult) {
                logger.warn("Result given for '{}' method but server is not expecting a result.", invocationMessage.getTarget());
            }
        }

        @Override
        public Type getReturnType(String invocationId) {
            InvocationRequest irq = getInvocation(invocationId);
            if (irq == null) {
                return null;
            }

            return irq.getReturnType();
        }

        @Override
        public List<Type> getParameterTypes(String methodName) {
            List<InvocationHandler> handlers = connection.handlers.get(methodName);
            if (handlers == null) {
                logger.warn("Failed to find handler for '{}' method.", methodName);
                return emptyArray;
            }

            if (handlers.isEmpty()) {
                throw new RuntimeException(String.format("There are no callbacks registered for the method '%s'.", methodName));
            }

            return handlers.get(0).getTypes();
        }

        private void errorHandshake(Exception error) {
            lock.lock();
            try {
                // If onError is called on a completed subject the global error handler is called
                if (!(handshakeResponseSubject.hasComplete() || handshakeResponseSubject.hasThrowable())) {
                    handshakeResponseSubject.onError(error);
                }
            } finally {
                lock.unlock();
            }
        }
    }

    // We don't have reconnect yet, but this helps align the Java client with the .NET client
    // and hopefully make it easier to implement reconnect in the future
    private final class ReconnectingConnectionState {
        private final Logger logger;
        private final Lock lock = new ReentrantLock();
        private ConnectionState state;
        private HubConnectionState hubConnectionState = HubConnectionState.DISCONNECTED;

        public ReconnectingConnectionState(Logger logger) {
            this.logger = logger;
        }

        public void setConnectionState(ConnectionState state) {
            this.lock.lock();
            try {
                this.state = state;
            } finally {
                this.lock.unlock();
            }
        }

        public ConnectionState getConnectionStateUnsynchronized(Boolean allowNull) {
            if (allowNull != true && this.state == null) {
                throw new RuntimeException("Connection is not active.");
            }
            return this.state;
        }

        public ConnectionState getConnectionState() {
            this.lock.lock();
            try {
                if (this.state == null) {
                    throw new RuntimeException("Connection is not active.");
                }
                return this.state;
            } finally {
                this.lock.unlock();
            }
        }

        public HubConnectionState getHubConnectionState() {
            return this.hubConnectionState;
        }

        public void changeState(HubConnectionState from, HubConnectionState to) {
            this.lock.lock();
            try {
                logger.debug("The HubConnection is attempting to transition from the {} state to the {} state.", from, to);
                if (this.hubConnectionState != from) {
                    logger.debug("The HubConnection failed to transition from the {} state to the {} state because it was actually in the {} state.",
                        from, to, this.hubConnectionState);
                    throw new RuntimeException(String.format("The HubConnection failed to transition from the '%s' state to the '%s' state because it was actually in the '%s' state.",
                        from, to, this.hubConnectionState));
                }

                this.hubConnectionState = to;
            } finally {
                this.lock.unlock();
            }
        }

        public void changeState(HubConnectionState to) {
            this.lock.lock();
            try {
                logger.debug("The HubConnection is transitioning from the {} state to the {} state.", this.hubConnectionState, to);
                this.hubConnectionState = to;
            } finally {
                this.lock.unlock();
            }
        }

        public void lock() {
            this.lock.lock();
        }

        public void unlock() {
            this.lock.unlock();
        }
    }

    @Override
    public void close() {
        try {
            stop().blockingAwait();
        } finally {
            // Don't close HttpClient if it's passed in by the user
            if (this.httpClient != null && this.httpClient instanceof DefaultHttpClient) {
                this.httpClient.close();
            }
        }
    }
}
