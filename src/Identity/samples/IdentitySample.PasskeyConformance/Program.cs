// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using IdentitySample.PasskeyConformance;
using IdentitySample.PasskeyConformance.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Test;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies(builder =>
    {
        builder.TwoFactorUserIdCookie!.Configure(options =>
        {
            options.Cookie.SameSite = SameSiteMode.None;
        });
    });

builder.Services.AddIdentityCore<PocoUser>(options =>
    {
        // The origin can't be inferred from the request, since the conformance testing tool
        // does not send the Origin header. Therefore, we need to explicitly set the allowed origins.
        options.Passkey.AllowedOrigins = [
            "http://localhost:7020",
            "https://localhost:7020"
        ];
    })
    .AddSignInManager();

builder.Services.AddSingleton<IUserStore<PocoUser>, InMemoryUserStore<PocoUser>>();
builder.Services.AddSingleton<IUserPasskeyStore<PocoUser>, InMemoryUserStore<PocoUser>>();

var app = builder.Build();

var attestationGroup = app.MapGroup("/attestation");

attestationGroup.MapPost("/options", async (
    [FromServices] UserManager<PocoUser> userManager,
    [FromServices] SignInManager<PocoUser> signInManager,
    [FromBody] ServerPublicKeyCredentialCreationOptionsRequest request) =>
{
    var userId = (await userManager.FindByNameAsync(request.Username) ?? new PocoUser()).Id;
    var userEntity = new PasskeyUserEntity(userId, request.Username, request.DisplayName);
    var creationArgs = new PasskeyCreationArgs(userEntity)
    {
        AuthenticatorSelection = request.AuthenticatorSelection,
        Extensions = request.Extensions,
    };

    if (request.Attestation is { Length: > 0 } attestation)
    {
        creationArgs.Attestation = attestation;
    }

    var options = await signInManager.ConfigurePasskeyCreationOptionsAsync(creationArgs);
    var response = new ServerPublicKeyCredentialOptionsResponse(options.AsJson());
    return Results.Ok(response);
});

attestationGroup.MapPost("/result", async (
    [FromServices] IUserPasskeyStore<PocoUser> passkeyStore,
    [FromServices] UserManager<PocoUser> userManager,
    [FromServices] SignInManager<PocoUser> signInManager,
    [FromBody] JsonElement result,
    CancellationToken cancellationToken) =>
{
    var credentialJson = ServerPublicKeyCredentialToJson(result);

    var options = await signInManager.RetrievePasskeyCreationOptionsAsync();

    await signInManager.SignOutAsync();

    if (options is null)
    {
        return Results.BadRequest(new FailedResponse("There are no original passkey options present."));
    }

    var attestationResult = await signInManager.PerformPasskeyAttestationAsync(credentialJson, options);
    if (!attestationResult.Succeeded)
    {
        return Results.BadRequest(new FailedResponse($"Attestation failed: {attestationResult.Failure.Message}"));
    }

    // Create the user if they don't exist yet.
    var userEntity = options.UserEntity;
    var user = await userManager.FindByIdAsync(userEntity.Id);
    if (user is null)
    {
        user = new PocoUser(userName: userEntity.Name)
        {
            Id = userEntity.Id,
        };
        var createUserResult = await userManager.CreateAsync(user);
        if (!createUserResult.Succeeded)
        {
            return Results.InternalServerError(new FailedResponse("Failed to create the user."));
        }
    }

    await passkeyStore.SetPasskeyAsync(user, attestationResult.Passkey, cancellationToken).ConfigureAwait(false);
    var updateResult = await userManager.UpdateAsync(user).ConfigureAwait(false);
    if (!updateResult.Succeeded)
    {
        return Results.InternalServerError(new FailedResponse("Unable to update the user."));
    }

    return Results.Ok(new OkResponse());
});

var assertionGroup = app.MapGroup("/assertion");

assertionGroup.MapPost("/options", async (
    [FromServices] UserManager<PocoUser> userManager,
    [FromServices] SignInManager<PocoUser> signInManager,
    [FromBody] ServerPublicKeyCredentialGetOptionsRequest request) =>
{
    var user = await userManager.FindByNameAsync(request.Username);
    if (user is null)
    {
        return Results.BadRequest($"User with username {request.Username} does not exist.");
    }

    var requestArgs = new PasskeyRequestArgs<PocoUser>
    {
        User = user,
        UserVerification = request.UserVerification,
        Extensions = request.Extensions,
    };
    var options = await signInManager.ConfigurePasskeyRequestOptionsAsync(requestArgs);
    var response = new ServerPublicKeyCredentialOptionsResponse(options.AsJson());
    return Results.Ok(response);
});

assertionGroup.MapPost("/result", async (
    [FromServices] SignInManager<PocoUser> signInManager,
    [FromServices] UserManager<PocoUser> userManager,
    [FromBody] JsonElement result) =>
{
    var credentialJson = ServerPublicKeyCredentialToJson(result);

    var options = await signInManager.RetrievePasskeyRequestOptionsAsync();
    await signInManager.SignOutAsync();

    if (options is null)
    {
        return Results.BadRequest(new FailedResponse("There are no original passkey options present."));
    }

    var assertionResult = await signInManager.PerformPasskeyAssertionAsync(credentialJson, options);
    if (!assertionResult.Succeeded)
    {
        return Results.BadRequest(new FailedResponse($"Assertion failed: {assertionResult.Failure.Message}"));
    }

    await userManager.SetPasskeyAsync(assertionResult.User, assertionResult.Passkey);

    return Results.Ok(new OkResponse());
});

app.UseHttpsRedirection();

app.Run();

static string ServerPublicKeyCredentialToJson(JsonElement serverPublicKeyCredential)
{
    // The response from the conformance testing tool comes in this format:
    // https://github.com/fido-alliance/conformance-test-tools-resources/blob/main/docs/FIDO2/Server/Conformance-Test-API.md#serverpublickeycredential
    // ...but we want it to be in this format:
    // https://www.w3.org/TR/webauthn-3/#dictdef-registrationresponsejson
    // This mainly entails renaming the 'getClientExtensionResults' property
    using var stream = new MemoryStream();
    using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions()
    {
        Indented = true,
    });
    writer.WriteStartObject();
    foreach (var property in serverPublicKeyCredential.EnumerateObject())
    {
        switch (property.Name)
        {
            case "getClientExtensionResults":
                writer.WritePropertyName("clientExtensionResults");
                break;
            default:
                writer.WritePropertyName(property.Name);
                break;
        }
        property.Value.WriteTo(writer);
    }
    writer.WriteEndObject();
    writer.Flush();
    var resultBytes = stream.ToArray();
    var resultJson = Encoding.UTF8.GetString(resultBytes);
    return resultJson;
}
