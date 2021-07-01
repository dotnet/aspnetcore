// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
