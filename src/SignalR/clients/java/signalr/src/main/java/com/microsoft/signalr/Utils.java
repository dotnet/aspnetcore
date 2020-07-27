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
    
    public static Object toPrimitive(Class<?> c, Object value) {
        if( Boolean.class == c) return ((Boolean) value).booleanValue();
        if( Byte.class == c) return ((Byte) value).byteValue();
        if( Short.class == c) return ((Short) value).shortValue();
        if( Integer.class == c) return ((Integer) value).intValue();
        if( Long.class == c) return ((Long) value).longValue();
        if( Float.class == c) return ((Float) value).floatValue();
        if( Double.class == c) return ((Double) value).doubleValue();
        return value;
    }
}