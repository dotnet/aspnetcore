// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authentication.BearerToken.DTO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.DTO;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Provides extension methods for <see cref="IEndpointRouteBuilder"/> to add identity endpoints.
/// </summary>
public static class IdentityApiEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Add endpoints for registering, logging in, and logging out using ASP.NET Core Identity.
    /// </summary>
    /// <typeparam name="TUser">The type describing the user. This should match the generic parameter in <see cref="UserManager{TUser}"/>.</typeparam>
    /// <param name="endpoints">
    /// The <see cref="IEndpointRouteBuilder"/> to add the identity endpoints to.
    /// Call <see cref="EndpointRouteBuilderExtensions.MapGroup(IEndpointRouteBuilder, string)"/> to add a prefix to all the endpoints.
    /// </param>
    /// <returns>An <see cref="IEndpointConventionBuilder"/> to further customize the added endpoints.</returns>
    public static IEndpointConventionBuilder MapIdentityApi<TUser>(this IEndpointRouteBuilder endpoints)
        where TUser : class, new()
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var timeProvider = endpoints.ServiceProvider.GetRequiredService<TimeProvider>();
        var bearerTokenOptions = endpoints.ServiceProvider.GetRequiredService<IOptionsMonitor<BearerTokenOptions>>();
        var emailSender = endpoints.ServiceProvider.GetRequiredService<IEmailSender>();
        var linkGenerator = endpoints.ServiceProvider.GetRequiredService<LinkGenerator>();

        // We'll figure out a unique endpoint name based on the final route pattern during endpoint generation.
        string? confirmEmailEndpointName = null;

        var routeGroup = endpoints.MapGroup("");

        // NOTE: We cannot inject UserManager<TUser> directly because the TUser generic parameter is currently unsupported by RDG.
        // https://github.com/dotnet/aspnetcore/issues/47338
        routeGroup.MapPost("/register", async Task<Results<Ok, ValidationProblem>>
            ([FromBody] RegisterRequest registration, [FromServices] IServiceProvider sp) =>
        {
            var userManager = sp.GetRequiredService<UserManager<TUser>>();

            if (!userManager.SupportsUserEmail)
            {
                throw new NotSupportedException($"{nameof(MapIdentityApi)} requires a user store with email support.");
            }

            var userStore = sp.GetRequiredService<IUserStore<TUser>>();
            var emailStore = (IUserEmailStore<TUser>)userStore;

            var user = new TUser();
            await userStore.SetUserNameAsync(user, registration.Username, CancellationToken.None);
            await emailStore.SetEmailAsync(user, registration.Email, CancellationToken.None);
            var result = await userManager.CreateAsync(user, registration.Password);

            if (result.Succeeded)
            {
                await SendConfirmationEmailAsync(user, userManager, registration.Email);
                return TypedResults.Ok();
            }

            return CreateValidationProblem(result);
        });

        routeGroup.MapPost("/login", async Task<Results<Ok<AccessTokenResponse>, EmptyHttpResult, ProblemHttpResult>>
            ([FromBody] LoginRequest login, [FromQuery] bool? cookieMode, [FromQuery] bool? persistCookies, [FromServices] IServiceProvider sp) =>
        {
            var signInManager = sp.GetRequiredService<SignInManager<TUser>>();

            signInManager.PrimaryAuthenticationScheme = cookieMode == true ? IdentityConstants.ApplicationScheme : IdentityConstants.BearerScheme;
            var isPersistent = persistCookies ?? true;

            var result = await signInManager.PasswordSignInAsync(login.Username, login.Password, isPersistent, lockoutOnFailure: true);

            if (result.RequiresTwoFactor)
            {
                if (!string.IsNullOrEmpty(login.TwoFactorCode))
                {
                    result = await signInManager.TwoFactorAuthenticatorSignInAsync(login.TwoFactorCode, isPersistent, rememberClient: isPersistent);
                }
                else if (!string.IsNullOrEmpty(login.TwoFactorRecoveryCode))
                {
                    result = await signInManager.TwoFactorRecoveryCodeSignInAsync(login.TwoFactorRecoveryCode);
                }
            }

            if (result.Succeeded)
            {
                // The signInManager already produced the needed response in the form of a cookie or bearer token.
                return TypedResults.Empty;
            }

            return TypedResults.Problem(result.ToString(), statusCode: StatusCodes.Status401Unauthorized);
        });

        routeGroup.MapPost("/refresh", async Task<Results<Ok<AccessTokenResponse>, UnauthorizedHttpResult, SignInHttpResult, ChallengeHttpResult>>
            ([FromBody] RefreshRequest refreshRequest, [FromServices] IServiceProvider sp) =>
        {
            var signInManager = sp.GetRequiredService<SignInManager<TUser>>();
            var refreshTokenProtector = bearerTokenOptions.Get(IdentityConstants.BearerScheme).RefreshTokenProtector;
            var refreshTicket = refreshTokenProtector.Unprotect(refreshRequest.RefreshToken);

            // Reject the /refresh attempt with a 401 if the token expired or the security stamp validation fails
            if (refreshTicket?.Properties?.ExpiresUtc is not { } expiresUtc ||
                timeProvider.GetUtcNow() >= expiresUtc ||
                await signInManager.ValidateSecurityStampAsync(refreshTicket.Principal) is not TUser user)

            {
                return TypedResults.Challenge();
            }

            var newPrincipal = await signInManager.CreateUserPrincipalAsync(user);
            return TypedResults.SignIn(newPrincipal, authenticationScheme: IdentityConstants.BearerScheme);
        });

        routeGroup.MapGet("/confirmEmail", async Task<Results<ContentHttpResult, UnauthorizedHttpResult>>
            ([FromQuery] string userId, [FromQuery] string code, [FromQuery] string? changedEmail, [FromServices] IServiceProvider sp) =>
        {
            var userManager = sp.GetRequiredService<UserManager<TUser>>();
            if (await userManager.FindByIdAsync(userId) is not { } user)
            {
                // We could respond with a 404 instead of a 401 like Identity UI, but that feels like unnecessary information.
                return TypedResults.Unauthorized();
            }

            IdentityResult result;
            try
            {
                code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
                if (string.IsNullOrEmpty(changedEmail))
                {
                    result = await userManager.ConfirmEmailAsync(user, code);
                }
                else
                {
                    result = await userManager.ChangeEmailAsync(user, changedEmail, code);
                }
            }
            catch (FormatException)
            {
                return TypedResults.Unauthorized();
            }

            if (!result.Succeeded)
            {
                return TypedResults.Unauthorized();
            }

            return TypedResults.Text("Thank you for confirming your email.");
        })
        .Add(endpointBuilder =>
        {
            var finalPattern = ((RouteEndpointBuilder)endpointBuilder).RoutePattern.RawText;
            confirmEmailEndpointName = $"{nameof(MapIdentityApi)}-{finalPattern}";
            endpointBuilder.Metadata.Add(new EndpointNameMetadata(confirmEmailEndpointName));
            endpointBuilder.Metadata.Add(new RouteNameMetadata(confirmEmailEndpointName));
        });

        routeGroup.MapPost("/resendConfirmationEmail", async Task<Ok>
            ([FromBody] ResendEmailRequest resendRequest, [FromServices] IServiceProvider sp) =>
        {
            var userManager = sp.GetRequiredService<UserManager<TUser>>();
            if (await userManager.FindByEmailAsync(resendRequest.Email) is not { } user)
            {
                return TypedResults.Ok();
            }

            await SendConfirmationEmailAsync(user, userManager, resendRequest.Email);
            return TypedResults.Ok();
        });

        routeGroup.MapPost("/resetPassword", async Task<Results<Ok, ValidationProblem>>
            ([FromBody] ResetPasswordRequest resetRequest, [FromServices] IServiceProvider sp) =>
        {
            var userManager = sp.GetRequiredService<UserManager<TUser>>();

            if (!string.IsNullOrEmpty(resetRequest.ResetCode) && string.IsNullOrEmpty(resetRequest.NewPassword))
            {
                return CreateValidationProblem("MissingNewPassword", "A password reset code was provided without a new password.");
            }

            var user = await userManager.FindByEmailAsync(resetRequest.Email);

            if (user is null || !(await userManager.IsEmailConfirmedAsync(user)))
            {
                // Don't reveal that the user does not exist or is not confirmed, so don't return a 200 if we would have
                // returned a 400 for an invalid code given a valid user email.
                if (!string.IsNullOrEmpty(resetRequest.ResetCode))
                {
                    return CreateValidationProblem(IdentityResult.Failed(userManager.ErrorDescriber.InvalidToken()));
                }
            }
            else if (string.IsNullOrEmpty(resetRequest.ResetCode))
            {
                var code = await userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                await emailSender.SendEmailAsync(resetRequest.Email, "Reset your password",
                    $"Reset your password using the following code: {HtmlEncoder.Default.Encode(code)}");
            }
            else
            {
                Debug.Assert(!string.IsNullOrEmpty(resetRequest.NewPassword));

                IdentityResult result;
                try
                {
                    var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(resetRequest.ResetCode));
                    result = await userManager.ResetPasswordAsync(user, code, resetRequest.NewPassword);
                }
                catch (FormatException)
                {
                    result = IdentityResult.Failed(userManager.ErrorDescriber.InvalidToken());
                }

                if (!result.Succeeded)
                {
                    return CreateValidationProblem(result);
                }
            }

            return TypedResults.Ok();
        });

        var accountGroup = routeGroup.MapGroup("/account").RequireAuthorization();

        accountGroup.MapGet("/2fa", async Task<Results<Ok<TwoFactorResponse>, NotFound>>
            (ClaimsPrincipal claimsPrincipal, [FromServices] IServiceProvider sp) =>
        {
            var signInManager = sp.GetRequiredService<SignInManager<TUser>>();
            if (await signInManager.UserManager.GetUserAsync(claimsPrincipal) is not { } user)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.Ok(await CreateTwoFactorResponseAsync(user, signInManager));
        });

        accountGroup.MapPost("/2fa", async Task<Results<Ok<TwoFactorResponse>, ValidationProblem, NotFound>>
            (ClaimsPrincipal claimsPrincipal, [FromBody] TwoFactorRequest tfaRequest, [FromServices] IServiceProvider sp) =>
        {
            var signInManager = sp.GetRequiredService<SignInManager<TUser>>();
            var userManager = signInManager.UserManager;
            if (await userManager.GetUserAsync(claimsPrincipal) is not { } user)
            {
                return TypedResults.NotFound();
            }

            if (tfaRequest.Enable == true)
            {
                if (tfaRequest.ResetSharedKey)
                {
                    return CreateValidationProblem("CannotResetSharedKeyAndEnable",
                        "Resetting the 2fa shared key must disable 2fa until a 2fa token based on the new shared key is validated.");
                }
                else if (string.IsNullOrEmpty(tfaRequest.TwoFactorCode))
                {
                    return CreateValidationProblem("RequiresTwoFactor",
                        "No 2fa token was provided by the request. A valid 2fa token is required to enable 2fa.");
                }
                else if (!await userManager.VerifyTwoFactorTokenAsync(user, userManager.Options.Tokens.AuthenticatorTokenProvider, tfaRequest.TwoFactorCode))
                {
                    return CreateValidationProblem("InvalidTwoFactorCode",
                        "The 2fa token provided by the request was invalid. A valid 2fa token is required to enable 2fa.");
                }

                await userManager.SetTwoFactorEnabledAsync(user, true);
            }
            else if (tfaRequest.Enable == false || tfaRequest.ResetSharedKey)
            {
                await userManager.SetTwoFactorEnabledAsync(user, false);
            }

            if (tfaRequest.ResetSharedKey)
            {
                await userManager.ResetAuthenticatorKeyAsync(user);
            }

            string[]? recoveryCodes = null;
            if (tfaRequest.ResetRecoveryCodes || (tfaRequest.Enable == true && await userManager.CountRecoveryCodesAsync(user) == 0))
            {
                var recoveryCodesEnumerable = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
                recoveryCodes = recoveryCodesEnumerable?.ToArray();
            }

            if (tfaRequest.ForgetMachine)
            {
                await signInManager.ForgetTwoFactorClientAsync();
            }

            return TypedResults.Ok(await CreateTwoFactorResponseAsync(user, signInManager, recoveryCodes));
        });

        accountGroup.MapGet("/info", async Task<Results<Ok<InfoResponse>, ValidationProblem, NotFound>>
            (ClaimsPrincipal claimsPrincipal, [FromServices] IServiceProvider sp) =>
        {
            var userManager = sp.GetRequiredService<UserManager<TUser>>();
            if (await userManager.GetUserAsync(claimsPrincipal) is not { } user)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.Ok(await CreateInfoResponseAsync(user, claimsPrincipal, userManager));
        });

        accountGroup.MapPost("/info", async Task<Results<Ok<InfoResponse>, ValidationProblem, NotFound>>
            (HttpContext httpContext, [FromBody] InfoRequest infoRequest, [FromServices] IServiceProvider sp) =>
        {
            var signInManager = sp.GetRequiredService<SignInManager<TUser>>();
            var userManager = signInManager.UserManager;
            if (await userManager.GetUserAsync(httpContext.User) is not { } user)
            {
                return TypedResults.NotFound();
            }

            List<IdentityResult>? failedResults = null;

            if (!string.IsNullOrEmpty(infoRequest.NewUsername))
            {
                var userName = await userManager.GetUserNameAsync(user);

                if (userName != infoRequest.NewUsername)
                {
                    AddIfFailed(ref failedResults, await userManager.SetUserNameAsync(user, infoRequest.NewUsername));
                }
            }

            if (!string.IsNullOrEmpty(infoRequest.NewEmail))
            {
                var email = await userManager.GetEmailAsync(user);

                if (email != infoRequest.NewEmail)
                {
                    await SendConfirmationEmailAsync(user, userManager, infoRequest.NewEmail, isChange: true);
                }
            }

            if (!string.IsNullOrEmpty(infoRequest.NewPassword))
            {
                if (string.IsNullOrEmpty(infoRequest.OldPassword))
                {
                    AddIfFailed(ref failedResults, IdentityResult.Failed(new IdentityError
                    {
                        Code = "OldPasswordRequired",
                        Description = "The old password is required to set a new password. If the old password is forgotten, use /resetPassword.",
                    }));
                }
                else
                {
                    AddIfFailed(ref failedResults, await userManager.ChangePasswordAsync(user, infoRequest.OldPassword, infoRequest.NewPassword));
                }
            }

            // Update cookie if the user is authenticated that way.
            // Currently, the user will have to log in again with bearer tokens to see updated claims.
            var authFeature = httpContext.Features.GetRequiredFeature<IAuthenticateResultFeature>();
            if (authFeature.AuthenticateResult?.Ticket?.AuthenticationScheme == IdentityConstants.ApplicationScheme)
            {
                await signInManager.RefreshSignInAsync(user);
            }

            if (failedResults is not null)
            {
                return CreateValidationProblem(failedResults);
            }
            else
            {
                return TypedResults.Ok(await CreateInfoResponseAsync(user, httpContext.User, userManager));
            }
        });

        async Task SendConfirmationEmailAsync(TUser user, UserManager<TUser> userManager, string email, bool isChange = false)
        {
            if (confirmEmailEndpointName is null)
            {
                throw new NotSupportedException("No email confirmation endpoint was registered!");
            }

            var code = isChange
                ? await userManager.GenerateChangeEmailTokenAsync(user, email)
                : await userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var userId = await userManager.GetUserIdAsync(user);
            var routeValues = new RouteValueDictionary()
            {
                ["userId"] = userId,
                ["code"] = code,
            };

            if (isChange)
            {
                // This is validated by the /confirmEmail endpoint on change.
                routeValues.Add("changedEmail", email);
            }

            var confirmEmailUrl = linkGenerator.GetPathByName(confirmEmailEndpointName, routeValues)
                ?? throw new NotSupportedException($"Could not find endpoint named '{confirmEmailEndpointName}'.");

            await emailSender.SendEmailAsync(email, "Confirm your email",
                $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(confirmEmailUrl)}'>clicking here</a>.");
        }

        return new IdentityEndpointsConventionBuilder(routeGroup);
    }

    private static void AddIfFailed(ref List<IdentityResult>? results, IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        results ??= new();
        results.Add(result);
    }

    private static ValidationProblem CreateValidationProblem(string errorCode, string errorDescription) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> {
            { errorCode, new[] { errorDescription } }
        });

    private static ValidationProblem CreateValidationProblem(IdentityResult result)
    {
        var errorDictionary = new Dictionary<string, string[]>(1);
        AddErrorsToDictionary(errorDictionary, result);
        return TypedResults.ValidationProblem(errorDictionary);
    }

    private static ValidationProblem CreateValidationProblem(List<IdentityResult> results)
    {
        var errorDictionary = new Dictionary<string, string[]>(results.Count);

        foreach (var result in results)
        {
            AddErrorsToDictionary(errorDictionary, result);
        }

        return TypedResults.ValidationProblem(errorDictionary);
    }

    private static void AddErrorsToDictionary(Dictionary<string, string[]> errorDictionary, IdentityResult result)
    {
        // We expect a single error code and description in the normal case.
        // This could be golfed with GroupBy and ToDictionary, but perf! :P
        Debug.Assert(!result.Succeeded);
        foreach (var error in result.Errors)
        {
            string[] newDescriptions;

            if (errorDictionary.TryGetValue(error.Code, out var descriptions))
            {
                newDescriptions = new string[descriptions.Length + 1];
                Array.Copy(descriptions, newDescriptions, descriptions.Length);
                newDescriptions[descriptions.Length] = error.Description;
            }
            else
            {
                newDescriptions = new[] { error.Description };
            }

            errorDictionary[error.Code] = newDescriptions;
        }
    }

    private static async Task<TwoFactorResponse> CreateTwoFactorResponseAsync<TUser>(TUser user, SignInManager<TUser> signInManager, string[]? recoveryCodes = null)
        where TUser : class
    {
        var userManager = signInManager.UserManager;

        var key = await userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
            key = await userManager.GetAuthenticatorKeyAsync(user);

            if (string.IsNullOrEmpty(key))
            {
                throw new NotSupportedException("The user manager must produce an authenticator key after reset.");
            }
        }

        return new()
        {
            SharedKey = key,
            RecoveryCodes = recoveryCodes,
            RecoveryCodesLeft = recoveryCodes?.Length ?? await userManager.CountRecoveryCodesAsync(user),
            IsTwoFactorEnabled = await userManager.GetTwoFactorEnabledAsync(user),
            IsMachineRemembered = await signInManager.IsTwoFactorClientRememberedAsync(user),
        };
    }

    private static async Task<InfoResponse> CreateInfoResponseAsync<TUser>(TUser user, ClaimsPrincipal claimsPrincipal, UserManager<TUser> userManager)
        where TUser : class
    {
        return new()
        {
            Username = await userManager.GetUserNameAsync(user) ?? throw new NotSupportedException("Users must have a user name."),
            Email = await userManager.GetEmailAsync(user) ?? throw new NotSupportedException("Users must have an email."),
            Claims = claimsPrincipal.Claims.ToDictionary(c => c.Type, c => c.Value),
        };
    }

    // Wrap RouteGroupBuilder with a non-public type to avoid a potential future behavioral breaking change.
    private sealed class IdentityEndpointsConventionBuilder(RouteGroupBuilder inner) : IEndpointConventionBuilder
    {
        private IEndpointConventionBuilder InnerAsConventionBuilder => inner;

        public void Add(Action<EndpointBuilder> convention) => InnerAsConventionBuilder.Add(convention);
        public void Finally(Action<EndpointBuilder> finallyConvention) => InnerAsConventionBuilder.Finally(finallyConvention);
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    private sealed class FromBodyAttribute : Attribute, IFromBodyMetadata
    {
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    private sealed class FromServicesAttribute : Attribute, IFromServiceMetadata
    {
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    private sealed class FromQueryAttribute : Attribute, IFromQueryMetadata
    {
        public string? Name => null;
    }
}
