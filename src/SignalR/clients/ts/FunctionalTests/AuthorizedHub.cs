// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FunctionalTests;

[Authorize(JwtBearerDefaults.AuthenticationScheme)]
public class HubWithAuthorization : Hub
{
    public string Echo(string message) => message;

    public string GetUserNameIdentifier() => Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    public string GetUserClaim(string type) => Context.User?.FindFirst(type)?.Value ?? string.Empty;
}
