// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using IdentitySample.PasskeyUI;
using IdentitySample.PasskeyUI.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Test;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = options =>
    {
        options.HttpContext.Response.Redirect("/");
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization();

builder.Services.AddIdentityCore<PocoUser>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IUserStore<PocoUser>, InMemoryUserStore<PocoUser>>();
builder.Services.AddSingleton<IUserPasskeyStore<PocoUser>, InMemoryUserStore<PocoUser>>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>();

app.MapPost("attestation/options", async (
    [FromServices] UserManager<PocoUser> userManager,
    [FromServices] SignInManager<PocoUser> signInManager,
    [FromBody] PublicKeyCredentialCreationOptionsRequest request) =>
{
    var userId = (await userManager.FindByNameAsync(request.Username) ?? new PocoUser()).Id;
    var userEntity = new PasskeyUserEntity(userId, request.Username, null);
    var creationArgs = new PasskeyCreationArgs(userEntity)
    {
        AuthenticatorSelection = request.AuthenticatorSelection,
        Extensions = request.Extensions,
    };

    if (!string.IsNullOrEmpty(request.Attestation))
    {
        creationArgs.Attestation = request.Attestation;
    }

    var options = await signInManager.ConfigurePasskeyCreationOptionsAsync(creationArgs);
    return Results.Content(options.AsJson(), contentType: "application/json");
});

app.MapPost("assertion/options", async (
    [FromServices] UserManager<PocoUser> userManager,
    [FromServices] SignInManager<PocoUser> signInManager,
    [FromBody] PublicKeyCredentialGetOptionsRequest request) =>
{
    var user = !string.IsNullOrEmpty(request.Username)
        ? await userManager.FindByNameAsync(request.Username)
        : null;

    var requestArgs = new PasskeyRequestArgs<PocoUser>
    {
        User = user,
        Extensions = request.Extensions,
    };

    if (!string.IsNullOrEmpty(request.UserVerification))
    {
        requestArgs.UserVerification = request.UserVerification;
    }

    var options = await signInManager.ConfigurePasskeyRequestOptionsAsync(requestArgs);
    return Results.Content(options.AsJson(), contentType: "application/json");
});

app.MapPost("account/logout", async (
    [FromServices] SignInManager<PocoUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return TypedResults.LocalRedirect($"~/");
});

app.Run();

sealed class PublicKeyCredentialCreationOptionsRequest(string username)
{
    public string Username { get; } = username;
    public AuthenticatorSelectionCriteria? AuthenticatorSelection { get; set; }
    public JsonElement? Extensions { get; set; }
    public string? Attestation { get; set; } = "none";
}

sealed class PublicKeyCredentialGetOptionsRequest
{
    public string? Username { get; set; }
    public string? UserVerification { get; set; }
    public JsonElement? Extensions { get; set; }
}
