// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace System.Net.Quic
{
    internal static class QuicImplementationProviders
    {
        public static Implementations.QuicImplementationProvider Mock { get; } = new Implementations.Mock.MockImplementationProvider();
        public static Implementations.QuicImplementationProvider MsQuic { get; } = new Implementations.MsQuic.MsQuicImplementationProvider();
        public static Implementations.QuicImplementationProvider Default => MsQuic;
    }
}
