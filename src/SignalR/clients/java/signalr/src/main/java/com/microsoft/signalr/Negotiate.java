// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

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

        if (!url.contains("negotiateVersion")) {
            negotiateUrl = Utils.appendQueryString(negotiateUrl, "negotiateVersion=" + negotiateVersion);
        }

        return negotiateUrl;
    }
}
