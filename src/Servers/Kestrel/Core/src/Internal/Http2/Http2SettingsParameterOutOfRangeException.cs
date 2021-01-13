// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    internal sealed class Http2SettingsParameterOutOfRangeException : Exception
    {
        public Http2SettingsParameterOutOfRangeException(Http2SettingsParameter parameter, long lowerBound, long upperBound)
            : base($"HTTP/2 SETTINGS parameter {parameter} must be set to a value between {lowerBound} and {upperBound}")
        {
            Parameter = parameter;
        }

        public Http2SettingsParameter Parameter { get; }
    }
}
