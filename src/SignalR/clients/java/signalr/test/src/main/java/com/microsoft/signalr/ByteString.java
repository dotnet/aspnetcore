// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import java.nio.ByteBuffer;

class ByteString{

    private byte[] src;

    private ByteString(byte[] src) {
        this.src = src;
    }

    public static ByteString of(byte[] src) {
        return new ByteString(src);
    }

    public static ByteString of(ByteBuffer src) {
        return new ByteString(src.array());
    }

    public byte[] array() {
        return src;
    }

    @Override
    public boolean equals(Object obj) {
        if (!(obj instanceof ByteString)) {
            return false;
        }
        byte[] otherSrc = ((ByteString) obj).array();
        if (otherSrc.length != src.length) {
            return false;
        }
        for (int i = 0; i < src.length; i++) {
            if (src[i] != otherSrc[i]) {
                return false;
            }
        }
        return true;
    }

    @Override
    public String toString() {
        String str = "";
        for (byte b: src) {
            str += String.format("%02X", b);
        }
        return str;
    }
}
