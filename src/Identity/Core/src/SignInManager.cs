// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Provides the APIs for user sign in.
/// </summary>
/// <typeparam name="TUser">The type encapsulating a user.</typeparam>
public class SignInManager<TUser> where TUser : class
{
    private const string LoginProviderKey = "LoginProvider";
    private const string XsrfKey = "XsrfId";
    private const string PasskeyOperationKey = "PasskeyOperation";
    private const string PasskeyStateKey = "PasskeyState";

    private readonly IHttpContextAccessor _contextAccessor;
    private readonly IAuthenticationSchemeProvider _schemes;
    private readonly IUserConfirmation<TUser> _confirmation;
    private readonly IPasskeyHandler<TUser>? _passkeyHandler;
    private readonly SignInManagerMetrics? _metrics;
    private HttpContext? _context;
    private TwoFactorAuthenticationInfo? _twoFactorInfo;
    private PasskeyAuthenticationInfo? _passkeyInfo;

    /// <summary>
    /// Creates a new instance of <see cref="SignInManager{TUser}"/>.
    /// </summary>
    /// <param name="userManager">An instance of <see cref="UserManager"/> used to retrieve users from and persist users.</param>
    /// <param name="contextAccessor">The accessor used to access the <see cref="HttpContext"/>.</param>
    /// <param name="claimsFactory">The factory to use to create claims principals for a user.</param>
    /// <param name="optionsAccessor">The accessor used to access the <see cref="IdentityOptions"/>.</param>
    /// <param name="logger">The logger used to log messages, warnings and errors.</param>
    /// <param name="schemes">The scheme provider that is used enumerate the authentication schemes.</param>
    /// <param name="confirmation">The <see cref="IUserConfirmation{TUser}"/> used check whether a user account is confirmed.</param>
    public SignInManager(UserManager<TUser> userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<TUser> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<TUser>> logger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<TUser> confirmation)
    {
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(contextAccessor);
        ArgumentNullException.ThrowIfNull(claimsFactory);

        UserManager = userManager;
        _contextAccessor = contextAccessor;
        ClaimsFactory = claimsFactory;
        Options = optionsAccessor?.Value ?? new IdentityOptions();
        Logger = logger;
        _schemes = schemes;
        _confirmation = confirmation;
        // SignInManagerMetrics created from constructor because of difficulties registering internal type.
        _metrics = userManager.ServiceProvider?.GetService<IMeterFactory>() is { } factory ? new SignInManagerMetrics(factory) : null;
        _passkeyHandler = userManager.ServiceProvider?.GetService<IPasskeyHandler<TUser>>();
    }

    /// <summary>
    /// Gets the <see cref="ILogger"/> used to log messages from the manager.
    /// </summary>
    /// <value>
    /// The <see cref="ILogger"/> used to log messages from the manager.
    /// </value>
    public virtual ILogger Logger { get; set; }

    /// <summary>
    /// The <see cref="UserManager{TUser}"/> used.
    /// </summary>
    public UserManager<TUser> UserManager { get; set; }

    /// <summary>
    /// The <see cref="IUserClaimsPrincipalFactory{TUser}"/> used.
    /// </summary>
    public IUserClaimsPrincipalFactory<TUser> ClaimsFactory { get; set; }

    /// <summary>
    /// The <see cref="IdentityOptions"/> used.
    /// </summary>
    public IdentityOptions Options { get; set; }

    /// <summary>
    /// The authentication scheme to sign in with. Defaults to <see cref="IdentityConstants.ApplicationScheme"/>.
    /// </summary>
    public string AuthenticationScheme { get; set; } = IdentityConstants.ApplicationScheme;

    /// <summary>
    /// The <see cref="HttpContext"/> used.
    /// </summary>
    public HttpContext Context
    {
        get
        {
            var context = _context ?? _contextAccessor?.HttpContext;
            if (context == null)
            {
                throw new InvalidOperationException("HttpContext must not be null.");
            }
            return context;
        }
        set
        {
            _context = value;
        }
    }

    /// <summary>
    /// Creates a <see cref="ClaimsPrincipal"/> for the specified <paramref name="user"/>, as an asynchronous operation.
    /// </summary>
    /// <param name="user">The user to create a <see cref="ClaimsPrincipal"/> for.</param>
    /// <returns>The task object representing the asynchronous operation, containing the ClaimsPrincipal for the specified user.</returns>
    public virtual async Task<ClaimsPrincipal> CreateUserPrincipalAsync(TUser user) => await ClaimsFactory.CreateAsync(user);

    /// <summary>
    /// Returns true if the principal has an identity with the application cookie identity
    /// </summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/> instance.</param>
    /// <returns>True if the user is logged in with identity.</returns>
    public virtual bool IsSignedIn(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        return principal.Identities != null &&
            principal.Identities.Any(i => i.AuthenticationType == AuthenticationScheme);
    }

