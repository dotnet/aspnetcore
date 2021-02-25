package com.microsoft.signalr.messagepack;

import java.io.IOException;
import java.lang.reflect.Array;
import java.lang.reflect.GenericArrayType;
import java.lang.reflect.ParameterizedType;
import java.lang.reflect.Type;
import java.lang.reflect.TypeVariable;
import java.lang.reflect.WildcardType;
import java.nio.ByteBuffer;
import java.util.ArrayList;

class Utils {
    public static int readLengthHeader(ByteBuffer buffer) throws IOException {
        // The payload starts with a length prefix encoded as a VarInt. VarInts use the most significant bit
        // as a marker whether the byte is the last byte of the VarInt or if it spans to the next byte. Bytes
        // appear in the reverse order - i.e. the first byte contains the least significant bits of the value
        // Examples:
        // VarInt: 0x35 - %00110101 - the most significant bit is 0 so the value is %x0110101 i.e. 0x35 (53)
        // VarInt: 0x80 0x25 - %10000000 %00101001 - the most significant bit of the first byte is 1 so the
        // remaining bits (%x0000000) are the lowest bits of the value. The most significant bit of the second
        // byte is 0 meaning this is last byte of the VarInt. The actual value bits (%x0101001) need to be
        // prepended to the bits we already read so the values is %01010010000000 i.e. 0x1480 (5248)
        // We support payloads up to 2GB so the biggest number we support is 7fffffff which when encoded as
        // VarInt is 0xFF 0xFF 0xFF 0xFF 0x07 - hence the maximum length prefix is 5 bytes.
        
        int length = 0;
        int numBytes = 0;
        int maxLength = 5;
        byte curr;
        
        do {
            // If we run out of bytes before we finish reading the length header, the message is malformed
            if (buffer.hasRemaining()) {
                curr = buffer.get();
            } else {
                throw new RuntimeException("The length header was incomplete");
            }
            length = length | (curr & (byte) 0x7f) << (numBytes * 7);
            numBytes++;
        } while (numBytes < maxLength && (curr & (byte) 0x80) != 0);
        
        // Max header length is 5, and the maximum value of the 5th byte is 0x07
        if ((curr & (byte) 0x80) != 0 || (numBytes == maxLength && curr > (byte) 0x07)) {
            throw new RuntimeException("Messages over 2GB in size are not supported");
        }
        
        return length;
    }
    
    public static ArrayList<Byte> getLengthHeader(int length) {
        // This code writes length prefix of the message as a VarInt. Read the comment in
        // the readLengthHeader for details.
        
        ArrayList<Byte> header = new ArrayList<Byte>();
        do {
            byte curr = (byte) (length & 0x7f);
            length >>= 7;
            if (length > 0) {
                curr |= 0x80;
            }
            header.add(curr);
        } while (length > 0);
        
        return header;
    }
    
    public static Object toPrimitive(Class<?> c, Object value) {
        if (boolean.class == c) return ((Boolean) value).booleanValue();
        if (byte.class == c) return ((Byte) value).byteValue();
        if (short.class == c) return ((Short) value).shortValue();
        if (int.class == c) return ((Integer) value).intValue();
        if (long.class == c) return ((Long) value).longValue();
        if (float.class == c) return ((Float) value).floatValue();
        if (double.class == c) return ((Double) value).doubleValue();
        if (char.class == c) return ((Character) value).charValue();
        return value;
    }
    
    public static Class<?> typeToClass(Type type) {
        if (type == null) {
            return null;
        }
        if (type instanceof Class) {
            return (Class<?>) type;
        } else if (type instanceof GenericArrayType) {
            // Instantiate an array of the same type as this type, then return its class
            return Array.newInstance(typeToClass(((GenericArrayType)type).getGenericComponentType()), 0).getClass();
        } else if (type instanceof ParameterizedType) {
            return typeToClass(((ParameterizedType) type).getRawType());
        } else if (type instanceof TypeVariable) {
            Type[] bounds = ((TypeVariable<?>) type).getBounds();
            return bounds.length == 0 ? Object.class : typeToClass(bounds[0]);
        } else if (type instanceof WildcardType) {
            Type[] bounds = ((WildcardType) type).getUpperBounds();
            return bounds.length == 0 ? Object.class : typeToClass(bounds[0]);
        } else { 
            throw new UnsupportedOperationException("Cannot handle type class: " + type.getClass());
        }
    }
}
