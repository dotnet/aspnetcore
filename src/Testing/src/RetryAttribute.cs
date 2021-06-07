// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;

namespace Microsoft.AspNetCore.Testing
{
    /// <summary>
    /// Runs a test multiple times when it fails
    /// This can be used on an assembly, class, or method name. Requires using the AspNetCore test framework.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class RetryAttribute : Attribute
    {
        public RetryAttribute(int maxRetries = 3)
        {
            MaxRetries = maxRetries;
        }

        /// <summary>
        /// The maximum number of times to retry a failed test. Defaults to 3.
        /// </summary>
        public int MaxRetries { get; }
    }
}
