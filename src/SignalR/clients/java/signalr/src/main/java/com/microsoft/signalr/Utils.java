// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

class Utils {
    public static String appendQueryString(String original, String queryStringValue) {
        if (original.contains("?")) {
            return original + "&" + queryStringValue;
        } else {
            return  original + "?" + queryStringValue;
        }
    }
}