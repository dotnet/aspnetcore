// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;

namespace Microsoft.AspNetCore.SignalR.Client.FunctionalTests;

internal class HeaderUserIdProvider : IUserIdProvider
{
    public static readonly string HeaderName = "Super-Insecure-UserName";

    public string GetUserId(HubConnectionContext connection)
    {
        // Prefer the authenticated principal's NameIdentifier so the user id tracks the current
        // token and changes when an authentication refresh swaps in a principal with a different subject.
        var nameIdentifier = connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(nameIdentifier))
        {
            return nameIdentifier;
        }

        // Super-insecure user id provider :). Don't use this for anything real!
        return connection.GetHttpContext()?.Request?.Headers?[HeaderName];
    }
}
