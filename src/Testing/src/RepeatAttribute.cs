// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;

namespace Microsoft.AspNetCore.Testing
{
    /// <summary>
    /// Runs a test multiple times to stress flaky tests that are believed to be fixed.
    /// This can be used on an assembly, class, or method name. Requires using the AspNetCore test framework.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = false)]
    public class RepeatAttribute : Attribute
    {
        public RepeatAttribute(int runCount = 10)
        {
            RunCount = runCount;
        }

        /// <summary>
        /// The number of times to run a test.
        /// </summary>
        public int RunCount { get; }
    }
}
