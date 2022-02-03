// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.SignalR;

namespace TestServer;

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
