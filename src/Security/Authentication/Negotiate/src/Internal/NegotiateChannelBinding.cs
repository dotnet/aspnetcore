// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;

namespace Microsoft.AspNetCore.Authentication.Negotiate;

internal sealed class NegotiateChannelBinding : ChannelBinding
{
    public NegotiateChannelBinding(ReadOnlyMemory<byte> channelBindingToken)
    {
        var bytes = channelBindingToken.ToArray();
        Size = bytes.Length;
        SetHandle(Marshal.AllocHGlobal(Size));
        Marshal.Copy(bytes, 0, handle, Size);
    }

    public override int Size { get; }

    protected override bool ReleaseHandle()
    {
        Marshal.FreeHGlobal(handle);
        return true;
    }
}
