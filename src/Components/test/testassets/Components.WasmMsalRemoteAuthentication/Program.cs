// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
var authorityBaseUriBuilder = new UriBuilder(builder.HostEnvironment.BaseAddress)
{
    Host = "localhost",
    Path = "subdir/oidc",
};

builder.Services.AddMsalAuthentication(options =>
{
    options.ProviderOptions.Authentication.Authority = authorityBaseUriBuilder.Uri.AbsoluteUri;
    options.ProviderOptions.Authentication.ClientId = "s6BhdRkqt3";
    options.ProviderOptions.Authentication.RedirectUri = "msal-authentication/login-callback";
    options.ProviderOptions.Authentication.PostLogoutRedirectUri = "msal-authentication/logout-callback";
    options.ProviderOptions.LoginMode = "redirect";
    options.AuthenticationPaths.LogInPath = "msal-authentication/login";
    options.AuthenticationPaths.LogInCallbackPath = "msal-authentication/login-callback";
    options.AuthenticationPaths.LogInFailedPath = "msal-authentication/login-failed";
    options.AuthenticationPaths.LogOutPath = "msal-authentication/logout";
    options.AuthenticationPaths.LogOutCallbackPath = "msal-authentication/logout-callback";
    options.AuthenticationPaths.LogOutFailedPath = "msal-authentication/logout-failed";
    options.AuthenticationPaths.LogOutSucceededPath = "msal-authentication/logged-out";
    options.AuthenticationPaths.ProfilePath = "msal-authentication/profile";
    options.AuthenticationPaths.RegisterPath = "msal-authentication/register";
});

await builder.Build().RunAsync();
