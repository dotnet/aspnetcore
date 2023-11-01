// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Quic;

namespace Microsoft.AspNetCore.InternalTesting;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class MsQuicSupportedAttribute : Attribute, ITestCondition
{
#pragma warning disable CA2252 // This API requires opting into preview features
    public bool IsMet => QuicListener.IsSupported;
#pragma warning restore CA2252 // This API requires opting into preview features

    public string SkipReason => "QUIC is not supported on the current test machine";
}
