// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using BasicTestApp.AuthTest;
using Microsoft.AspNetCore.Mvc;

namespace Components.TestServer.Controllers;

[Route("api/[controller]")]
public class UserController : Controller
{
    // Servers are not expected to expose everything from the server-side ClaimsPrincipal
    // to the client. It's up to the developer to choose what kind of authentication state
    // data is needed on the client so it can display suitable options in the UI.
    // In this class, we inform the client only about certain roles and certain other claims.
    static readonly string[] ExposedRoles = new[] { "IrrelevantRole", "TestRole" };

    // GET api/user
    [HttpGet]
    public ClientSideAuthenticationStateData Get()
    {
        return new ClientSideAuthenticationStateData
        {
            IsAuthenticated = User.Identity.IsAuthenticated,
            UserName = User.Identity.Name,
            ExposedClaims = User.Claims
                .Where(c => c.Type == "test-claim" || IsExposedRole(c))
                .Select(c => new ExposedClaim { Type = c.Type, Value = c.Value }).ToList()
        };
    }

    private bool IsExposedRole(Claim claim)
        => claim.Type == ClaimTypes.Role
        && ExposedRoles.Contains(claim.Value);
}
