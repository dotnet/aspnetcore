// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Quic;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class MsQuicSupportedAttribute : Attribute, ITestCondition
    {
        public bool IsMet => QuicImplementationProviders.MsQuic.IsSupported;

        public string SkipReason => "QUIC is not supported on the current test machine";
    }
}
