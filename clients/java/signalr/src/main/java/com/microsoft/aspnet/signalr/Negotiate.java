// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.aspnet.signalr;

import java.io.IOException;

import okhttp3.OkHttpClient;
import okhttp3.Request;
import okhttp3.RequestBody;
import okhttp3.Response;

class Negotiate {

    public static NegotiateResponse processNegotiate(String url, OkHttpClient httpClient) throws IOException {
        return processNegotiate(url, httpClient, null);
    }

    public static NegotiateResponse processNegotiate(String url, OkHttpClient httpClient,String accessTokenHeader) throws IOException {
        url = resolveNegotiateUrl(url);
        RequestBody body = RequestBody.create(null, new byte[]{});
        Request.Builder requestBuilder = new Request.Builder()
                .url(url)
                .post(body);

        if (accessTokenHeader != null) {
            requestBuilder.addHeader("Authorization", "Bearer " + accessTokenHeader);
        }

        Request request = requestBuilder.build();

        Response response = httpClient.newCall(request).execute();
        String result = response.body().string();
        return new NegotiateResponse(result);
    }

    public static String resolveNegotiateUrl(String url) {
        String negotiateUrl = "";

        // Check if we have a query string. If we do then we ignore it for now.
        int queryStringIndex = url.indexOf('?');
        if (queryStringIndex > 0) {
            negotiateUrl = url.substring(0, url.indexOf('?'));
        } else {
            negotiateUrl = url;
        }

        //Check if the url ends in a /
        if (negotiateUrl.charAt(negotiateUrl.length() - 1) != '/') {
            negotiateUrl += "/";
        }

        negotiateUrl += "negotiate";

        // Add the query string back if it existed.
        if (queryStringIndex > 0) {
            negotiateUrl += url.substring(url.indexOf('?'));
        }

        return negotiateUrl;
    }
}