    /// <summary>
    /// Returns a flag indicating whether the specified user can sign in.
    /// </summary>
    /// <param name="user">The user whose sign-in status should be returned.</param>
    /// <returns>
    /// The task object representing the asynchronous operation, containing a flag that is true
    /// if the specified user can sign-in, otherwise false.
    /// </returns>
    public virtual async Task<bool> CanSignInAsync(TUser user)
    {
        if (Options.SignIn.RequireConfirmedEmail && !(await UserManager.IsEmailConfirmedAsync(user)))
        {
            Logger.LogDebug(EventIds.UserCannotSignInWithoutConfirmedEmail, "User cannot sign in without a confirmed email.");
            return false;
        }
        if (Options.SignIn.RequireConfirmedPhoneNumber && !(await UserManager.IsPhoneNumberConfirmedAsync(user)))
        {
            Logger.LogDebug(EventIds.UserCannotSignInWithoutConfirmedPhoneNumber, "User cannot sign in without a confirmed phone number.");
            return false;
        }
        if (Options.SignIn.RequireConfirmedAccount && !(await _confirmation.IsConfirmedAsync(UserManager, user)))
        {
            Logger.LogDebug(EventIds.UserCannotSignInWithoutConfirmedAccount, "User cannot sign in without a confirmed account.");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Signs in the specified <paramref name="user"/>, whilst preserving the existing
    /// AuthenticationProperties of the current signed-in user like rememberMe, as an asynchronous operation.
    /// </summary>
    /// <param name="user">The user to sign-in.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    public virtual async Task RefreshSignInAsync(TUser user)
    {
        var startTimestamp = Stopwatch.GetTimestamp();
        try
        {
            var (success, isPersistent) = await RefreshSignInCoreAsync(user);
            var signInResult = success ? SignInResult.Success : SignInResult.Failed;
            _metrics?.AuthenticateSignIn(typeof(TUser).FullName!, AuthenticationScheme, signInResult, SignInType.Refresh, isPersistent, startTimestamp);
        }
        catch (Exception ex)
        {
            _metrics?.AuthenticateSignIn(typeof(TUser).FullName!, AuthenticationScheme, result: null, SignInType.Refresh, isPersistent: null, startTimestamp, ex);
            throw;
        }
    }

    private async Task<(bool success, bool? isPersistent)> RefreshSignInCoreAsync(TUser user)
    {
        var auth = await Context.AuthenticateAsync(AuthenticationScheme);
        if (!auth.Succeeded || auth.Principal?.Identity?.IsAuthenticated != true)
        {
            Logger.LogError("RefreshSignInAsync prevented because the user is not currently authenticated. Use SignInAsync instead for initial sign in.");
            return (false, auth.Properties?.IsPersistent);
        }

        var authenticatedUserId = UserManager.GetUserId(auth.Principal);
        var newUserId = await UserManager.GetUserIdAsync(user);
        if (authenticatedUserId == null || authenticatedUserId != newUserId)
        {
            Logger.LogError("RefreshSignInAsync prevented because currently authenticated user has a different UserId. Use SignInAsync instead to change users.");
            return (false, auth.Properties?.IsPersistent);
        }

        IList<Claim> claims = Array.Empty<Claim>();
        var authenticationMethod = auth.Principal?.FindFirst(ClaimTypes.AuthenticationMethod);
        var amr = auth.Principal?.FindFirst("amr");

        if (authenticationMethod != null || amr != null)
        {
            claims = new List<Claim>();
            if (authenticationMethod != null)
            {
                claims.Add(authenticationMethod);
            }
            if (amr != null)
            {
                claims.Add(amr);
            }
        }

        await SignInWithClaimsAsync(user, auth.Properties, claims);
        return (true, auth.Properties?.IsPersistent ?? false);
    }

    /// <summary>
    /// Signs in the specified <paramref name="user"/>.
    /// </summary>
    /// <param name="user">The user to sign-in.</param>
    /// <param name="isPersistent">Flag indicating whether the sign-in cookie should persist after the browser is closed.</param>
    /// <param name="authenticationMethod">Name of the method used to authenticate the user.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required for backwards compatibility")]
    public virtual Task SignInAsync(TUser user, bool isPersistent, string? authenticationMethod = null)
        => SignInAsync(user, new AuthenticationProperties { IsPersistent = isPersistent }, authenticationMethod);

    /// <summary>
    /// Signs in the specified <paramref name="user"/>.
    /// </summary>
    /// <param name="user">The user to sign-in.</param>
    /// <param name="authenticationProperties">Properties applied to the login and authentication cookie.</param>
    /// <param name="authenticationMethod">Name of the method used to authenticate the user.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required for backwards compatibility")]
    public virtual Task SignInAsync(TUser user, AuthenticationProperties authenticationProperties, string? authenticationMethod = null)
    {
        IList<Claim> additionalClaims = Array.Empty<Claim>();
        if (authenticationMethod != null)
        {
            additionalClaims = new List<Claim>();
            additionalClaims.Add(new Claim(ClaimTypes.AuthenticationMethod, authenticationMethod));
        }
        return SignInWithClaimsAsync(user, authenticationProperties, additionalClaims);
    }

    /// <summary>
    /// Signs in the specified <paramref name="user"/>.
    /// </summary>
    /// <param name="user">The user to sign-in.</param>
    /// <param name="isPersistent">Flag indicating whether the sign-in cookie should persist after the browser is closed.</param>
    /// <param name="additionalClaims">Additional claims that will be stored in the cookie.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    public virtual Task SignInWithClaimsAsync(TUser user, bool isPersistent, IEnumerable<Claim> additionalClaims)
        => SignInWithClaimsAsync(user, new AuthenticationProperties { IsPersistent = isPersistent }, additionalClaims);

    /// <summary>
    /// Signs in the specified <paramref name="user"/>.
    /// </summary>
    /// <param name="user">The user to sign-in.</param>
    /// <param name="authenticationProperties">Properties applied to the login and authentication cookie.</param>
    /// <param name="additionalClaims">Additional claims that will be stored in the cookie.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    public virtual async Task SignInWithClaimsAsync(TUser user, AuthenticationProperties? authenticationProperties, IEnumerable<Claim> additionalClaims)
    {
        try
        {
            var userPrincipal = await CreateUserPrincipalAsync(user);
            foreach (var claim in additionalClaims)
            {
                userPrincipal.Identities.First().AddClaim(claim);
            }

            authenticationProperties ??= new AuthenticationProperties();
            await Context.SignInAsync(AuthenticationScheme,
                userPrincipal,
                authenticationProperties);

            // This is useful for updating claims immediately when hitting MapIdentityApi's /account/info endpoint with cookies.
            Context.User = userPrincipal;

            _metrics?.SignInUserPrincipal(typeof(TUser).FullName!, AuthenticationScheme, authenticationProperties.IsPersistent);
        }
        catch (Exception ex)
        {
            _metrics?.SignInUserPrincipal(typeof(TUser).FullName!, AuthenticationScheme, isPersistent: null, ex);
            throw;
        }
    }

    /// <summary>
    /// Signs the current user out of the application.
    /// </summary>
    public virtual async Task SignOutAsync()
    {
        try
        {
            await Context.SignOutAsync(AuthenticationScheme);

            if (await _schemes.GetSchemeAsync(IdentityConstants.ExternalScheme) != null)
            {
                await Context.SignOutAsync(IdentityConstants.ExternalScheme);
            }
            if (await _schemes.GetSchemeAsync(IdentityConstants.TwoFactorUserIdScheme) != null)
            {
                await Context.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme);
            }

            _metrics?.SignOutUserPrincipal(typeof(TUser).FullName!, AuthenticationScheme);
        }
        catch (Exception ex)
        {
            _metrics?.SignOutUserPrincipal(typeof(TUser).FullName!, AuthenticationScheme, ex);
            throw;
        }
    }

    /// <summary>
    /// Validates the security stamp for the specified <paramref name="principal"/> against
    /// the persisted stamp for the current user, as an asynchronous operation.
    /// </summary>
    /// <param name="principal">The principal whose stamp should be validated.</param>
    /// <returns>The task object representing the asynchronous operation. The task will contain the <typeparamref name="TUser"/>
    /// if the stamp matches the persisted value, otherwise it will return null.</returns>
    public virtual async Task<TUser?> ValidateSecurityStampAsync(ClaimsPrincipal? principal)
    {
        if (principal == null)
        {
            return null;
        }
        var user = await UserManager.GetUserAsync(principal);
        if (await ValidateSecurityStampAsync(user, principal.FindFirstValue(Options.ClaimsIdentity.SecurityStampClaimType)))
        {
            return user;
        }
        Logger.LogDebug(EventIds.SecurityStampValidationFailedId4, "Failed to validate a security stamp.");
        return null;
    }

    /// <summary>
    /// Validates the security stamp for the specified <paramref name="principal"/> from one of
    /// the two factor principals (remember client or user id) against
    /// the persisted stamp for the current user, as an asynchronous operation.
    /// </summary>
    /// <param name="principal">The principal whose stamp should be validated.</param>
    /// <returns>The task object representing the asynchronous operation. The task will contain the <typeparamref name="TUser"/>
    /// if the stamp matches the persisted value, otherwise it will return null.</returns>
    public virtual async Task<TUser?> ValidateTwoFactorSecurityStampAsync(ClaimsPrincipal? principal)
    {
        if (principal == null || principal.Identity?.Name == null)
        {
            return null;
        }
        var user = await UserManager.FindByIdAsync(principal.Identity.Name);
        if (await ValidateSecurityStampAsync(user, principal.FindFirstValue(Options.ClaimsIdentity.SecurityStampClaimType)))
        {
            return user;
        }
        Logger.LogDebug(EventIds.TwoFactorSecurityStampValidationFailed, "Failed to validate a security stamp.");
        return null;
    }

    /// <summary>
    /// Validates the security stamp for the specified <paramref name="user"/>.  If no user is specified, or if the store
    /// does not support security stamps, validation is considered successful.
    /// </summary>
    /// <param name="user">The user whose stamp should be validated.</param>
    /// <param name="securityStamp">The expected security stamp value.</param>
    /// <returns>The result of the validation.</returns>
    public virtual async Task<bool> ValidateSecurityStampAsync(TUser? user, string? securityStamp)
        => user != null &&
        // Only validate the security stamp if the store supports it
        (!UserManager.SupportsUserSecurityStamp || securityStamp == await UserManager.GetSecurityStampAsync(user));

    /// <summary>
    /// Attempts to sign in the specified <paramref name="user"/> and <paramref name="password"/> combination
    /// as an asynchronous operation.
    /// </summary>
    /// <param name="user">The user to sign in.</param>
    /// <param name="password">The password to attempt to sign in with.</param>
    /// <param name="isPersistent">Flag indicating whether the sign-in cookie should persist after the browser is closed.</param>
    /// <param name="lockoutOnFailure">Flag indicating if the user account should be locked if the sign in fails.</param>
    /// <returns>The task object representing the asynchronous operation containing the <see cref="SignInResult"/>
    /// for the sign-in attempt.</returns>
    public virtual async Task<SignInResult> PasswordSignInAsync(TUser user, string password,
        bool isPersistent, bool lockoutOnFailure)
    {
        var startTimestamp = Stopwatch.GetTimestamp();
        try
        {
            ArgumentNullException.ThrowIfNull(user);

            var attempt = await CheckPasswordSignInAsync(user, password, lockoutOnFailure);
            var result = attempt.Succeeded
                ? await SignInOrTwoFactorAsync(user, isPersistent)
                : attempt;
            _metrics?.AuthenticateSignIn(typeof(TUser).FullName!, AuthenticationScheme, result, SignInType.Password, isPersistent, startTimestamp);

            return result;
        }
        catch (Exception ex)
        {
            _metrics?.AuthenticateSignIn(typeof(TUser).FullName!, AuthenticationScheme, result: null, SignInType.Password, isPersistent, startTimestamp, ex);
            throw;
        }
    }

    /// <summary>
    /// Attempts to sign in the specified <paramref name="userName"/> and <paramref name="password"/> combination
    /// as an asynchronous operation.
    /// </summary>
    /// <param name="userName">The user name to sign in.</param>
    /// <param name="password">The password to attempt to sign in with.</param>
    /// <param name="isPersistent">Flag indicating whether the sign-in cookie should persist after the browser is closed.</param>
    /// <param name="lockoutOnFailure">Flag indicating if the user account should be locked if the sign in fails.</param>
    /// <returns>The task object representing the asynchronous operation containing the <see cref="SignInResult"/>
    /// for the sign-in attempt.</returns>
    public virtual async Task<SignInResult> PasswordSignInAsync(string userName, string password,
        bool isPersistent, bool lockoutOnFailure)
    {
        var startTimestamp = Stopwatch.GetTimestamp();
        var user = await UserManager.FindByNameAsync(userName);
        if (user == null)
        {
            _metrics?.AuthenticateSignIn(typeof(TUser).FullName!, AuthenticationScheme, SignInResult.Failed, SignInType.Password, isPersistent, startTimestamp);
            return SignInResult.Failed;
        }

        return await PasswordSignInAsync(user, password, isPersistent, lockoutOnFailure);
    }

    /// <summary>
    /// Attempts a password sign in for a user.
    /// </summary>
    /// <param name="user">The user to sign in.</param>
    /// <param name="password">The password to attempt to sign in with.</param>
    /// <param name="lockoutOnFailure">Flag indicating if the user account should be locked if the sign in fails.</param>
    /// <returns>The task object representing the asynchronous operation containing the <see cref="SignInResult"/>
    /// for the sign-in attempt.</returns>
    public virtual async Task<SignInResult> CheckPasswordSignInAsync(TUser user, string password, bool lockoutOnFailure)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(user);

            var result = await CheckPasswordSignInCoreAsync(user, password, lockoutOnFailure);
            _metrics?.CheckPasswordSignIn(typeof(TUser).FullName!, result);

            return result;
        }
        catch (Exception ex)
        {
            _metrics?.CheckPasswordSignIn(typeof(TUser).FullName!, result: null, ex);
            throw;
        }
    }

    private async Task<SignInResult> CheckPasswordSignInCoreAsync(TUser user, string password, bool lockoutOnFailure)
    {
        var error = await PreSignInCheck(user);
        if (error != null)
        {
            return error;
        }

        if (await UserManager.CheckPasswordAsync(user, password))
        {
            var alwaysLockout = AppContext.TryGetSwitch("Microsoft.AspNetCore.Identity.CheckPasswordSignInAlwaysResetLockoutOnSuccess", out var enabled) && enabled;
            // Only reset the lockout when not in quirks mode if either TFA is not enabled or the client is remembered for TFA.
            if (alwaysLockout || !await IsTwoFactorEnabledAsync(user) || await IsTwoFactorClientRememberedAsync(user))
            {
                var resetLockoutResult = await ResetLockoutWithResult(user);
                if (!resetLockoutResult.Succeeded)
                {
                    // ResetLockout got an unsuccessful result that could be caused by concurrency failures indicating an
                    // attacker could be trying to bypass the MaxFailedAccessAttempts limit. Return the same failure we do
                    // when failing to increment the lockout to avoid giving an attacker extra guesses at the password.
                    return SignInResult.Failed;
                }
            }

            return SignInResult.Success;
        }
        Logger.LogDebug(EventIds.InvalidPassword, "User failed to provide the correct password.");

        if (UserManager.SupportsUserLockout && lockoutOnFailure)
        {
            // If lockout is requested, increment access failed count which might lock out the user
            var incrementLockoutResult = await UserManager.AccessFailedAsync(user) ?? IdentityResult.Success;
            if (!incrementLockoutResult.Succeeded)
            {
                // Return the same failure we do when resetting the lockout fails after a correct password.
                return SignInResult.Failed;
            }

            if (await UserManager.IsLockedOutAsync(user))
            {
                return await LockedOut(user);
            }
        }
        return SignInResult.Failed;
    }

    /// <summary>
    /// Generates passkey creation options for the specified <paramref name="userEntity"/>.
    /// </summary>
    /// <param name="userEntity">The user entity for which to create passkey options.</param>
    /// <returns>A JSON string representing the created passkey options.</returns>
    public virtual async Task<string> MakePasskeyCreationOptionsAsync(PasskeyUserEntity userEntity)
    {
        ThrowIfNoPasskeyHandler();
        ArgumentNullException.ThrowIfNull(userEntity);

        var result = await _passkeyHandler.MakeCreationOptionsAsync(userEntity, Context);
        await StorePasskeyAuthenticationInfoAsync(PasskeyOperations.Attestation, result.AttestationState);
        return result.CreationOptionsJson;
    }

    /// <summary>
    /// Creates passkey assertion options for the specified <paramref name="user"/>.
    /// </summary>
    /// <param name="user">The user for whom to create passkey assertion options.</param>
    /// <returns>A JSON string representing the created passkey assertion options.</returns>
    public virtual async Task<string> MakePasskeyRequestOptionsAsync(TUser? user)
    {
        ThrowIfNoPasskeyHandler();

        var result = await _passkeyHandler.MakeRequestOptionsAsync(user, Context);
        await StorePasskeyAuthenticationInfoAsync(PasskeyOperations.Assertion, result.AssertionState);
        return result.RequestOptionsJson;
    }

    /// <summary>
    /// Performs passkey attestation for the given <paramref name="credentialJson"/>.
    /// </summary>
    /// <remarks>
    /// The <paramref name="credentialJson"/> should be obtained by JSON-serializing the result of the
    /// <c>navigator.credentials.create()</c> JavaScript API. The argument to <c>navigator.credentials.create()</c>
    /// should be obtained by calling <see cref="MakePasskeyCreationOptionsAsync(PasskeyUserEntity)"/>.
    /// </remarks>
    /// <param name="credentialJson">The credentials obtained by JSON-serializing the result of the <c>navigator.credentials.create()</c> JavaScript function.</param>
    /// <returns>
    /// A task object representing the asynchronous operation containing the <see cref="PasskeyAttestationResult"/>.
    /// </returns>
    public virtual async Task<PasskeyAttestationResult> PerformPasskeyAttestationAsync(string credentialJson)
    {
        ThrowIfNoPasskeyHandler();
        ArgumentException.ThrowIfNullOrEmpty(credentialJson);

        var passkeyInfo = await RetrievePasskeyAuthenticationInfoAsync()
            ?? throw new InvalidOperationException(
                "No passkey attestation is underway. " +
                $"Make sure to call '{nameof(SignInManager<>)}.{nameof(MakePasskeyCreationOptionsAsync)}()' to initiate a passkey attestation.");
        if (!string.Equals(PasskeyOperations.Attestation, passkeyInfo.Operation, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Expected passkey operation '{PasskeyOperations.Attestation}', but got '{passkeyInfo.Operation}'. " +
                $"This may indicate that you have not previously called '{nameof(SignInManager<>)}.{nameof(MakePasskeyCreationOptionsAsync)}()'.");
        }
        var context = new PasskeyAttestationContext
        {
            CredentialJson = credentialJson,
            AttestationState = passkeyInfo.State,
            HttpContext = Context,
        };
        var result = await _passkeyHandler.PerformAttestationAsync(context);
        if (!result.Succeeded)
        {
            Logger.LogDebug(EventIds.PasskeyAttestationFailed, "Passkey attestation failed: {message}", result.Failure.Message);
        }

        return result;
    }

    /// <summary>
    /// Performs passkey assertion for the given <paramref name="credentialJson"/>.
    /// </summary>
    /// <remarks>
    /// The <paramref name="credentialJson"/> should be obtained by JSON-serializing the result of the
    /// <c>navigator.credentials.get()</c> JavaScript API. The argument to <c>navigator.credentials.get()</c>
    /// should be obtained by calling <see cref="MakePasskeyRequestOptionsAsync(TUser)"/>.
    /// Upon success, the <see cref="PasskeyAssertionResult{TUser}.Passkey"/> should be stored on the
    /// <see cref="PasskeyAssertionResult{TUser}.User"/> using <see cref="UserManager{TUser}.AddOrUpdatePasskeyAsync(TUser, UserPasskeyInfo)"/>.
    /// </remarks>
    /// <param name="credentialJson">The credentials obtained by JSON-serializing the result of the <c>navigator.credentials.get()</c> JavaScript function.</param>
    /// <returns>
    /// A task object representing the asynchronous operation containing the <see cref="PasskeyAssertionResult{TUser}"/>.
    /// </returns>
    public virtual async Task<PasskeyAssertionResult<TUser>> PerformPasskeyAssertionAsync(string credentialJson)
    {
        ThrowIfNoPasskeyHandler();
        ArgumentException.ThrowIfNullOrEmpty(credentialJson);

        var passkeyInfo = await RetrievePasskeyAuthenticationInfoAsync()
            ?? throw new InvalidOperationException(
                "No passkey assertion is underway. " +
                $"Make sure to call '{nameof(SignInManager<>)}.{nameof(MakePasskeyRequestOptionsAsync)}()' to initiate a passkey assertion.");
        if (!string.Equals(PasskeyOperations.Assertion, passkeyInfo.Operation, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Expected passkey operation '{PasskeyOperations.Assertion}', but got '{passkeyInfo.Operation}'. " +
                $"This may indicate that you have not previously called '{nameof(SignInManager<>)}.{nameof(MakePasskeyRequestOptionsAsync)}()'.");
        }
        var context = new PasskeyAssertionContext
        {
            CredentialJson = credentialJson,
            AssertionState = passkeyInfo.State,
            HttpContext = Context,
        };
        var result = await _passkeyHandler.PerformAssertionAsync(context);
        if (!result.Succeeded)
        {
            Logger.LogDebug(EventIds.PasskeyAssertionFailed, "Passkey assertion failed: {message}", result.Failure.Message);
        }

        return result;
    }

    /// <summary>
    /// Performs a passkey assertion and attempts to sign in the user.
    /// </summary>
    /// <remarks>
    /// The <paramref name="credentialJson"/> should be obtained by JSON-serializing the result of the
    /// <c>navigator.credentials.get()</c> JavaScript API. The argument to <c>navigator.credentials.get()</c>
    /// should be obtained by calling <see cref="MakePasskeyRequestOptionsAsync(TUser)"/>.
    /// </remarks>
    /// <param name="credentialJson">The credentials obtained by JSON-serializing the result of the <c>navigator.credentials.get()</c> JavaScript function.</param>
    /// <returns>
    /// The task object representing the asynchronous operation containing the <see cref="SignInResult"/>
    /// for the sign-in attempt.
    /// </returns>
    public virtual async Task<SignInResult> PasskeySignInAsync(string credentialJson)
    {
        var startTimestamp = Stopwatch.GetTimestamp();
        try
        {
            var result = await PasskeySignInCoreAsync(credentialJson);
            _metrics?.AuthenticateSignIn(typeof(TUser).FullName!, AuthenticationScheme, result, SignInType.Passkey, isPersistent: false, startTimestamp);

            return result;
        }
        catch (Exception ex)
        {
            _metrics?.AuthenticateSignIn(typeof(TUser).FullName!, AuthenticationScheme, result: null, SignInType.Passkey, isPersistent: false, startTimestamp, ex);
            throw;
        }
    }

    private async Task<SignInResult> PasskeySignInCoreAsync(string credentialJson)
    {
        ArgumentException.ThrowIfNullOrEmpty(credentialJson);

        var assertionResult = await PerformPasskeyAssertionAsync(credentialJson);
        if (!assertionResult.Succeeded)
        {
            return SignInResult.Failed;
        }

        // After a successful assertion, we need to update the passkey so that it has the latest
        // sign count and authenticator data.
        var setPasskeyResult = await UserManager.AddOrUpdatePasskeyAsync(assertionResult.User, assertionResult.Passkey);
        if (!setPasskeyResult.Succeeded)
        {
            return SignInResult.Failed;
        }

        return await SignInOrTwoFactorAsync(assertionResult.User, isPersistent: false, bypassTwoFactor: true);
    }

    [MemberNotNull(nameof(_passkeyHandler))]
    private void ThrowIfNoPasskeyHandler()
    {
        if (_passkeyHandler is null)
        {
            throw new InvalidOperationException(
                $"This operation requires an {nameof(IPasskeyHandler<>)} service to be registered.");
        }
    }

    private async Task StorePasskeyAuthenticationInfoAsync(string operation, string? state)
    {
        var props = new AuthenticationProperties();
        props.Items[PasskeyOperationKey] = operation;
        props.Items[PasskeyStateKey] = state;
        var claimsIdentity = new ClaimsIdentity(IdentityConstants.TwoFactorUserIdScheme);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
        await Context.SignInAsync(IdentityConstants.TwoFactorUserIdScheme, claimsPrincipal, props);
    }

    private async Task<PasskeyAuthenticationInfo?> RetrievePasskeyAuthenticationInfoAsync()
    {
        return _passkeyInfo ??= await RetrievePasskeyInfoCoreAsync();

        async Task<PasskeyAuthenticationInfo?> RetrievePasskeyInfoCoreAsync()
        {
            var result = await Context.AuthenticateAsync(IdentityConstants.TwoFactorUserIdScheme);
            await Context.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme);

            if (result.Properties is not { } properties)
            {
                return null;
            }

            if (!properties.Items.TryGetValue(PasskeyOperationKey, out var operation) ||
                !properties.Items.TryGetValue(PasskeyStateKey, out var state))
            {
                return null;
            }

            return new()
            {
                Operation = operation,
                State = state,
            };
        }
    }

    /// <summary>
    /// Returns a flag indicating if the current client browser has been remembered by two factor authentication
    /// for the user attempting to login, as an asynchronous operation.
    /// </summary>
    /// <param name="user">The user attempting to login.</param>
    /// <returns>
    /// The task object representing the asynchronous operation containing true if the browser has been remembered
    /// for the current user.
    /// </returns>
    public virtual async Task<bool> IsTwoFactorClientRememberedAsync(TUser user)
    {
        if (await _schemes.GetSchemeAsync(IdentityConstants.TwoFactorRememberMeScheme) == null)
        {
            return false;
        }

        var userId = await UserManager.GetUserIdAsync(user);
        var result = await Context.AuthenticateAsync(IdentityConstants.TwoFactorRememberMeScheme);
        return (result?.Principal != null && result.Principal.FindFirstValue(ClaimTypes.Name) == userId);
    }

    /// <summary>
    /// Sets a flag on the browser to indicate the user has selected "Remember this browser" for two factor authentication purposes,
    /// as an asynchronous operation.
    /// </summary>
    /// <param name="user">The user who choose "remember this browser".</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    public virtual async Task RememberTwoFactorClientAsync(TUser user)
    {
        try
        {
            var principal = await StoreRememberClient(user);
            await Context.SignInAsync(IdentityConstants.TwoFactorRememberMeScheme,
                principal,
                new AuthenticationProperties { IsPersistent = true });
            _metrics?.RememberTwoFactorClient(typeof(TUser).FullName!, IdentityConstants.TwoFactorRememberMeScheme);
        }
        catch (Exception ex)
        {
            _metrics?.RememberTwoFactorClient(typeof(TUser).FullName!, IdentityConstants.TwoFactorRememberMeScheme, ex);
            throw;
        }
    }

    /// <summary>
    /// Clears the "Remember this browser flag" from the current browser, as an asynchronous operation.
    /// </summary>
    /// <returns>The task object representing the asynchronous operation.</returns>
    public virtual async Task ForgetTwoFactorClientAsync()
    {
        try
        {
            await Context.SignOutAsync(IdentityConstants.TwoFactorRememberMeScheme);
            _metrics?.ForgetTwoFactorClient(typeof(TUser).FullName!, IdentityConstants.TwoFactorRememberMeScheme);
        }
        catch (Exception ex)
        {
            _metrics?.ForgetTwoFactorClient(typeof(TUser).FullName!, IdentityConstants.TwoFactorRememberMeScheme, ex);
            throw;
        }
    }

    /// <summary>
    /// Signs in the user without two factor authentication using a two factor recovery code.
    /// </summary>
    /// <param name="recoveryCode">The two factor recovery code.</param>
    /// <returns></returns>
    public virtual async Task<SignInResult> TwoFactorRecoveryCodeSignInAsync(string recoveryCode)
    {
        var startTimestamp = Stopwatch.GetTimestamp();
        try
        {
            var result = await TwoFactorRecoveryCodeSignInCoreAsync(recoveryCode);
            _metrics?.AuthenticateSignIn(typeof(TUser).FullName!, AuthenticationScheme, result, SignInType.TwoFactorRecoveryCode, isPersistent: false, startTimestamp);

            return result;
        }
        catch (Exception ex)
        {
            _metrics?.AuthenticateSignIn(typeof(TUser).FullName!, AuthenticationScheme, result: null, SignInType.TwoFactorRecoveryCode, isPersistent: false, startTimestamp, ex);
            throw;
        }
    }

    private async Task<SignInResult> TwoFactorRecoveryCodeSignInCoreAsync(string recoveryCode)
    {
        var twoFactorInfo = await RetrieveTwoFactorInfoAsync();
        if (twoFactorInfo == null)
        {
            return SignInResult.Failed;
        }

        var result = await UserManager.RedeemTwoFactorRecoveryCodeAsync(twoFactorInfo.User, recoveryCode);
        if (result.Succeeded)
        {
            return await DoTwoFactorSignInAsync(twoFactorInfo.User, twoFactorInfo, isPersistent: false, rememberClient: false);
        }

        // We don't protect against brute force attacks since codes are expected to be random.
        return SignInResult.Failed;
    }

    private async Task<SignInResult> DoTwoFactorSignInAsync(TUser user, TwoFactorAuthenticationInfo twoFactorInfo, bool isPersistent, bool rememberClient)
    {
        var resetLockoutResult = await ResetLockoutWithResult(user);
        if (!resetLockoutResult.Succeeded)
        {
            // ResetLockout got an unsuccessful result that could be caused by concurrency failures indicating an
            // attacker could be trying to bypass the MaxFailedAccessAttempts limit. Return the same failure we do
            // when failing to increment the lockout to avoid giving an attacker extra guesses at the two factor code.
            return SignInResult.Failed;
        }

        var claims = new List<Claim>
        {
            new Claim("amr", "mfa")
        };

        if (twoFactorInfo.LoginProvider != null)
        {
            claims.Add(new Claim(ClaimTypes.AuthenticationMethod, twoFactorInfo.LoginProvider));
        }
        // Cleanup external cookie
        if (await _schemes.GetSchemeAsync(IdentityConstants.ExternalScheme) != null)
        {
            await Context.SignOutAsync(IdentityConstants.ExternalScheme);
        }
        // Cleanup two factor user id cookie
        if (await _schemes.GetSchemeAsync(IdentityConstants.TwoFactorUserIdScheme) != null)
        {
            await Context.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme);
            if (rememberClient)
            {
                await RememberTwoFactorClientAsync(user);
            }
        }
        await SignInWithClaimsAsync(user, isPersistent, claims);
        return SignInResult.Success;
    }

    /// <summary>
    /// Validates the sign in code from an authenticator app and creates and signs in the user, as an asynchronous operation.
    /// </summary>
    /// <param name="code">The two factor authentication code to validate.</param>
    /// <param name="isPersistent">Flag indicating whether the sign-in cookie should persist after the browser is closed.</param>
    /// <param name="rememberClient">Flag indicating whether the current browser should be remember, suppressing all further
    /// two factor authentication prompts.</param>
    /// <returns>The task object representing the asynchronous operation containing the <see cref="SignInResult"/>
    /// for the sign-in attempt.</returns>
    public virtual async Task<SignInResult> TwoFactorAuthenticatorSignInAsync(string code, bool isPersistent, bool rememberClient)
    {
        var startTimestamp = Stopwatch.GetTimestamp();
        try
        {
            var result = await TwoFactorAuthenticatorSignInCoreAsync(code, isPersistent, rememberClient);
            _metrics?.AuthenticateSignIn(typeof(TUser).FullName!, AuthenticationScheme, result, SignInType.TwoFactorAuthenticator, isPersistent, startTimestamp);

            return result;
        }
        catch (Exception ex)
        {
            _metrics?.AuthenticateSignIn(typeof(TUser).FullName!, AuthenticationScheme, result: null, SignInType.TwoFactorAuthenticator, isPersistent, startTimestamp, ex);
            throw;
        }
    }

    private async Task<SignInResult> TwoFactorAuthenticatorSignInCoreAsync(string code, bool isPersistent, bool rememberClient)
    {
        var twoFactorInfo = await RetrieveTwoFactorInfoAsync();
        if (twoFactorInfo == null)
        {
            return SignInResult.Failed;
        }

        var user = twoFactorInfo.User;
        var error = await PreSignInCheck(user);
        if (error != null)
        {
            return error;
        }

        if (await UserManager.VerifyTwoFactorTokenAsync(user, Options.Tokens.AuthenticatorTokenProvider, code))
        {
            return await DoTwoFactorSignInAsync(user, twoFactorInfo, isPersistent, rememberClient);
        }
        // If the token is incorrect, record the failure which also may cause the user to be locked out
        if (UserManager.SupportsUserLockout)
        {
            var incrementLockoutResult = await UserManager.AccessFailedAsync(user) ?? IdentityResult.Success;
            if (!incrementLockoutResult.Succeeded)
            {
                // Return the same failure we do when resetting the lockout fails after a correct two factor code.
                // This is currently redundant, but it's here in case the code gets copied elsewhere.
                return SignInResult.Failed;
            }

            if (await UserManager.IsLockedOutAsync(user))
            {
                return await LockedOut(user);
            }
        }
        return SignInResult.Failed;
    }

    /// <summary>
    /// Validates the two factor sign in code and creates and signs in the user, as an asynchronous operation.
    /// </summary>
    /// <param name="provider">The two factor authentication provider to validate the code against.</param>
    /// <param name="code">The two factor authentication code to validate.</param>
    /// <param name="isPersistent">Flag indicating whether the sign-in cookie should persist after the browser is closed.</param>
    /// <param name="rememberClient">Flag indicating whether the current browser should be remember, suppressing all further
    /// two factor authentication prompts.</param>
    /// <returns>The task object representing the asynchronous operation containing the <see cref="SignInResult"/>
    /// for the sign-in attempt.</returns>
    public virtual async Task<SignInResult> TwoFactorSignInAsync(string provider, string code, bool isPersistent, bool rememberClient)
    {
        var startTimestamp = Stopwatch.GetTimestamp();
        try
        {
            var result = await TwoFactorSignInCoreAsync(provider, code, isPersistent, rememberClient);
            _metrics?.AuthenticateSignIn(typeof(TUser).FullName!, AuthenticationScheme, result, SignInType.TwoFactor, isPersistent, startTimestamp);

            return result;
        }
        catch (Exception ex)
        {
            _metrics?.AuthenticateSignIn(typeof(TUser).FullName!, AuthenticationScheme, result: null, SignInType.TwoFactor, isPersistent, startTimestamp, ex);
            throw;
        }
    }

    private async Task<SignInResult> TwoFactorSignInCoreAsync(string provider, string code, bool isPersistent, bool rememberClient)
    {
        var twoFactorInfo = await RetrieveTwoFactorInfoAsync();
        if (twoFactorInfo == null)
        {
            return SignInResult.Failed;
        }

        var user = twoFactorInfo.User;
        var error = await PreSignInCheck(user);
        if (error != null)
        {
            return error;
        }
        if (await UserManager.VerifyTwoFactorTokenAsync(user, provider, code))
        {
            return await DoTwoFactorSignInAsync(user, twoFactorInfo, isPersistent, rememberClient);
        }
        // If the token is incorrect, record the failure which also may cause the user to be locked out
        if (UserManager.SupportsUserLockout)
        {
            var incrementLockoutResult = await UserManager.AccessFailedAsync(user) ?? IdentityResult.Success;
            if (!incrementLockoutResult.Succeeded)
            {
                // Return the same failure we do when resetting the lockout fails after a correct two factor code.
                // This is currently redundant, but it's here in case the code gets copied elsewhere.
                return SignInResult.Failed;
            }

            if (await UserManager.IsLockedOutAsync(user))
            {
                return await LockedOut(user);
            }
        }
        return SignInResult.Failed;
    }

    /// <summary>
    /// Gets the <typeparamref name="TUser"/> for the current two factor authentication login, as an asynchronous operation.
    /// </summary>
    /// <returns>The task object representing the asynchronous operation containing the <typeparamref name="TUser"/>
    /// for the sign-in attempt.</returns>
    public virtual async Task<TUser?> GetTwoFactorAuthenticationUserAsync()
    {
        var info = await RetrieveTwoFactorInfoAsync();
        if (info == null)
        {
            return null;
        }

        return info.User;
    }

    /// <summary>
    /// Signs in a user via a previously registered third party login, as an asynchronous operation.
    /// </summary>
    /// <param name="loginProvider">The login provider to use.</param>
    /// <param name="providerKey">The unique provider identifier for the user.</param>
    /// <param name="isPersistent">Flag indicating whether the sign-in cookie should persist after the browser is closed.</param>
    /// <returns>The task object representing the asynchronous operation containing the <see cref="SignInResult"/>
    /// for the sign-in attempt.</returns>
    public virtual Task<SignInResult> ExternalLoginSignInAsync(string loginProvider, string providerKey, bool isPersistent)
        => ExternalLoginSignInAsync(loginProvider, providerKey, isPersistent, bypassTwoFactor: false);

    /// <summary>
    /// Signs in a user via a previously registered third party login, as an asynchronous operation.
    /// </summary>
    /// <param name="loginProvider">The login provider to use.</param>
    /// <param name="providerKey">The unique provider identifier for the user.</param>
    /// <param name="isPersistent">Flag indicating whether the sign-in cookie should persist after the browser is closed.</param>
    /// <param name="bypassTwoFactor">Flag indicating whether to bypass two factor authentication.</param>
    /// <returns>The task object representing the asynchronous operation containing the <see cref="SignInResult"/>
    /// for the sign-in attempt.</returns>
    public virtual async Task<SignInResult> ExternalLoginSignInAsync(string loginProvider, string providerKey, bool isPersistent, bool bypassTwoFactor)
    {
        var startTimestamp = Stopwatch.GetTimestamp();
        try
        {
            var result = await ExternalLoginSignInCoreAsync(loginProvider, providerKey, isPersistent, bypassTwoFactor);
            _metrics?.AuthenticateSignIn(typeof(TUser).FullName!, AuthenticationScheme, result, SignInType.External, isPersistent, startTimestamp);

            return result;
        }
        catch (Exception ex)
        {
            _metrics?.AuthenticateSignIn(typeof(TUser).FullName!, AuthenticationScheme, result: null, SignInType.External, isPersistent, startTimestamp, ex);
            throw;
        }
    }

    private async Task<SignInResult> ExternalLoginSignInCoreAsync(string loginProvider, string providerKey, bool isPersistent, bool bypassTwoFactor)
    {
        var user = await UserManager.FindByLoginAsync(loginProvider, providerKey);
        if (user == null)
        {
            return SignInResult.Failed;
        }

        var error = await PreSignInCheck(user);
        if (error != null)
        {
            return error;
        }
        return await SignInOrTwoFactorAsync(user, isPersistent, loginProvider, bypassTwoFactor);
    }

    /// <summary>
    /// Gets a collection of <see cref="Authentication.AuthenticationScheme"/>s for the known external login providers.
    /// </summary>
    /// <returns>A collection of <see cref="Authentication.AuthenticationScheme"/>s for the known external login providers.</returns>
    public virtual async Task<IEnumerable<AuthenticationScheme>> GetExternalAuthenticationSchemesAsync()
    {
        var schemes = await _schemes.GetAllSchemesAsync();
        return schemes.Where(s => !string.IsNullOrEmpty(s.DisplayName));
    }

    /// <summary>
    /// Gets the external login information for the current login, as an asynchronous operation.
    /// </summary>
    /// <param name="expectedXsrf">Flag indication whether a Cross Site Request Forgery token was expected in the current request.</param>
    /// <returns>The task object representing the asynchronous operation containing the <see cref="ExternalLoginInfo"/>
    /// for the sign-in attempt.</returns>
    public virtual async Task<ExternalLoginInfo?> GetExternalLoginInfoAsync(string? expectedXsrf = null)
    {
        var auth = await Context.AuthenticateAsync(IdentityConstants.ExternalScheme);
        var items = auth?.Properties?.Items;
        if (auth?.Principal == null || items == null || !items.TryGetValue(LoginProviderKey, out var provider))
        {
            return null;
        }

        if (expectedXsrf != null)
        {
            if (!items.TryGetValue(XsrfKey, out var userId) ||
                userId != expectedXsrf)
            {
                return null;
            }
        }

        var providerKey = auth.Principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? auth.Principal.FindFirstValue("sub");
        if (providerKey == null || provider == null)
        {
            return null;
        }

        var providerDisplayName = (await GetExternalAuthenticationSchemesAsync()).FirstOrDefault(p => p.Name == provider)?.DisplayName
                                    ?? provider;
        return new ExternalLoginInfo(auth.Principal, provider, providerKey, providerDisplayName)
        {
            AuthenticationTokens = auth.Properties?.GetTokens(),
            AuthenticationProperties = auth.Properties
        };
    }

    /// <summary>
    /// Stores any authentication tokens found in the external authentication cookie into the associated user.
    /// </summary>
    /// <param name="externalLogin">The information from the external login provider.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/> of the operation.</returns>
    public virtual async Task<IdentityResult> UpdateExternalAuthenticationTokensAsync(ExternalLoginInfo externalLogin)
    {
        ArgumentNullException.ThrowIfNull(externalLogin);

        if (externalLogin.AuthenticationTokens != null && externalLogin.AuthenticationTokens.Any())
        {
            var user = await UserManager.FindByLoginAsync(externalLogin.LoginProvider, externalLogin.ProviderKey);
            if (user == null)
            {
                return IdentityResult.Failed();
            }

            foreach (var token in externalLogin.AuthenticationTokens)
            {
                var result = await UserManager.SetAuthenticationTokenAsync(user, externalLogin.LoginProvider, token.Name, token.Value);
                if (!result.Succeeded)
                {
                    return result;
                }
            }
        }

        return IdentityResult.Success;
    }

    /// <summary>
    /// Configures the redirect URL and user identifier for the specified external login <paramref name="provider"/>.
    /// </summary>
    /// <param name="provider">The provider to configure.</param>
    /// <param name="redirectUrl">The external login URL users should be redirected to during the login flow.</param>
    /// <param name="userId">The current user's identifier, which will be used to provide CSRF protection.</param>
    /// <returns>A configured <see cref="AuthenticationProperties"/>.</returns>
    public virtual AuthenticationProperties ConfigureExternalAuthenticationProperties(string? provider, [StringSyntax(StringSyntaxAttribute.Uri)] string? redirectUrl, string? userId = null)
    {
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        properties.Items[LoginProviderKey] = provider;
        if (userId != null)
        {
            properties.Items[XsrfKey] = userId;
        }
        return properties;
    }

    /// <summary>
    /// Creates a claims principal for the specified 2fa information.
    /// </summary>
    /// <param name="userId">The user whose is logging in via 2fa.</param>
    /// <param name="loginProvider">The 2fa provider.</param>
    /// <returns>A <see cref="ClaimsPrincipal"/> containing the user 2fa information.</returns>
    internal static ClaimsPrincipal StoreTwoFactorInfo(string userId, string? loginProvider)
    {
        var identity = new ClaimsIdentity(IdentityConstants.TwoFactorUserIdScheme);
        identity.AddClaim(new Claim(ClaimTypes.Name, userId));
        if (loginProvider != null)
        {
            identity.AddClaim(new Claim(ClaimTypes.AuthenticationMethod, loginProvider));
        }
        return new ClaimsPrincipal(identity);
    }

    internal async Task<ClaimsPrincipal> StoreRememberClient(TUser user)
    {
        var userId = await UserManager.GetUserIdAsync(user);
        var rememberBrowserIdentity = new ClaimsIdentity(IdentityConstants.TwoFactorRememberMeScheme);
        rememberBrowserIdentity.AddClaim(new Claim(ClaimTypes.Name, userId));
        if (UserManager.SupportsUserSecurityStamp)
        {
            var stamp = await UserManager.GetSecurityStampAsync(user);
            rememberBrowserIdentity.AddClaim(new Claim(Options.ClaimsIdentity.SecurityStampClaimType, stamp));
        }
        return new ClaimsPrincipal(rememberBrowserIdentity);
    }

    /// <summary>
    /// Check if the <paramref name="user"/> has two factor enabled.
    /// </summary>
    /// <param name="user"></param>
    /// <returns>
    /// The task object representing the asynchronous operation containing true if the user has two factor enabled.
    /// </returns>
    public virtual async Task<bool> IsTwoFactorEnabledAsync(TUser user)
        => UserManager.SupportsUserTwoFactor &&
        await UserManager.GetTwoFactorEnabledAsync(user) &&
        (await UserManager.GetValidTwoFactorProvidersAsync(user)).Count > 0;

    /// <summary>
    /// Signs in the specified <paramref name="user"/> if <paramref name="bypassTwoFactor"/> is set to false.
    /// Otherwise stores the <paramref name="user"/> for use after a two factor check.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="isPersistent">Flag indicating whether the sign-in cookie should persist after the browser is closed.</param>
    /// <param name="loginProvider">The login provider to use. Default is null</param>
    /// <param name="bypassTwoFactor">Flag indicating whether to bypass two factor authentication. Default is false</param>
    /// <returns>Returns a <see cref="SignInResult"/></returns>
    protected virtual async Task<SignInResult> SignInOrTwoFactorAsync(TUser user, bool isPersistent, string? loginProvider = null, bool bypassTwoFactor = false)
    {
        if (!bypassTwoFactor && await IsTwoFactorEnabledAsync(user))
        {
            if (!await IsTwoFactorClientRememberedAsync(user))
            {
                // Allow the two-factor flow to continue later within the same request with or without a TwoFactorUserIdScheme in
                // the event that the two-factor code or recovery code has already been provided as is the case for MapIdentityApi.
                _twoFactorInfo = new()
                {
                    User = user,
                    LoginProvider = loginProvider,
                };

                if (await _schemes.GetSchemeAsync(IdentityConstants.TwoFactorUserIdScheme) != null)
                {
                    // Store the userId for use after two factor check
                    var userId = await UserManager.GetUserIdAsync(user);
                    await Context.SignInAsync(IdentityConstants.TwoFactorUserIdScheme, StoreTwoFactorInfo(userId, loginProvider));
                }

                return SignInResult.TwoFactorRequired;
            }
        }
        // Cleanup external cookie
        if (loginProvider != null)
        {
            await Context.SignOutAsync(IdentityConstants.ExternalScheme);
        }
        if (loginProvider == null)
        {
            await SignInWithClaimsAsync(user, isPersistent, new Claim[] { new Claim("amr", "pwd") });
        }
        else
        {
            await SignInAsync(user, isPersistent, loginProvider);
        }
        return SignInResult.Success;
    }

    private async Task<TwoFactorAuthenticationInfo?> RetrieveTwoFactorInfoAsync()
    {
        if (_twoFactorInfo != null)
        {
            return _twoFactorInfo;
        }

        var result = await Context.AuthenticateAsync(IdentityConstants.TwoFactorUserIdScheme);
        if (result?.Principal == null)
        {
            return null;
        }

        var userId = result.Principal.FindFirstValue(ClaimTypes.Name);
        if (userId == null)
        {
            return null;
        }

        var user = await UserManager.FindByIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        return new TwoFactorAuthenticationInfo
        {
            User = user,
            LoginProvider = result.Principal.FindFirstValue(ClaimTypes.AuthenticationMethod),
        };
    }

    /// <summary>
    /// Used to determine if a user is considered locked out.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>Whether a user is considered locked out.</returns>
    protected virtual async Task<bool> IsLockedOut(TUser user)
    {
        return UserManager.SupportsUserLockout && await UserManager.IsLockedOutAsync(user);
    }

    /// <summary>
    /// Returns a locked out SignInResult.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>A locked out SignInResult</returns>
    protected virtual Task<SignInResult> LockedOut(TUser user)
    {
        Logger.LogDebug(EventIds.UserLockedOut, "User is currently locked out.");
        return Task.FromResult(SignInResult.LockedOut);
    }

    /// <summary>
    /// Used to ensure that a user is allowed to sign in.
    /// </summary>
    /// <param name="user">The user</param>
    /// <returns>Null if the user should be allowed to sign in, otherwise the SignInResult why they should be denied.</returns>
    protected virtual async Task<SignInResult?> PreSignInCheck(TUser user)
    {
        if (!await CanSignInAsync(user))
        {
            return SignInResult.NotAllowed;
        }
        if (await IsLockedOut(user))
        {
            return await LockedOut(user);
        }
        return null;
    }

    /// <summary>
    /// Used to reset a user's lockout count.
    /// </summary>
    /// <param name="user">The user</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/> of the operation.</returns>
    protected virtual async Task ResetLockout(TUser user)
    {
        if (UserManager.SupportsUserLockout)
        {
            // The IdentityResult should not be null according to the annotations, but our own tests return null and I'm trying to limit breakages.
            var result = await UserManager.ResetAccessFailedCountAsync(user) ?? IdentityResult.Success;

            if (!result.Succeeded)
            {
                throw new IdentityResultException(result);
            }
        }
    }

    private async Task<IdentityResult> ResetLockoutWithResult(TUser user)
    {
        // Avoid relying on throwing an exception if we're not in a derived class.
        if (GetType() == typeof(SignInManager<TUser>))
        {
            if (!UserManager.SupportsUserLockout)
            {
                return IdentityResult.Success;
            }

            return await UserManager.ResetAccessFailedCountAsync(user) ?? IdentityResult.Success;
        }

        try
        {
            var resetLockoutTask = ResetLockout(user);

            if (resetLockoutTask is Task<IdentityResult> resultTask)
            {
                return await resultTask ?? IdentityResult.Success;
            }

            await resetLockoutTask;
            return IdentityResult.Success;
        }
        catch (IdentityResultException ex)
        {
            return ex.IdentityResult;
        }
    }

    private sealed class IdentityResultException : Exception
    {
        internal IdentityResultException(IdentityResult result) : base()
        {
            IdentityResult = result;
        }

        internal IdentityResult IdentityResult { get; set; }

        public override string Message
        {
            get
            {
                var sb = new StringBuilder("ResetLockout failed.");

                foreach (var error in IdentityResult.Errors)
                {
                    sb.AppendLine();
                    sb.Append(error.Code);
                    sb.Append(": ");
                    sb.Append(error.Description);
                }

                return sb.ToString();
            }
        }
    }

    internal sealed class TwoFactorAuthenticationInfo
    {
        public required TUser User { get; init; }
        public string? LoginProvider { get; init; }
    }

    internal sealed class PasskeyAuthenticationInfo
    {
        public required string? Operation { get; init; }
        public required string? State { get; init; }

    }

    private static class PasskeyOperations
    {
        public const string Attestation = "Attestation";
        public const string Assertion = "Assertion";
    }
}
