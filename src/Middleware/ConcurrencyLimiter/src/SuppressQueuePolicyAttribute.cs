// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.ConcurrencyLimiter
{
    /// <summary>
    /// Specifies that the class or method that this attribute applied to does not limit concurrency request.
    /// </summary>
    public class SuppressQueuePolicyAttribute : ISuppressQueuePolicyMetadata
    {
    }
}
