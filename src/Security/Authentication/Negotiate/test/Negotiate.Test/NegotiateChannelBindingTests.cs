// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Authentication.Negotiate;

public class NegotiateChannelBindingTests
{
    [Fact]
    public void Constructor_CopiesChannelBindingToken()
    {
        var channelBindingToken = new byte[] { 0x01, 0x23, 0x45, 0x67 };

        using var channelBinding = new NegotiateChannelBinding(channelBindingToken);
        channelBindingToken[0] = 0xff;
        var copiedToken = new byte[channelBinding.Size];
        Marshal.Copy(channelBinding.DangerousGetHandle(), copiedToken, 0, copiedToken.Length);

        Assert.Equal(new byte[] { 0x01, 0x23, 0x45, 0x67 }, copiedToken);
    }
}
