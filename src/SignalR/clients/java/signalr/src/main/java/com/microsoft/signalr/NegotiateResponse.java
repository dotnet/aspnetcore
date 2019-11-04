// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr;

import java.io.IOException;
import java.util.HashSet;
import java.util.Set;

import com.google.gson.stream.JsonReader;

class NegotiateResponse {
    private String connectionId;
    private String connectionToken;
    private Set<String> availableTransports = new HashSet<>();
    private String redirectUrl;
    private String accessToken;
    private String error;
    private String finalUrl;
    private int version;

    public NegotiateResponse(JsonReader reader) {
        try {
            reader.beginObject();

            do {
                String name = reader.nextName();
                switch (name) {
                    case "error":
                        this.error = reader.nextString();
                        break;
                    case "ProtocolVersion":
                        this.error = "Detected an ASP.NET SignalR Server. This client only supports connecting to an ASP.NET Core SignalR Server. See https://aka.ms/signalr-core-differences for details.";
                        return;
                    case "negotiateVersion":
                        this.version = reader.nextInt();
                        break;
                    case "connectionToken":
                        this.connectionToken = reader.nextString();
                        break;
                    case "url":
                        this.redirectUrl = reader.nextString();
                        break;
                    case "accessToken":
                        this.accessToken = reader.nextString();
                        break;
                    case "availableTransports":
                        reader.beginArray();
                        while (reader.hasNext()) {
                            reader.beginObject();
                            while (reader.hasNext()) {
                                String transport = null;
                                String property = reader.nextName();
                                switch (property) {
                                    case "transport":
                                        transport = reader.nextString();
                                        break;
                                    case "transferFormats":
                                        // transfer formats aren't supported currently
                                        reader.skipValue();
                                        break;
                                    default:
                                        // Skip unknown property, allows new clients to still work with old protocols
                                        reader.skipValue();
                                        break;
                                }
                                this.availableTransports.add(transport);
                            }
                            reader.endObject();
                        }
                        reader.endArray();
                        break;
                    case "connectionId":
                        this.connectionId = reader.nextString();
                        break;
                    default:
                        // Skip unknown property, allows new clients to still work with old protocols
                        reader.skipValue();
                        break;
                }
            } while (reader.hasNext());
            reader.endObject();
            reader.close();
        } catch (IOException ex) {
            throw new RuntimeException("Error reading NegotiateResponse", ex);
        }
    }

    public NegotiateResponse(String url) {
        this.finalUrl = url;
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

    public String getError() {
        return error;
    }

    public String getFinalUrl() {
        return finalUrl;
    }

    public int getVersion() {
        return version;
    }

    public String getConnectionToken() {
        return connectionToken;
    }

    public void setFinalUrl(String url) {
        this.finalUrl = url;
    }
}
