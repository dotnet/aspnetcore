// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SignalR.Client.FunctionalTests
{
    internal class HeaderUserIdProvider : IUserIdProvider
    {
        public static readonly string HeaderName = "Super-Insecure-UserName";

        public string GetUserId(HubConnectionContext connection)
        {
            // Super-insecure user id provider :). Don't use this for anything real!
            return connection.GetHttpContext()?.Request?.Headers?[HeaderName];
        }
    }
}
