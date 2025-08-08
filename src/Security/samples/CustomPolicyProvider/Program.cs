// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

using CustomPolicyProvider;

var builder = WebApplication.CreateBuilder(args);

// Replace the default authorization policy provider with our own
// custom provider which can return authorization policies for given
// policy names (instead of using the default policy provider)
builder.Services.AddSingleton<IAuthorizationPolicyProvider, MinimumAgePolicyProvider>();

// As always, handlers must be provided for the requirements of the authorization policies
builder.Services.AddSingleton<IAuthorizationHandler, MinimumAgeAuthorizationHandler>();

builder.Services.AddControllersWithViews();

// Add cookie authentication so that it's possible to sign-in to test the
// custom authorization policy behavior of the sample
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.AccessDeniedPath = "/account/denied";
        options.LoginPath = "/account/signin";
    });

var app = builder.Build();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

public partial class Program { }
