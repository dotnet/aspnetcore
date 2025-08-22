// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.HttpSys.Internal;

internal static class Constants
{
    internal const string HttpScheme = "http";
    internal const string HttpsScheme = "https";
    internal const string Chunked = "chunked";
    internal const string Close = "close";
    internal const string KeepAlive = "keep-alive";
    internal const string Zero = "0";
    internal const string SchemeDelimiter = "://";
    internal const string DefaultServerAddress = "http://localhost:5000";

    internal static readonly Version V1_0 = new Version(1, 0);
    internal static readonly Version V1_1 = new Version(1, 1);
    internal static readonly Version V2 = new Version(2, 0);
}
