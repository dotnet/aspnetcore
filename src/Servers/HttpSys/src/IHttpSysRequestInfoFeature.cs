// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Note: This will also be useful for IIS in-proc.
// Plan: Have Microsoft.AspNetCore.Server.IIS take a dependency on Microsoft.AspNetCore.Server.HttpSys and implement this interface.
namespace Microsoft.AspNetCore.Server.HttpSys;

/// <summary>
/// This exposes the Http.Sys HTTP_REQUEST_INFO extensibility point as opaque data for the caller to interperate.
/// <see href="https://learn.microsoft.com/windows/win32/api/http/ns-http-http_request_v2"/>,
/// <see href="https://learn.microsoft.com/windows/win32/api/http/ns-http-http_request_info"/>
/// </summary>
public interface IHttpSysRequestInfoFeature
{
    /// <summary>
    /// A collection of the HTTP_REQUEST_INFO for the current request. The integer represents the identifying
    /// HTTP_REQUEST_INFO_TYPE enum value. The Memory is opaque bytes that need to be interperted in the format
    /// specified by the enum value.
    /// </summary>
    public IReadOnlyDictionary<int, ReadOnlyMemory<byte>> RequestInfo { get; }
}
