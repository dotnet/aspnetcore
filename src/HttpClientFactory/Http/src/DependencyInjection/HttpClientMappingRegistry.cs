// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    // Internal tracking for HTTP Client configuration. This is used to prevent some common mistakes
    // that are easy to make with HTTP Client registration.
    //
    // See: https://github.com/dotnet/extensions/issues/519
    // See: https://github.com/dotnet/extensions/issues/960
    internal class HttpClientMappingRegistry
    {
        public Dictionary<string, Type> NamedClientRegistrations { get; } = new Dictionary<string, Type>();
    }
}
