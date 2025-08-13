// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using IdentitySample.PasskeyConformance;
using IdentitySample.PasskeyConformance.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Test;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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

builder.Services.AddIdentityCore<PocoUser>()
    .AddSignInManager();

builder.Services.AddSingleton<IUserStore<PocoUser>, InMemoryUserStore<PocoUser>>();
builder.Services.AddSingleton<IUserPasskeyStore<PocoUser>, InMemoryUserStore<PocoUser>>();

// In a real app, you'd rely on the SignInManager to securely store passkey state in
// an auth cookie, but we bypass the SignInManager for this sample so that we can
// customize the passkey options on a per-request basis. This cookie is a simple
// way for us to persist passkey attestation and assertion state across requests.
var passkeyStateCookie = new CookieBuilder
{
    Name = "PasskeyConformance.PasskeyState",
    HttpOnly = true,
    SameSite = SameSiteMode.None,
    SecurePolicy = CookieSecurePolicy.SameAsRequest,
};

var app = builder.Build();

var attestationGroup = app.MapGroup("/attestation");

attestationGroup.MapPost("/options", async (
    [FromServices] UserManager<PocoUser> userManager,
    [FromBody] ServerPublicKeyCredentialCreationOptionsRequest request,
    HttpContext context) =>
{
    var passkeyOptions = GetPasskeyOptionsFromCreationRequest(request);
    var userId = (await userManager.FindByNameAsync(request.Username) ?? new PocoUser()).Id;
    var userEntity = new PasskeyUserEntity
    {
        Id = userId,
        Name = request.Username,
        DisplayName = request.DisplayName
    };
    var passkeyHandler = new PasskeyHandler<PocoUser>(userManager, passkeyOptions);
    var result = await passkeyHandler.MakeCreationOptionsAsync(userEntity, context);
    var response = new ServerPublicKeyCredentialOptionsResponse(result.CreationOptionsJson);
    var state = new PasskeyAttestationState
    {
        Request = request,
        AttestationState = result.AttestationState,
    };
    var stateJson = JsonSerializer.Serialize(state, JsonSerializerOptions.Web);
    context.Response.Cookies.Append(passkeyStateCookie.Name, stateJson, passkeyStateCookie.Build(context));
    return Results.Ok(response);
});

attestationGroup.MapPost("/result", async (
    [FromServices] IUserPasskeyStore<PocoUser> passkeyStore,
    [FromServices] UserManager<PocoUser> userManager,
    [FromBody] JsonElement result,
    HttpContext context,
    CancellationToken cancellationToken) =>
{
    var credentialJson = ServerPublicKeyCredentialToJson(result);

    if (!context.Request.Cookies.TryGetValue(passkeyStateCookie.Name, out var stateJson))
    {
        return Results.BadRequest(new FailedResponse("There is no passkey attestation state present."));
    }

    var state = JsonSerializer.Deserialize<PasskeyAttestationState>(stateJson, JsonSerializerOptions.Web);
    if (state is null)
    {
        return Results.BadRequest(new FailedResponse("The passkey attestation state is invalid or missing."));
    }

    var passkeyOptions = GetPasskeyOptionsFromCreationRequest(state.Request);
    var passkeyHandler = new PasskeyHandler<PocoUser>(userManager, passkeyOptions);

    var attestationResult = await passkeyHandler.PerformAttestationAsync(new()
    {
        HttpContext = context,
        AttestationState = state.AttestationState,
        CredentialJson = credentialJson,
    });

    if (!attestationResult.Succeeded)
    {
        return Results.BadRequest(new FailedResponse($"Attestation failed: {attestationResult.Failure.Message}"));
    }

    // Create the user if they don't exist yet.
    var userEntity = attestationResult.UserEntity;
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

    await passkeyStore.AddOrUpdatePasskeyAsync(user, attestationResult.Passkey, cancellationToken).ConfigureAwait(false);
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
    [FromBody] ServerPublicKeyCredentialGetOptionsRequest request,
    HttpContext context) =>
{
    var user = await userManager.FindByNameAsync(request.Username);
    if (user is null)
    {
        return Results.BadRequest($"User with username {request.Username} does not exist.");
    }

    var passkeyOptions = GetPasskeyOptionsFromGetRequest(request);
    var passkeyHandler = new PasskeyHandler<PocoUser>(userManager, passkeyOptions);

    var result = await passkeyHandler.MakeRequestOptionsAsync(user, context);
    var response = new ServerPublicKeyCredentialOptionsResponse(result.RequestOptionsJson);
    var state = new PasskeyAssertionState
    {
        Request = request,
        AssertionState = result.AssertionState,
    };
    var stateJson = JsonSerializer.Serialize(state, JsonSerializerOptions.Web);
    context.Response.Cookies.Append(passkeyStateCookie.Name, stateJson, passkeyStateCookie.Build(context));
    return Results.Ok(response);
});

assertionGroup.MapPost("/result", async (
    [FromServices] UserManager<PocoUser> userManager,
    [FromBody] JsonElement result,
    HttpContext context) =>
{
    var credentialJson = ServerPublicKeyCredentialToJson(result);

    if (!context.Request.Cookies.TryGetValue(passkeyStateCookie.Name, out var stateJson))
    {
        return Results.BadRequest(new FailedResponse("There is no passkey assertion state present."));
    }

    var state = JsonSerializer.Deserialize<PasskeyAssertionState>(stateJson, JsonSerializerOptions.Web);
    if (state is null)
    {
        return Results.BadRequest(new FailedResponse("The passkey assertion state is invalid or missing."));
    }

    var passkeyOptions = GetPasskeyOptionsFromGetRequest(state.Request);
    var passkeyHandler = new PasskeyHandler<PocoUser>(userManager, passkeyOptions);

    var assertionResult = await passkeyHandler.PerformAssertionAsync(new()
    {
        HttpContext = context,
        CredentialJson = credentialJson,
        AssertionState = state.AssertionState,
    });
    if (!assertionResult.Succeeded)
    {
        return Results.BadRequest(new FailedResponse($"Assertion failed: {assertionResult.Failure.Message}"));
    }

    await userManager.AddOrUpdatePasskeyAsync(assertionResult.User, assertionResult.Passkey);

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

static ValueTask<bool> ValidateOriginAsync(PasskeyOriginValidationContext context)
{
    if (!Uri.TryCreate(context.Origin, UriKind.Absolute, out var uri))
    {
        return ValueTask.FromResult(false);
    }

    return ValueTask.FromResult(uri.Host == "localhost" && uri.Port == 7020);
}

static IOptions<IdentityPasskeyOptions> GetPasskeyOptionsFromCreationRequest(ServerPublicKeyCredentialCreationOptionsRequest request)
    => Options.Create(new IdentityPasskeyOptions()
    {
        ValidateOrigin = ValidateOriginAsync,
        AttestationConveyancePreference = request.Attestation,
        AuthenticatorAttachment = request.AuthenticatorSelection?.AuthenticatorAttachment,
        ResidentKeyRequirement = request.AuthenticatorSelection?.ResidentKey,
        UserVerificationRequirement = request.AuthenticatorSelection?.UserVerification,
    });

static IOptions<IdentityPasskeyOptions> GetPasskeyOptionsFromGetRequest(ServerPublicKeyCredentialGetOptionsRequest request)
    => Options.Create(new IdentityPasskeyOptions()
    {
        ValidateOrigin = ValidateOriginAsync,
        UserVerificationRequirement = request.UserVerification,
    });
