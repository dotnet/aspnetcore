// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import java.io.IOException;
import java.net.CookieManager;
import java.net.CookiePolicy;
import java.nio.ByteBuffer;
import java.util.Collection;
import java.util.Map;
import java.util.concurrent.TimeUnit;

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
            OkHttpClient.Builder builder = new OkHttpClient.Builder()
                .cookieJar(new JavaNetCookieJar(new CookieManager(null, CookiePolicy.ACCEPT_ALL)));

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
            public void onResponse(Call call, Response response) {
                HttpResponse httpResponse;
                try (ResponseBody body = response.body()) {
                    httpResponse = new HttpResponse(response.code(), response.message(), ByteBuffer.wrap(body.bytes()));
                } catch (Throwable ex) {
                    // OkHttp marks the callback as signalled before invoking onResponse, so it never calls
                    // onFailure for anything thrown from here. Reading the body can still fail, for example when
                    // the connection drops mid-response, so terminate the Single ourselves. Otherwise callers
                    // such as negotiate would wait on it forever.
                    responseSubject.onError(ex);
                    return;
                }

                responseSubject.onSuccess(httpResponse);
            }
        });

        return responseSubject;
    }

    @Override
    public WebSocketWrapper createWebSocket(String url, Map<String, String> headers) {
        return new OkHttpWebSocketWrapper(url, headers, client);
    }
}
