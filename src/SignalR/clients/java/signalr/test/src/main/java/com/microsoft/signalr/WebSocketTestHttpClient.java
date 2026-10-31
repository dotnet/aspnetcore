package com.microsoft.signalr;

import java.nio.ByteBuffer;
import java.util.Map;

import io.reactivex.rxjava3.core.Completable;
import io.reactivex.rxjava3.core.Single;

class WebSocketTestHttpClient extends HttpClient {
    @Override
    public Single<HttpResponse> send(HttpRequest request) {
        return null;
    }

    @Override
    public Single<HttpResponse> send(HttpRequest request, ByteBuffer body) {
        return null;
    }

    @Override
    public WebSocketWrapper createWebSocket(String url, Map<String, String> headers) {
        return new TestWebSocketWrapper(url, headers);
    }

    @Override
    public HttpClient cloneWithTimeOut(int timeoutInMilliseconds) {
        return null;
    }

    @Override
    public void close() {
    }
}

class TestWebSocketWrapper extends WebSocketWrapper {
    private WebSocketOnClosedCallback onClose;

    public TestWebSocketWrapper(String url, Map<String, String> headers)
    {
    }

    @Override
    public Completable start() {
        return Completable.complete();
    }

    @Override
    public Completable stop() {
        if (onClose != null) {
            onClose.invoke(null, "");
        }
        return Completable.complete();
    }

    @Override
    public Completable send(ByteBuffer message) {
        return Completable.complete();
    }

    @Override
    public void setOnReceive(OnReceiveCallBack onReceive) {
    }

    @Override
    public void setOnClose(WebSocketOnClosedCallback onClose) {
        this.onClose = onClose;
    }
}