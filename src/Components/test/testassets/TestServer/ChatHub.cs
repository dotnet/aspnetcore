// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace TestServer
{
    public class ChatHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        public string GetHeaderValue(string headerName)
        {
            var context = Context.GetHttpContext();

            if (context == null)
            {
                throw new InvalidOperationException("Unable to get HttpContext from request.");
            }

            var headers = context.Request.Headers;

            if (headers == null)
            {
                throw new InvalidOperationException("Unable to get headers from context.");
            }

            headers.TryGetValue(headerName, out var val);
            return val.ToString();
        }
    }
}
