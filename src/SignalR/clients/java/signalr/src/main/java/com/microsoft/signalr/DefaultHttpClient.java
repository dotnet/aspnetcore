// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import java.io.IOException;
import java.util.ArrayList;
import java.util.Collection;
import java.util.List;
import java.util.Map;
import java.util.concurrent.locks.Lock;
import java.util.concurrent.locks.ReentrantLock;

import io.reactivex.Single;
import io.reactivex.subjects.SingleSubject;
import okhttp3.Call;
import okhttp3.Callback;
import okhttp3.Cookie;
import okhttp3.CookieJar;
import okhttp3.HttpUrl;
import okhttp3.OkHttpClient;
import okhttp3.Request;
import okhttp3.RequestBody;
import okhttp3.Response;
import okhttp3.ResponseBody;

final class DefaultHttpClient extends HttpClient {
    private final OkHttpClient client;

    public DefaultHttpClient() {
        this.client = new OkHttpClient.Builder().cookieJar(new CookieJar() {
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
        }).build();
    }

    @Override
    public Single<HttpResponse> send(HttpRequest httpRequest) {
        Request.Builder requestBuilder = new Request.Builder().url(httpRequest.getUrl());

        switch (httpRequest.getMethod()) {
            case "GET":
                requestBuilder.get();
                break;
            case "POST":
                RequestBody body = RequestBody.create(null, new byte[]{});
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
                    HttpResponse httpResponse = new HttpResponse(response.code(), response.message(), body.string());
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
