// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;

namespace Microsoft.AspNetCore.Authentication.Negotiate;

internal sealed class NegotiateChannelBinding : ChannelBinding
{
    public unsafe NegotiateChannelBinding(ReadOnlyMemory<byte> channelBindingToken)
    {
        // ITlsConnectionFeature exposes managed bytes, but NegotiateAuthentication requires
        // a ChannelBinding handle that remains valid throughout the authentication exchange.
        Size = channelBindingToken.Length;
        SetHandle(Marshal.AllocHGlobal(Size));
        using var pinnedToken = channelBindingToken.Pin();
        Buffer.MemoryCopy(pinnedToken.Pointer, (void*)handle, Size, Size);
    }

    public override int Size { get; }

    protected override bool ReleaseHandle()
    {
        Marshal.FreeHGlobal(handle);
        return true;
    }
}
