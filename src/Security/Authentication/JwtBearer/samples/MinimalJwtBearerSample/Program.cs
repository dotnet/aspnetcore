// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication()
    .AddJwtBearer()
    .AddJwtBearer("ClaimedDetails")
    .AddJwtBearer("InvalidScheme");

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
