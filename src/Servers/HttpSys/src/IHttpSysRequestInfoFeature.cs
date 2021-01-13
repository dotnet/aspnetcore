// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

// Note: This will also be useful for IIS in-proc.
// Plan: Have Microsoft.AspNetCore.Server.IIS take a dependency on Microsoft.AspNetCore.Server.HttpSys and implement this interface.
namespace Microsoft.AspNetCore.Server.HttpSys
{
    /// <summary>
    /// This exposes the Http.Sys HTTP_REQUEST_INFO extensibility point as opaque data for the caller to interperate.
    /// https://docs.microsoft.com/en-us/windows/win32/api/http/ns-http-http_request_v2
    /// https://docs.microsoft.com/en-us/windows/win32/api/http/ns-http-http_request_info
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
}
