// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import com.google.gson.Gson;

public class DefaultJsonProtocolHandShakeMessage {
    private String protocol = "json";
    private int version = 1;
    private static final String RECORD_SEPARATOR = "\u001e";

    public String createHandshakeMessage() {
        Gson gson = new Gson();
        return gson.toJson(this) + RECORD_SEPARATOR;
    }
}
