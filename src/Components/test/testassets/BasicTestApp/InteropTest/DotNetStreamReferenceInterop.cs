// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace BasicTestApp.InteropTest;

public class DotNetStreamReferenceInterop
{
    [JSInvokable]
    public static DotNetStreamReference GetDotNetStreamReference()
    {
        var data = new byte[100000];
        for (var i = 0; i < data.Length; i++)
        {
            data[i] = (byte)(i % 256);
        }

        var dataStream = new MemoryStream(data);
        var streamRef = new DotNetStreamReference(dataStream);
        return streamRef;
    }

    [JSInvokable]
    public static Task<DotNetStreamReference> GetDotNetStreamReferenceAsync()
    {
        return Task.FromResult(GetDotNetStreamReference());
    }

    [JSInvokable]
    public static DotNetStreamReferenceWrapper GetDotNetStreamWrapperReference()
    {
        var wrapper = new DotNetStreamReferenceWrapper()
        {
            StrVal = "somestr",
            DotNetStreamReferenceVal = GetDotNetStreamReference(),
            IntVal = 25,
        };

        return wrapper;
    }

    [JSInvokable]
    public static Task<DotNetStreamReferenceWrapper> GetDotNetStreamWrapperReferenceAsync()
    {
        return Task.FromResult(GetDotNetStreamWrapperReference());
    }

    public class DotNetStreamReferenceWrapper
    {
        public string StrVal { get; set; }

        public DotNetStreamReference DotNetStreamReferenceVal { get; set; }

        public int IntVal { get; set; }
    }
}
