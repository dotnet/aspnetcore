package com.microsoft.signalr;

import com.google.gson.Gson;
import com.google.gson.stream.JsonReader;
import com.google.gson.stream.JsonWriter;

import java.io.IOException;

/**
 * This internal class is a way to delegate serialisation of a specific sub property of a complex
 * model to another gson instance.
 * This is used to allow user customisation of the serialisation of their own types.
 */
class UserProvidedGsonTypeAdapter extends com.google.gson.TypeAdapter<Object[]> {

    private Gson gson;

    public UserProvidedGsonTypeAdapter(Gson gson) {
        if (null == gson) throw new IllegalArgumentException("gson may not be null");
        this.gson = gson;
    }

    @Override
    public void write(JsonWriter out, Object[] value) throws IOException {
        gson.getAdapter(Object[].class).write(out, value);
    }

    @Override
    public Object[] read(JsonReader in) throws IOException {
        return gson.getAdapter(Object[].class).read(in);
    }
}
