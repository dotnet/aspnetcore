// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace BasicTestApp.InteropTest
{
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
}
