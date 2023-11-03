// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
#if !NETSTANDARD2_0
using Microsoft.DotNet.RemoteExecutor;
#endif

namespace Microsoft.AspNetCore.InternalTesting;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
public class RemoteExecutionSupportedAttribute : Attribute, ITestCondition
{
#if NETSTANDARD2_0
    public bool IsMet => false;
#else
    public bool IsMet => RemoteExecutor.IsSupported;
#endif

    public string SkipReason { get; set; }
}
