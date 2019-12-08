package com.microsoft.signalr;

import com.google.gson.GsonBuilder;

/**
 * Used to configure options for the creation of the JSON hub protocol
 */
public class JsonHubProtocolOptions {

    private GsonBuilder gsonBuilder;

    /**
     * Creates a default instance with no changes to the default configuration
     */
    public JsonHubProtocolOptions() {
    }

    /**
     * Creates an instance with the specified {@link GsonBuilder} which be used to deserialise
     * user payloads.
     */
    public JsonHubProtocolOptions(GsonBuilder gsonBuilder) {
        this.setGsonBuilder(gsonBuilder);
    }

    /**
     * Gets the {@link GsonBuilder} which be used to deserialise user payloads, if provided.
     */
    public GsonBuilder getGsonBuilder() {
        return gsonBuilder;
    }

    /**
     * Sets the {@link GsonBuilder} which be used to deserialise user payloads, or null if you
     * want to use the default {@link GsonBuilder}.
     */
    public void setGsonBuilder(GsonBuilder gsonBuilder) {
        this.gsonBuilder = gsonBuilder;
    }
}
