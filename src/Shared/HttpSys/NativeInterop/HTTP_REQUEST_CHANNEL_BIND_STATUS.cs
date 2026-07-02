// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.HttpSys.Internal;

/// <summary>
/// https://learn.microsoft.com/en-us/windows/win32/api/http/ns-http-http_channel_bind_info
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct HTTP_REQUEST_CHANNEL_BIND_STATUS
{
    public void* ServiceName;
    public byte* ChannelToken;
    public uint ChannelTokenSize;
    public uint Flags;
}
