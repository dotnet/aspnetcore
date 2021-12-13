// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http
{
    internal interface IHttpStreamHeadersHandler
    {
        void OnStaticIndexedHeader(int index);
        void OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value);
        void OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value);
        void OnHeadersComplete(bool endStream);
        void OnDynamicIndexedHeader(int? index, ReadOnlySpan<byte> name, ReadOnlySpan<byte> value);
    }
}
