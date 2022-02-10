// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace JwtSample;

[Authorize(JwtBearerDefaults.AuthenticationScheme)]
public class Broadcaster : Hub
{
    public Task Broadcast(string sender, string message) =>
        Clients.All.SendAsync("Message", sender, message);
}
