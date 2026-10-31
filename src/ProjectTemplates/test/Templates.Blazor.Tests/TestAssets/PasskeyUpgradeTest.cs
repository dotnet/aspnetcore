// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TestApp.Data;

namespace TestApp;

internal static class PasskeyUpgradeTest
{
    internal static void AddServices(IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<PasskeyUpgradeTestState>();
        new IdentityBuilder(typeof(ApplicationUser), services).AddUserManager<PasskeyUpgradeTestUserManager>();
    }

    internal static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/test/passkeys", async (
            HttpContext context, UserManager<ApplicationUser> userManager, PasskeyUpgradeTestState state) =>
        {
            var user = await userManager.GetUserAsync(context.User);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var passkeys = await userManager.GetPasskeysAsync(user);
            return Results.Ok(new
            {
                user.Id,
                user.Email,
                Passkeys = passkeys.Select(passkey => new
                {
                    CredentialId = Base64Url.EncodeToString(passkey.CredentialId),
                    passkey.IsUserVerified,
                }),
                State = state,
            });
        }).RequireAuthorization();
    }
}

internal sealed class PasskeyUpgradeTestState
{
    public int SaveAttempts { get; set; }
    public string Outcome { get; set; } = "";
    public string UserId { get; set; } = "";
    public string CredentialId { get; set; } = "";
    public bool IsUserVerified { get; set; }
    public bool Saved { get; set; }
    public bool LookupFailed { get; set; }
}

internal sealed class PasskeyUpgradeTestUserManager(
    IUserStore<ApplicationUser> store,
    IOptions<IdentityOptions> optionsAccessor,
    IPasswordHasher<ApplicationUser> passwordHasher,
    IEnumerable<IUserValidator<ApplicationUser>> userValidators,
    IEnumerable<IPasswordValidator<ApplicationUser>> passwordValidators,
    ILookupNormalizer keyNormalizer,
    IdentityErrorDescriber errors,
    IServiceProvider services,
    ILogger<UserManager<ApplicationUser>> logger)
    : UserManager<ApplicationUser>(store, optionsAccessor, passwordHasher, userValidators,
        passwordValidators, keyNormalizer, errors, services, logger)
{
    private readonly PasskeyUpgradeTestState _state = services.GetRequiredService<PasskeyUpgradeTestState>();
    private readonly IHttpContextAccessor _httpContextAccessor = services.GetRequiredService<IHttpContextAccessor>();
    private bool _saveAttempted;

    public override async Task<IdentityResult> AddOrUpdatePasskeyAsync(ApplicationUser user, UserPasskeyInfo passkey)
    {
        _saveAttempted = true;
        _state.SaveAttempts++;
        _state.Outcome = _httpContextAccessor.HttpContext!.Request.Headers["X-Passkey-Test-Outcome"].ToString();
        _state.UserId = user.Id;
        _state.CredentialId = Base64Url.EncodeToString(passkey.CredentialId);
        _state.IsUserVerified = passkey.IsUserVerified;
        if (_state.Outcome is "Success" or "SavedThenFailure")
        {
            var result = await base.AddOrUpdatePasskeyAsync(user, passkey);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException("The real passkey store failed.");
            }
            _state.Saved = true;
            if (_state.Outcome is "Success")
            {
                return result;
            }
        }
        if (_state.Outcome is "DatabaseFailure")
        {
            throw new DbUpdateException("Simulated passkey persistence failure.");
        }

        return IdentityResult.Failed(new IdentityError { Description = "Simulated passkey persistence failure." });
    }

    public override Task<ApplicationUser?> FindByPasskeyIdAsync(byte[] credentialId)
    {
        if (_saveAttempted && _state.Outcome is "LookupFailure")
        {
            _state.LookupFailed = true;
            throw new DbUpdateException("Simulated inconclusive passkey lookup.");
        }

        return base.FindByPasskeyIdAsync(credentialId);
    }
}
