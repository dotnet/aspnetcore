// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Https.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests;

/// <summary>
/// Skips a test when the running framework or platform has no usable sans-IO TLS
/// implementation. This is the same question <see cref="SansIoTlsSupport.IsSupported"/>
/// answers for the middleware, so the tests skip exactly where the feature would decline to
/// engage rather than failing on platforms it never claimed to support.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class SansIoTlsSupportedAttribute : Attribute, ITestCondition
{
    public bool IsMet => SansIoTlsSupport.IsSupported;

    public string SkipReason => "The sans-IO TLS layer is not supported on this platform or runtime.";
}
