// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Don't ever change this unless we are explicitly trying to remove IHttpHeadersHandler as public API.
#if KESTREL
using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
#else
namespace System.Net.Http
#endif
{
#if KESTREL
    public
#else
    internal
#endif
    interface IHttpHeadersHandler
    {
        void OnStaticIndexedHeader(int index);
        void OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value);
        void OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value);
        void OnHeadersComplete(bool endStream);
    }
}
