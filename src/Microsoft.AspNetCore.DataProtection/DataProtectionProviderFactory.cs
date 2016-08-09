// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.DataProtection
{
    internal static class DataProtectionProviderFactory
    {
        public static ILoggerFactory GetDefaultLoggerFactory()
        {
            return NullLoggerFactory.Instance;
        }
    }
}
