// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace BasicTestApp.InteropTest;

public class ByteArrayInterop
{
    [JSInvokable]
    public static byte[] RoundTripByteArray(byte[] byteArray)
    {
        return byteArray;
    }

    [JSInvokable]
    public static Task<byte[]> RoundTripByteArrayAsync(byte[] byteArray)
    {
        return Task.FromResult(byteArray);
    }

    [JSInvokable]
    public static ByteArrayWrapper RoundTripByteArrayWrapperObject(ByteArrayWrapper byteArrayWrapper)
    {
        return byteArrayWrapper;
    }

    [JSInvokable]
    public static Task<ByteArrayWrapper> RoundTripByteArrayWrapperObjectAsync(ByteArrayWrapper byteArrayWrapper)
    {
        return Task.FromResult(byteArrayWrapper);
    }

    public class ByteArrayWrapper
    {
        public string StrVal { get; set; }
        public byte[] ByteArrayVal { get; set; }
        public int IntVal { get; set; }

        public override string ToString()
        {
            return $"StrVal: {StrVal}, IntVal: {IntVal}, ByteArrayVal: {string.Join(',', ByteArrayVal)}";
        }
    }
}
