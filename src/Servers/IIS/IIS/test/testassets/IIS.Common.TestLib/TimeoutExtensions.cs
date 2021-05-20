// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Server.IntegrationTesting
{

    public static class TimeoutExtensions
    {
        public static TimeSpan DefaultTimeoutValue = TimeSpan.FromSeconds(300);
    }
}
