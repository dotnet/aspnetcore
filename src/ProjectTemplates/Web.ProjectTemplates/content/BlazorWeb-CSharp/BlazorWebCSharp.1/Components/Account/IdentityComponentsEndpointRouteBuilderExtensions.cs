using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using BlazorWebCSharp._1.Components.Account;
using BlazorWebCSharp._1.Components.Account.Pages;
using BlazorWebCSharp._1.Components.Account.Pages.Manage;
using BlazorWebCSharp._1.Components.Account.Shared;
using BlazorWebCSharp._1.Data;

namespace Microsoft.AspNetCore.Routing;

internal static class IdentityComponentsEndpointRouteBuilderExtensions
{
    // These endpoints are required by the Identity Razor components defined in the /Components/Account/Pages directory of this project.
    public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var accountGroup = endpoints.MapGroup("/Account");

        accountGroup.MapPost("/PerformExternalLogin", (
            HttpContext context,
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromForm] string provider,
            [FromForm] string returnUrl) =>
        {
            IEnumerable<KeyValuePair<string, StringValues>> query = [
                new("ReturnUrl", returnUrl),
                new("Action", ExternalLogin.LoginCallbackAction)];

            var redirectUrl = UriHelper.BuildRelative(
                context.Request.PathBase,
                "/Account/ExternalLogin",
                QueryString.Create(query));

            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return TypedResults.Challenge(properties, [provider]);
        });

        accountGroup.MapPost("/Logout", async (
            HttpContext context,
            ClaimsPrincipal user,
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromForm] string returnUrl) =>
        {
            PasskeyReauthentication.Clear(context);
            await signInManager.SignOutAsync();
            return TypedResults.LocalRedirect($"~/{returnUrl}");
        });

        accountGroup.MapPost("/PasskeyRequestOptions", [RequireAntiforgeryToken] async (
            HttpContext context,
            [FromServices] UserManager<ApplicationUser> userManager,
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromQuery] string? username) =>
        {
            var antiforgeryValidationFeature = context.Features.Get<IAntiforgeryValidationFeature>();
            if (antiforgeryValidationFeature is not { IsValid: true })
            {
                return Results.BadRequest(antiforgeryValidationFeature?.Error?.Message ?? "Antiforgery validation failed.");
            }

            var user = string.IsNullOrEmpty(username) ? null : await userManager.FindByNameAsync(username);
            var optionsJson = await signInManager.MakePasskeyRequestOptionsAsync(user);
            return TypedResults.Content(optionsJson, contentType: "application/json");
        });

        // Creation options for a visitor who does not have an account yet. This is anonymous by
        // necessity, so it never touches an existing account.
        accountGroup.MapPost("/PasskeyRegistrationOptions", [RequireAntiforgeryToken] async (
            HttpContext context,
            [FromServices] UserManager<ApplicationUser> userManager,
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromQuery] string? username) =>
        {
            var antiforgeryValidationFeature = context.Features.Get<IAntiforgeryValidationFeature>();
            if (antiforgeryValidationFeature is not { IsValid: true })
            {
                return Results.BadRequest(antiforgeryValidationFeature?.Error?.Message ?? "Antiforgery validation failed.");
            }

            if (string.IsNullOrEmpty(username) || !new EmailAddressAttribute().IsValid(username))
            {
                return Results.Text("Enter a valid email address.", "text/plain", statusCode: StatusCodes.Status400BadRequest);
            }

            // The account does not exist yet, so the ceremony needs an ID up front. A new ApplicationUser
            // gets one from its constructor. Never reuse an existing account's ID here.
            var user = new ApplicationUser { UserName = username, Email = username };

            // Running the same validators that CreateAsync will run keeps the rejection rules from
            // drifting apart, so the user is not asked for a passkey that cannot be saved.
            foreach (var validator in userManager.UserValidators)
            {
                var result = await validator.ValidateAsync(userManager, user);
                if (!result.Succeeded)
                {
                    return Results.Text(result.Errors.First().Description, "text/plain", statusCode: StatusCodes.Status400BadRequest);
                }
            }

            var optionsJson = await signInManager.MakePasskeyCreationOptionsAsync(new()
            {
                Id = user.Id,
                Name = username,
                DisplayName = username,
            });
            return TypedResults.Content(optionsJson, contentType: "application/json");
        });

        var manageGroup = accountGroup.MapGroup("/Manage").RequireAuthorization();

        // Creation options are only handed out once the user has confirmed their identity with a
        // credential the account already has.
        manageGroup.MapPost("/PasskeyCreationOptions", [RequireAntiforgeryToken] async (
            HttpContext context,
            [FromServices] UserManager<ApplicationUser> userManager,
            [FromServices] SignInManager<ApplicationUser> signInManager) =>
        {
            var antiforgeryValidationFeature = context.Features.Get<IAntiforgeryValidationFeature>();
            if (antiforgeryValidationFeature is not { IsValid: true })
            {
                return Results.BadRequest(antiforgeryValidationFeature?.Error?.Message ?? "Antiforgery validation failed.");
            }

            var user = await userManager.GetUserAsync(context.User);
            if (user is null)
            {
                return Results.NotFound($"Unable to load user with ID '{userManager.GetUserId(context.User)}'.");
            }

            if (!await PasskeyReauthentication.IsVerifiedAsync(context, userManager, user))
            {
                return Results.BadRequest("You must confirm your identity before adding a passkey.");
            }

            var userId = await userManager.GetUserIdAsync(user);
            var userName = await userManager.GetUserNameAsync(user) ?? "User";
            var optionsJson = await signInManager.MakePasskeyCreationOptionsAsync(new()
            {
                Id = userId,
                Name = userName,
                DisplayName = userName
            });
            return TypedResults.Content(optionsJson, contentType: "application/json");
        });

        // Unlike /PasskeyRequestOptions, this never takes a username: the passkey being asserted
        // must belong to the account that is already signed in.
        manageGroup.MapPost("/PasskeyReauthenticationOptions", [RequireAntiforgeryToken] async (
            HttpContext context,
            [FromServices] UserManager<ApplicationUser> userManager,
            [FromServices] SignInManager<ApplicationUser> signInManager) =>
        {
            var antiforgeryValidationFeature = context.Features.Get<IAntiforgeryValidationFeature>();
            if (antiforgeryValidationFeature is not { IsValid: true })
            {
                return Results.BadRequest(antiforgeryValidationFeature?.Error?.Message ?? "Antiforgery validation failed.");
            }

            var user = await userManager.GetUserAsync(context.User);
            if (user is null)
            {
                return Results.NotFound($"Unable to load user with ID '{userManager.GetUserId(context.User)}'.");
            }

            var optionsJson = await signInManager.MakePasskeyRequestOptionsAsync(user);
            return TypedResults.Content(optionsJson, contentType: "application/json");
        });

        // Lets an account whose only credential is an external login confirm by being challenged again.
        manageGroup.MapPost("/ReauthenticateExternalLogin", [RequireAntiforgeryToken] async (
            HttpContext context,
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromForm] string provider,
            [FromForm] string returnUrl) =>
        {
            var antiforgeryValidationFeature = context.Features.Get<IAntiforgeryValidationFeature>();
            if (antiforgeryValidationFeature is not { IsValid: true })
            {
                return Results.BadRequest(antiforgeryValidationFeature?.Error?.Message ?? "Antiforgery validation failed.");
            }

            // The provider redirects back to this path once the challenge completes, so reject
            // anything that is not a path relative to the application root.
            if (!Uri.IsWellFormedUriString(returnUrl, UriKind.Relative) ||
                returnUrl.StartsWith('/') ||
                returnUrl.StartsWith('\\'))
            {
                return Results.BadRequest("The return URL must be relative to the application root.");
            }

            // Clear the existing external cookie to ensure a clean challenge
            await context.SignOutAsync(IdentityConstants.ExternalScheme);

            var redirectUrl = UriHelper.BuildRelative(
                context.Request.PathBase,
                $"/{returnUrl}",
                QueryString.Create("Action", ReauthenticationPrompt.ReauthenticationCallbackAction));

            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, signInManager.UserManager.GetUserId(context.User));
            return Results.Challenge(properties, [provider]);
        });

        manageGroup.MapPost("/LinkExternalLogin", async (
            HttpContext context,
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromForm] string provider) =>
        {
            // Clear the existing external cookie to ensure a clean login process
            await context.SignOutAsync(IdentityConstants.ExternalScheme);

            var redirectUrl = UriHelper.BuildRelative(
                context.Request.PathBase,
                "/Account/Manage/ExternalLogins",
                QueryString.Create("Action", ExternalLogins.LinkLoginCallbackAction));

            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, signInManager.UserManager.GetUserId(context.User));
            return TypedResults.Challenge(properties, [provider]);
        });

        var loggerFactory = endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var downloadLogger = loggerFactory.CreateLogger("DownloadPersonalData");

        manageGroup.MapPost("/DownloadPersonalData", [RequireAntiforgeryToken] async (
            HttpContext context,
            [FromServices] UserManager<ApplicationUser> userManager,
            [FromServices] AuthenticationStateProvider authenticationStateProvider) =>
        {
            var antiforgeryValidationFeature = context.Features.Get<IAntiforgeryValidationFeature>();
            if (antiforgeryValidationFeature is not { IsValid: true })
            {
                return Results.BadRequest(antiforgeryValidationFeature?.Error?.Message ?? "Antiforgery validation failed.");
            }

            var user = await userManager.GetUserAsync(context.User);
            if (user is null)
            {
                return Results.NotFound($"Unable to load user with ID '{userManager.GetUserId(context.User)}'.");
            }

            var userId = await userManager.GetUserIdAsync(user);
            downloadLogger.LogInformation("User with ID '{UserId}' asked for their personal data.", userId);

            // Only include personal data for download
            var personalData = new Dictionary<string, string>();
            var personalDataProps = typeof(ApplicationUser).GetProperties().Where(
                prop => Attribute.IsDefined(prop, typeof(PersonalDataAttribute)));
            foreach (var p in personalDataProps)
            {
                personalData.Add(p.Name, p.GetValue(user)?.ToString() ?? "null");
            }

            var logins = await userManager.GetLoginsAsync(user);
            foreach (var l in logins)
            {
                personalData.Add($"{l.LoginProvider} external login provider key", l.ProviderKey);
            }

            personalData.Add("Authenticator Key", (await userManager.GetAuthenticatorKeyAsync(user))!);
            var fileBytes = JsonSerializer.SerializeToUtf8Bytes(personalData);

            context.Response.Headers.TryAdd("Content-Disposition", "attachment; filename=PersonalData.json");
            return TypedResults.File(fileBytes, contentType: "application/json", fileDownloadName: "PersonalData.json");
        });

        return accountGroup;
    }
}
