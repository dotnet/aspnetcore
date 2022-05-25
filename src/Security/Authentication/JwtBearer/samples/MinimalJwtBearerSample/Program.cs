// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

builder.Authentication.AddJwtBearer();
builder.Authentication.AddJwtBearer("ClaimedDetails");

builder.Services.AddAuthorization(options =>
    options.AddPolicy("is_admin", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("is_admin", "true");
    }));

var app = builder.Build();

app.MapGet("/protected", (ClaimsPrincipal user) => $"Hello {user.Identity?.Name}!")
    .RequireAuthorization();

app.MapGet("/protected-with-claims", (ClaimsPrincipal user) =>
{
    return $"Glory be to the admin {user.Identity?.Name}!";
})
.RequireAuthorization("is_admin");

app.Run();
