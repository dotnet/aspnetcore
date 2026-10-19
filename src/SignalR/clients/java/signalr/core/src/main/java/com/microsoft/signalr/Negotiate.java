// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import okhttp3.HttpUrl;

class Negotiate {
    public static String resolveNegotiateUrl(String url, int negotiateVersion) {
        String negotiateUrl = "";

        // Check if we have a query string. If we do then we ignore it for now.
        int queryStringIndex = url.indexOf('?');
        if (queryStringIndex > 0) {
            negotiateUrl = url.substring(0, queryStringIndex);
        } else {
            negotiateUrl = url;
        }

        // Check if the url ends in a /
        if (negotiateUrl.charAt(negotiateUrl.length() - 1) != '/') {
            negotiateUrl += "/";
        }

        negotiateUrl += "negotiate";

        // Add the query string back if it existed.
        if (queryStringIndex > 0) {
            negotiateUrl += url.substring(queryStringIndex);
        }

        HttpUrl parsedUrl = HttpUrl.parse(url);
        boolean hasNegotiateVersion = parsedUrl != null && parsedUrl.queryParameterNames().contains("negotiateVersion");
        if (!hasNegotiateVersion) {
            negotiateUrl = Utils.appendQueryString(negotiateUrl, "negotiateVersion=" + negotiateVersion);
        }

        return negotiateUrl;
    }
}
