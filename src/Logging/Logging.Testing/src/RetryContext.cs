// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging.Testing
{
    public class RetryContext
    {
        internal int Limit { get; set; }

        internal object TestClassInstance { get; set; }

        internal string Reason { get; set; }

        internal int CurrentIteration { get; set; }
    }
}