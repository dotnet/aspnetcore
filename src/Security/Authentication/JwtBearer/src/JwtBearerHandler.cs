// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Authentication.JwtBearer;

/// <summary>
/// An <see cref="AuthenticationHandler{TOptions}"/> that can perform JWT-bearer based authentication.
/// </summary>
public class JwtBearerHandler : AuthenticationHandler<JwtBearerOptions>
{
    /// <summary>
    /// Initializes a new instance of <see cref="JwtBearerHandler"/>.
    /// </summary>
    /// <inheritdoc />
    [Obsolete("ISystemClock is obsolete, use TimeProvider on AuthenticationSchemeOptions instead.")]
    public JwtBearerHandler(IOptionsMonitor<JwtBearerOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock)
    { }

    /// <summary>
    /// Initializes a new instance of <see cref="JwtBearerHandler"/>.
    /// </summary>
    /// <inheritdoc />
    public JwtBearerHandler(IOptionsMonitor<JwtBearerOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    { }

    /// <summary>
    /// The handler calls methods on the events which give the application control at certain points where processing is occurring.
    /// If it is not provided a default instance is supplied which does nothing when the methods are called.
    /// </summary>
    protected new JwtBearerEvents Events
    {
        get => (JwtBearerEvents)base.Events!;
        set => base.Events = value;
    }

    /// <inheritdoc />
    protected override Task<object> CreateEventsAsync() => Task.FromResult<object>(new JwtBearerEvents());

    /// <summary>
    /// Searches the 'Authorization' header for a 'Bearer' token. If the 'Bearer' token is found, it is validated using <see cref="TokenValidationParameters"/> set in the options.
    /// </summary>
    /// <returns></returns>
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? token;
        try
        {
            // Give application opportunity to find from a different location, adjust, or reject token
            var messageReceivedContext = new MessageReceivedContext(Context, Scheme, Options);

            // event can set the token
            await Events.MessageReceived(messageReceivedContext);
            if (messageReceivedContext.Result != null)
            {
                return messageReceivedContext.Result;
            }

            // If application retrieved token from somewhere else, use that.
            token = messageReceivedContext.Token;

            if (string.IsNullOrEmpty(token))
            {
                string authorization = Request.Headers.Authorization.ToString();

                // If no authorization header found, nothing to process further
                if (string.IsNullOrEmpty(authorization))
                {
                    return AuthenticateResult.NoResult();
                }

                if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    token = authorization.Substring("Bearer ".Length).Trim();
                }

                // If no token found, no further work possible
                if (string.IsNullOrEmpty(token))
                {
                    return AuthenticateResult.NoResult();
                }
            }

            var tvp = await SetupTokenValidationParametersAsync();
            List<Exception>? validationFailures = null;
            SecurityToken? validatedToken = null;
            ClaimsPrincipal? principal = null;

            if (!Options.UseSecurityTokenValidators)
            {
                foreach (var tokenHandler in Options.TokenHandlers)
                {
                    try
                    {
                        var tokenValidationResult = await tokenHandler.ValidateTokenAsync(token, tvp);
                        if (tokenValidationResult.IsValid)
                        {
                            principal = new ClaimsPrincipal(tokenValidationResult.ClaimsIdentity);
                            validatedToken = tokenValidationResult.SecurityToken;
                            break;
                        }
                        else
                        {
                            validationFailures ??= new List<Exception>(1);
                            RecordTokenValidationError(tokenValidationResult.Exception ?? new SecurityTokenValidationException($"The TokenHandler: '{tokenHandler}', was unable to validate the Token."), validationFailures);
                        }
                    }
                    catch (Exception ex)
                    {
                        validationFailures ??= new List<Exception>(1);
                        RecordTokenValidationError(ex, validationFailures);
                    }
                }
            }
            else
            {
#pragma warning disable CS0618 // Type or member is obsolete
                foreach (var validator in Options.SecurityTokenValidators)
                {
                    if (validator.CanReadToken(token))
                    {
                        try
                        {
                            principal = validator.ValidateToken(token, tvp, out validatedToken);
                        }
                        catch (Exception ex)
                        {
                            validationFailures ??= new List<Exception>(1);
                            RecordTokenValidationError(ex, validationFailures);
                            continue;
                        }
                    }
                }
#pragma warning restore CS0618 // Type or member is obsolete
            }

            if (principal != null && validatedToken != null)
            {
                Logger.TokenValidationSucceeded();

                var tokenValidatedContext = new TokenValidatedContext(Context, Scheme, Options)
                {
                    Principal = principal
                };

                tokenValidatedContext.SecurityToken = validatedToken;
                tokenValidatedContext.Properties.ExpiresUtc = GetSafeDateTime(validatedToken.ValidTo);
                tokenValidatedContext.Properties.IssuedUtc = GetSafeDateTime(validatedToken.ValidFrom);

                await Events.TokenValidated(tokenValidatedContext);
                if (tokenValidatedContext.Result != null)
                {
                    return tokenValidatedContext.Result;
                }

                if (Options.SaveToken)
                {
                    tokenValidatedContext.Properties.StoreTokens(new[]
                    {
                        new AuthenticationToken { Name = "access_token", Value = token }
                    });
                }

                tokenValidatedContext.Success();
                return tokenValidatedContext.Result!;
            }

            if (validationFailures != null)
            {
                var authenticationFailedContext = new AuthenticationFailedContext(Context, Scheme, Options)
                {
                    Exception = (validationFailures.Count == 1) ? validationFailures[0] : new AggregateException(validationFailures)
                };

                await Events.AuthenticationFailed(authenticationFailedContext);
                if (authenticationFailedContext.Result != null)
                {
                    return authenticationFailedContext.Result;
                }

                return AuthenticateResult.Fail(authenticationFailedContext.Exception);
            }

            if (!Options.UseSecurityTokenValidators)
            {
                return AuthenticateResults.TokenHandlerUnableToValidate;
            }

            return AuthenticateResults.ValidatorNotFound;
        }
        catch (Exception ex)
        {
            Logger.ErrorProcessingMessage(ex);

            var authenticationFailedContext = new AuthenticationFailedContext(Context, Scheme, Options)
            {
                Exception = ex
            };

            await Events.AuthenticationFailed(authenticationFailedContext);
            if (authenticationFailedContext.Result != null)
            {
                return authenticationFailedContext.Result;
            }

            throw;
        }
    }

    private void RecordTokenValidationError(Exception exception, List<Exception> exceptions)
    {
        if (exception != null)
        {
            Logger.TokenValidationFailed(exception);
            exceptions.Add(exception);
        }

        // Refresh the configuration for exceptions that may be caused by key rollovers. The user can also request a refresh in the event.
        // Refreshing on SecurityTokenSignatureKeyNotFound may be redundant if Last-Known-Good is enabled, it won't do much harm, most likely will be a nop.
        if (Options.RefreshOnIssuerKeyNotFound && Options.ConfigurationManager != null
            && exception is SecurityTokenSignatureKeyNotFoundException)
        {
            Options.ConfigurationManager.RequestRefresh();
        }
    }

    private async Task<TokenValidationParameters> SetupTokenValidationParametersAsync()
    {
        // Clone to avoid cross request race conditions for updated configurations.
        var tokenValidationParameters = Options.TokenValidationParameters.Clone();

        if (Options.ConfigurationManager is BaseConfigurationManager baseConfigurationManager)
        {
            tokenValidationParameters.ConfigurationManager = baseConfigurationManager;
        }
        else
        {
            if (Options.ConfigurationManager != null)
            {
                // GetConfigurationAsync has a time interval that must pass before new http request will be issued.
                var configuration = await Options.ConfigurationManager.GetConfigurationAsync(Context.RequestAborted);
                var issuers = new[] { configuration.Issuer };
                tokenValidationParameters.ValidIssuers = (tokenValidationParameters.ValidIssuers == null ? issuers : tokenValidationParameters.ValidIssuers.Concat(issuers));
                tokenValidationParameters.IssuerSigningKeys = (tokenValidationParameters.IssuerSigningKeys == null ? configuration.SigningKeys : tokenValidationParameters.IssuerSigningKeys.Concat(configuration.SigningKeys));
            }
        }

        return tokenValidationParameters;
    }

    private static DateTime? GetSafeDateTime(DateTime dateTime)
    {
        // Assigning DateTime.MinValue or default(DateTime) to a DateTimeOffset when in a UTC+X timezone will throw
        // Since we don't really care about DateTime.MinValue in this case let's just set the field to null
        if (dateTime == DateTime.MinValue)
        {
            return null;
        }
        return dateTime;
    }

    /// <inheritdoc />
    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        var authResult = await HandleAuthenticateOnceSafeAsync();
        var eventContext = new JwtBearerChallengeContext(Context, Scheme, Options, properties)
        {
            AuthenticateFailure = authResult?.Failure
        };

        // Avoid returning error=invalid_token if the error is not caused by an authentication failure (e.g missing token).
        if (Options.IncludeErrorDetails && eventContext.AuthenticateFailure != null)
        {
            eventContext.Error = "invalid_token";
            eventContext.ErrorDescription = CreateErrorDescription(eventContext.AuthenticateFailure);
        }

        await Events.Challenge(eventContext);
        if (eventContext.Handled)
        {
            return;
        }

        Response.StatusCode = 401;

        if (string.IsNullOrEmpty(eventContext.Error) &&
            string.IsNullOrEmpty(eventContext.ErrorDescription) &&
            string.IsNullOrEmpty(eventContext.ErrorUri))
        {
            Response.Headers.Append(HeaderNames.WWWAuthenticate, Options.Challenge);
        }
        else
        {
            // https://tools.ietf.org/html/rfc6750#section-3.1
            // WWW-Authenticate: Bearer realm="example", error="invalid_token", error_description="The access token expired"
            var builder = new StringBuilder(Options.Challenge);
            if (Options.Challenge.IndexOf(' ') > 0)
            {
                // Only add a comma after the first param, if any
                builder.Append(',');
            }
            if (!string.IsNullOrEmpty(eventContext.Error))
            {
                builder.Append(" error=\"");
                builder.Append(eventContext.Error);
                builder.Append('\"');
            }
            if (!string.IsNullOrEmpty(eventContext.ErrorDescription))
            {
                if (!string.IsNullOrEmpty(eventContext.Error))
                {
                    builder.Append(',');
                }

                builder.Append(" error_description=\"");
                builder.Append(eventContext.ErrorDescription);
                builder.Append('\"');
            }
            if (!string.IsNullOrEmpty(eventContext.ErrorUri))
            {
                if (!string.IsNullOrEmpty(eventContext.Error) ||
                    !string.IsNullOrEmpty(eventContext.ErrorDescription))
                {
                    builder.Append(',');
                }

                builder.Append(" error_uri=\"");
                builder.Append(eventContext.ErrorUri);
                builder.Append('\"');
            }

            Response.Headers.Append(HeaderNames.WWWAuthenticate, builder.ToString());
        }
    }

    /// <inheritdoc />
    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        var forbiddenContext = new ForbiddenContext(Context, Scheme, Options);

        if (Response.StatusCode == 403)
        {
            // No-op
        }
        else if (Response.HasStarted)
        {
            Logger.ForbiddenResponseHasStarted();
        }
        else
        {
            Response.StatusCode = 403;
        }

        return Events.Forbidden(forbiddenContext);
    }

    private static string CreateErrorDescription(Exception authFailure)
    {
        IReadOnlyCollection<Exception> exceptions;
        if (authFailure is AggregateException agEx)
        {
            exceptions = agEx.InnerExceptions;
        }
        else
        {
            exceptions = new[] { authFailure };
        }

        var messages = new List<string>(exceptions.Count);

        foreach (var ex in exceptions)
        {
            // Order sensitive, some of these exceptions derive from others
            // and we want to display the most specific message possible.
            string? message = ex switch
            {
                SecurityTokenInvalidAudienceException stia => $"The audience '{stia.InvalidAudience ?? "(null)"}' is invalid",
                SecurityTokenInvalidIssuerException stii => $"The issuer '{stii.InvalidIssuer ?? "(null)"}' is invalid",
                SecurityTokenNoExpirationException _ => "The token has no expiration",
                SecurityTokenInvalidLifetimeException stil => "The token lifetime is invalid; NotBefore: "
                    + $"'{stil.NotBefore?.ToString(CultureInfo.InvariantCulture) ?? "(null)"}'"
                    + $", Expires: '{stil.Expires?.ToString(CultureInfo.InvariantCulture) ?? "(null)"}'",
                SecurityTokenNotYetValidException stnyv => $"The token is not valid before '{stnyv.NotBefore.ToString(CultureInfo.InvariantCulture)}'",
                SecurityTokenExpiredException ste => $"The token expired at '{ste.Expires.ToString(CultureInfo.InvariantCulture)}'",
                SecurityTokenSignatureKeyNotFoundException _ => "The signature key was not found",
                SecurityTokenInvalidSignatureException _ => "The signature is invalid",
                _ => null,
            };

            if (message is not null)
            {
                messages.Add(message);
            }
        }

        return string.Join("; ", messages);
    }
}
