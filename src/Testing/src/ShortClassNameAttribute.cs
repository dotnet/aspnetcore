// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.InternalTesting;

/// <summary>
/// Used to specify that <see cref="TestFileOutputContext.TestClassName"/> should used the
/// unqualified class name. This is needed when a fully-qualified class name exceeds
/// max path for logging.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = false)]
public class ShortClassNameAttribute : Attribute
{
}
