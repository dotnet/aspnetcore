// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR.Client.FunctionalTests;

internal class HeaderUserIdProvider : IUserIdProvider
{
    public static readonly string HeaderName = "Super-Insecure-UserName";

    public string GetUserId(HubConnectionContext connection)
    {
        // Super-insecure user id provider :). Don't use this for anything real!
        return connection.GetHttpContext()?.Request?.Headers?[HeaderName];
    }
}
