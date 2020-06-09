// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Hosting
{
    internal class WebHostUtilities
    {
        public static bool ParseBool(IConfiguration configuration, string key)
        {
            return string.Equals("true", configuration[key], StringComparison.OrdinalIgnoreCase)
                || string.Equals("1", configuration[key], StringComparison.OrdinalIgnoreCase);
        }
    }
}
