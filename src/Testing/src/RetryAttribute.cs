// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;

namespace Microsoft.AspNetCore.InternalTesting;

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
