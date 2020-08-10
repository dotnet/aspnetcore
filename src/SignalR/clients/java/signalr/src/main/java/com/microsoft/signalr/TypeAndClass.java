package com.microsoft.signalr;

import java.lang.reflect.Type;

class TypeAndClass {
    
    private Type type;
    private Class<?> clazz;
    
    public TypeAndClass(Type type, Class<?> clazz) {
        this.type = type;
        this.clazz = clazz;
    }
    
    public Type getType() {
        return type;
    }
    
    public Class<?> getClazz() {
        return clazz;
    }
}