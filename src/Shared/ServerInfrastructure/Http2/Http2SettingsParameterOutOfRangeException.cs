// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;

internal sealed class Http2SettingsParameterOutOfRangeException : Exception
{
    public Http2SettingsParameterOutOfRangeException(Http2SettingsParameter parameter, long lowerBound, long upperBound)
        : base($"HTTP/2 SETTINGS parameter {parameter} must be set to a value between {lowerBound} and {upperBound}")
    {
        Parameter = parameter;
    }

    public Http2SettingsParameter Parameter { get; }
}
