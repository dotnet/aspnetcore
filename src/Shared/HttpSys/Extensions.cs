// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.HttpSys.Internal
{
    internal static class Extensions
    {
        public static string GetHttpProtocolVersion(this Version version) => version switch
        {
            { Major: 2, Minor: 0 } => "HTTP/2",
            { Major: 1, Minor: 1 } => "HTTP/1.1",
            { Major: 1, Minor: 0 } => "HTTP/1.0",
            _ => "HTTP/" + version.ToString(2)
        };
    }
}
