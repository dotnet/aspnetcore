// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import java.io.IOException;
import java.nio.ByteBuffer;
import java.util.ArrayList;
import java.util.Collection;
import java.util.List;
import java.util.Map;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.locks.Lock;
import java.util.concurrent.locks.ReentrantLock;

import io.reactivex.rxjava3.core.Single;
import io.reactivex.rxjava3.subjects.SingleSubject;
import okhttp3.*;
import okio.ByteString;

final class DefaultHttpClient extends HttpClient {
    private OkHttpClient client = null;

    public DefaultHttpClient(Action1<OkHttpClient.Builder> configureBuilder) {
        this(null, configureBuilder);
    }

    public DefaultHttpClient cloneWithTimeOut(int timeoutInMilliseconds) {
        OkHttpClient newClient = client.newBuilder().readTimeout(timeoutInMilliseconds, TimeUnit.MILLISECONDS)
                .build();
        return new DefaultHttpClient(newClient, null);
    }

    @Override
    public void close() {
        if (this.client != null) {
            this.client.dispatcher().executorService().shutdown();
        }
    }

    public DefaultHttpClient(OkHttpClient client, Action1<OkHttpClient.Builder> configureBuilder) {
        if (client != null) {
            this.client = client;
        } else {
            OkHttpClient.Builder builder = new OkHttpClient.Builder().cookieJar(new CookieJar() {
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
            });

            if (configureBuilder != null) {
                configureBuilder.invoke(builder);
            }

            this.client = builder.build();
        }
    }

    @Override
    public Single<HttpResponse> send(HttpRequest httpRequest) {
        return send(httpRequest, null);
    }

    @Override
    public Single<HttpResponse> send(HttpRequest httpRequest, ByteBuffer bodyContent) {
        Request.Builder requestBuilder = new Request.Builder().url(httpRequest.getUrl());

        switch (httpRequest.getMethod()) {
            case "GET":
                requestBuilder.get();
                break;
            case "POST":
                RequestBody body;
                if (bodyContent != null) {
                    body = RequestBody.Companion.create(ByteString.of(bodyContent), MediaType.parse("text/plain"));
                } else {
                    body = RequestBody.Companion.create(new byte[]{}, null);
                }

                requestBuilder.post(body);
                break;
            case "DELETE":
                requestBuilder.delete();
                break;
        }

        if (httpRequest.getHeaders() != null) {
            Collection<String> keys = httpRequest.getHeaders().keySet();
            for (String key : keys) {
                requestBuilder.addHeader(key, httpRequest.getHeaders().get(key));
            }
        }

        Request request = requestBuilder.build();

        SingleSubject<HttpResponse> responseSubject = SingleSubject.create();

        client.newCall(request).enqueue(new Callback() {
            @Override
            public void onFailure(Call call, IOException e) {
                Throwable cause = e.getCause();
                if (cause == null) {
                    cause = e;
                }
                responseSubject.onError(cause);
            }

            @Override
            public void onResponse(Call call, Response response) throws IOException {
                try (ResponseBody body = response.body()) {
                    HttpResponse httpResponse = new HttpResponse(response.code(), response.message(), ByteBuffer.wrap(body.bytes()));
                    responseSubject.onSuccess(httpResponse);
                }
            }
        });

        return responseSubject;
    }

    @Override
    public WebSocketWrapper createWebSocket(String url, Map<String, String> headers) {
        return new OkHttpWebSocketWrapper(url, headers, client);
    }
}
