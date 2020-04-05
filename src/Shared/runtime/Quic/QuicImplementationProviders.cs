// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Quic
{
    internal static class QuicImplementationProviders
    {
        public static Implementations.QuicImplementationProvider Mock { get; } = new Implementations.Mock.MockImplementationProvider();
        public static Implementations.QuicImplementationProvider MsQuic { get; } = new Implementations.MsQuic.MsQuicImplementationProvider();
        public static Implementations.QuicImplementationProvider Default => MsQuic;
    }
}
