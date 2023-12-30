// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.InternalTesting;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class TlsAlpnSupportedAttribute : Attribute, ITestCondition
{
    public bool IsMet => true; // Replace with https://github.com/dotnet/runtime/issues/79687
    public string SkipReason => "TLS ALPN is not supported on the current test machine";
}
