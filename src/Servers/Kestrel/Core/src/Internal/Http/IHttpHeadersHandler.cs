// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

/// <summary>
/// This API supports framework infrastructure and is not intended to be used
/// directly from application code.
/// </summary>
public interface IHttpHeadersHandler
{
    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    void OnStaticIndexedHeader(int index);

    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    void OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value);

    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    void OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value);

    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    void OnHeadersComplete(bool endStream);
}
