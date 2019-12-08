package com.microsoft.signalr;

import com.google.gson.Gson;
import com.google.gson.TypeAdapter;
import com.google.gson.TypeAdapterFactory;
import com.google.gson.reflect.TypeToken;

/**
 * This interface is just used to register the type of the user provided Gson instance,
 * so that we can retrieve it from another Gson instance when required
 */
class UserProvidedGsonType implements TypeAdapterFactory {

    public static Gson gson;

    @Override
    public <T> TypeAdapter<T> create(Gson gson, TypeToken<T> type) {
        return (TypeAdapter<T>) new UserProvidedGsonTypeAdapter(gson);
    }
}
