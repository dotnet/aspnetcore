// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

class Negotiate {
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
