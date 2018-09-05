// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.aspnet.signalr;

import com.google.gson.JsonArray;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;

import java.util.HashSet;
import java.util.Set;

public class NegotiateResponse {
    private String connectionId;
    private Set<String> availableTransports = new HashSet<>();
    private String redirectUrl;
    private String accessToken;
    private JsonParser jsonParser = new JsonParser();

    public NegotiateResponse(String negotiatePayload) {
        JsonObject negotiateResponse = jsonParser.parse(negotiatePayload).getAsJsonObject();
        if (negotiateResponse.has("url")) {
            this.redirectUrl = negotiateResponse.get("url").getAsString();
            if (negotiateResponse.has("accessToken")) {
                this.accessToken = negotiateResponse.get("accessToken").getAsString();
            }
            return;
        }
        this.connectionId = negotiateResponse.get("connectionId").getAsString();
        JsonArray transports = (JsonArray) negotiateResponse.get("availableTransports");
        for (int i = 0; i < transports.size(); i++) {
            availableTransports.add(transports.get(i).getAsJsonObject().get("transport").getAsString());
        }
    }

    public String getConnectionId() {
        return connectionId;
    }

    public Set<String> getAvailableTransports() {
        return availableTransports;
    }

    public String getRedirectUrl() {
        return redirectUrl;
    }

    public String getAccessToken() {
        return accessToken;
    }
}

