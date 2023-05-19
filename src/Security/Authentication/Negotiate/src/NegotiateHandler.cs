// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Authentication.Negotiate;

/// <summary>
/// Authenticates requests using Negotiate, Kerberos, or NTLM.
/// </summary>
public class NegotiateHandler : AuthenticationHandler<NegotiateOptions>, IAuthenticationRequestHandler
{
    private const string AuthPersistenceKey = nameof(AuthPersistence);
    private const string NegotiateVerb = "Negotiate";
    private const string AuthHeaderPrefix = NegotiateVerb + " ";

    private bool _requestProcessed;
    private INegotiateState? _negotiateState;

    /// <summary>
    /// Creates a new <see cref="NegotiateHandler"/>
    /// </summary>
    /// <inheritdoc />
    [Obsolete("ISystemClock is obsolete, use TimeProvider on AuthenticationSchemeOptions instead.")]
    public NegotiateHandler(IOptionsMonitor<NegotiateOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock)
    { }

    /// <summary>
    /// Creates a new <see cref="NegotiateHandler"/>
    /// </summary>
    /// <inheritdoc />
    public NegotiateHandler(IOptionsMonitor<NegotiateOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    { }

    /// <summary>
    /// The handler calls methods on the events which give the application control at certain points where processing is occurring.
    /// If it is not provided a default instance is supplied which does nothing when the methods are called.
    /// </summary>
    protected new NegotiateEvents Events
    {
        get => (NegotiateEvents)base.Events!;
        set => base.Events = value;
    }

    /// <summary>
    /// Creates the default events type.
    /// </summary>
    /// <returns></returns>
    protected override Task<object> CreateEventsAsync() => Task.FromResult<object>(new NegotiateEvents());

    private bool IsSupportedProtocol => HttpProtocol.IsHttp11(Request.Protocol) || HttpProtocol.IsHttp10(Request.Protocol);

    /// <summary>
    /// Intercepts incomplete Negotiate authentication handshakes and continues or completes them.
    /// </summary>
    /// <returns><see langword="true" /> if a response was generated, otherwise <see langword="false"/>.</returns>
    public async Task<bool> HandleRequestAsync()
    {
        AuthPersistence? persistence = null;
        bool authFailedEventCalled = false;
        try
        {
            if (_requestProcessed || Options.DeferToServer)
            {
                // This request was already processed but something is re-executing it like an exception handler.
                // Don't re-run because we could corrupt the connection state, e.g. if this was a stage2 NTLM request
                // that we've already completed the handshake for.
                // Or we're in deferral mode where we let the server handle the authentication.
                return false;
            }

            _requestProcessed = true;

            if (!IsSupportedProtocol)
            {
                // HTTP/1.0 and HTTP/1.1 are supported. Do not throw because this may be running on a server that supports
                // additional protocols.
                return false;
            }

            var connectionItems = GetConnectionItems();
            persistence = (AuthPersistence)connectionItems[AuthPersistenceKey]!;
            _negotiateState = persistence?.State;

            var authorizationHeader = Request.Headers.Authorization;

            if (StringValues.IsNullOrEmpty(authorizationHeader))
            {
                if (_negotiateState?.IsCompleted == false)
                {
                    throw new InvalidOperationException("An anonymous request was received in between authentication handshake requests.");
                }
                return false;
            }

            var authorization = authorizationHeader.ToString();
            string? token = null;
            if (authorization.StartsWith(AuthHeaderPrefix, StringComparison.OrdinalIgnoreCase))
            {
                token = authorization.Substring(AuthHeaderPrefix.Length).Trim();
            }
            else
            {
                if (_negotiateState?.IsCompleted == false)
                {
                    throw new InvalidOperationException("Non-negotiate request was received in between authentication handshake requests.");
                }
                return false;
            }

            // WinHttpHandler re-authenticates an existing connection if it gets another challenge on subsequent requests.
            if (_negotiateState?.IsCompleted == true)
            {
                Logger.Reauthenticating();
                _negotiateState.Dispose();
                _negotiateState = null;
                if (persistence != null)
                {
                    persistence.State = null;
                }
            }

            _negotiateState ??= Options.StateFactory.CreateInstance();

            var outgoing = _negotiateState.GetOutgoingBlob(token, out var errorType, out var exception);
            if (errorType != BlobErrorType.None)
            {
                Debug.Assert(exception != null);

                Logger.NegotiateError(errorType.ToString());
                _negotiateState.Dispose();
                _negotiateState = null;
                if (persistence?.State != null)
                {
                    persistence.State.Dispose();
                    persistence.State = null;
                }

                if (errorType == BlobErrorType.CredentialError)
                {
                    Logger.CredentialError(exception);
                    authFailedEventCalled = true; // Could throw, and we don't want to double trigger the event.
                    var result = await InvokeAuthenticateFailedEvent(exception);
                    return result ?? false; // Default to skipping the handler, let AuthZ generate a new 401
                }
                else if (errorType == BlobErrorType.ClientError)
                {
                    Logger.ClientError(exception);
                    authFailedEventCalled = true; // Could throw, and we don't want to double trigger the event.
                    var result = await InvokeAuthenticateFailedEvent(exception);
                    if (result.HasValue)
                    {
                        return result.Value;
                    }
                    Context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return true; // Default to terminating request
                }

                throw exception;
            }

            if (!_negotiateState.IsCompleted)
            {
                persistence ??= EstablishConnectionPersistence(connectionItems);
                // Save the state long enough to complete the multi-stage handshake.
                // We'll remove it once complete if !PersistNtlm/KerberosCredentials.
                persistence.State = _negotiateState;

                Logger.IncompleteNegotiateChallenge();
                Response.StatusCode = StatusCodes.Status401Unauthorized;
                Response.Headers.Append(HeaderNames.WWWAuthenticate, AuthHeaderPrefix + outgoing);
                return true;
            }

            Logger.NegotiateComplete();

            // There can be a final blob of data we need to send to the client, but let the request execute as normal.
            if (!string.IsNullOrEmpty(outgoing))
            {
                Response.OnStarting(() =>
                {
                    // Only include it if the response ultimately succeeds. This avoids adding it twice if Challenge is called again.
                    if (Response.StatusCode < StatusCodes.Status400BadRequest)
                    {
                        Response.Headers.Append(HeaderNames.WWWAuthenticate, AuthHeaderPrefix + outgoing);
                    }
                    return Task.CompletedTask;
                });
            }

            // Deal with connection credential persistence.

            if (_negotiateState.Protocol == "NTLM" && !Options.PersistNtlmCredentials)
            {
                // NTLM was already put in the persitence cache on the prior request so we could complete the handshake.
                // Take it out if we don't want it to persist.
                Debug.Assert(object.ReferenceEquals(persistence?.State, _negotiateState),
                    "NTLM is a two stage process, it must have already been in the cache for the handshake to succeed.");
                Logger.DisablingCredentialPersistence(_negotiateState.Protocol);
                persistence.State = null;
                Response.RegisterForDispose(_negotiateState);
            }
            else if (_negotiateState.Protocol == "Kerberos")
            {
                // Kerberos can require one or two stage handshakes
                if (Options.PersistKerberosCredentials)
                {
                    Logger.EnablingCredentialPersistence();
                    persistence ??= EstablishConnectionPersistence(connectionItems);
                    persistence.State = _negotiateState;
                }
                else
                {
                    if (persistence?.State != null)
                    {
                        Logger.DisablingCredentialPersistence(_negotiateState.Protocol);
                        persistence.State = null;
                    }
                    Response.RegisterForDispose(_negotiateState);
                }
            }

            // Note we run the Authenticated event in HandleAuthenticateAsync so it is per-request rather than per connection.
        }
        catch (Exception ex)
        {
            if (authFailedEventCalled)
            {
                throw;
            }

            Logger.ExceptionProcessingAuth(ex);

            // Clear state so it's possible to retry on the same connection.
            _negotiateState?.Dispose();
            _negotiateState = null;
            if (persistence?.State != null)
            {
                persistence.State.Dispose();
                persistence.State = null;
            }

            var result = await InvokeAuthenticateFailedEvent(ex);
            if (result.HasValue)
            {
                return result.Value;
            }

            throw;
        }

        return false;
    }

    private async Task<bool?> InvokeAuthenticateFailedEvent(Exception ex)
    {
        var errorContext = new AuthenticationFailedContext(Context, Scheme, Options) { Exception = ex };
        await Events.AuthenticationFailed(errorContext);

        if (errorContext.Result != null)
        {
            if (errorContext.Result.Handled)
            {
                return true;
            }
            else if (errorContext.Result.Skipped)
            {
                return false;
            }
            else if (errorContext.Result.Failure != null)
            {
                throw new AuthenticationFailureException("An error was returned from the AuthenticationFailed event.", errorContext.Result.Failure);
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if the current request is authenticated and returns the user.
    /// </summary>
    /// <returns></returns>
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!_requestProcessed)
        {
            throw new InvalidOperationException("AuthenticateAsync must not be called before the UseAuthentication middleware runs.");
        }

        if (!IsSupportedProtocol)
        {
            // Not supported. We don't throw because Negotiate may be set as the default auth
            // handler on a server that's running HTTP/1 and HTTP/2. We'll challenge HTTP/2 requests
            // that require auth and they'll downgrade to HTTP/1.1.
            Logger.ProtocolNotSupported(Request.Protocol);
            return AuthenticateResult.NoResult();
        }

        if (_negotiateState == null)
        {
            return AuthenticateResult.NoResult();
        }

        if (!_negotiateState.IsCompleted)
        {
            // This case should have been rejected by HandleRequestAsync
            throw new InvalidOperationException("Attempting to use an incomplete authentication context.");
        }

        // Make a new copy of the user for each request, they are mutable objects and
        // things like ClaimsTransformation run per request.
        var identity = _negotiateState.GetIdentity();
        ClaimsPrincipal user;
        if (OperatingSystem.IsWindows() && identity is WindowsIdentity winIdentity)
        {
            user = new WindowsPrincipal(winIdentity);
            Response.RegisterForDispose(winIdentity);
        }
        else
        {
            user = new ClaimsPrincipal(new ClaimsIdentity(identity));
        }

        AuthenticatedContext authenticatedContext;

        if (Options.LdapSettings.EnableLdapClaimResolution)
        {
            var ldapContext = new LdapContext(Context, Scheme, Options, Options.LdapSettings)
            {
                Principal = user
            };

            await Events.RetrieveLdapClaims(ldapContext);

            if (ldapContext.Result != null)
            {
                return ldapContext.Result;
            }

            await LdapAdapter.RetrieveClaimsAsync(ldapContext.LdapSettings, (ldapContext.Principal.Identity as ClaimsIdentity)!, Logger);

            authenticatedContext = new AuthenticatedContext(Context, Scheme, Options)
            {
                Principal = ldapContext.Principal
            };
        }
        else
        {
            authenticatedContext = new AuthenticatedContext(Context, Scheme, Options)
            {
                Principal = user
            };
        }

        await Events.Authenticated(authenticatedContext);

        if (authenticatedContext.Result != null)
        {
            return authenticatedContext.Result;
        }

        var ticket = new AuthenticationTicket(authenticatedContext.Principal, authenticatedContext.Properties, Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }

    /// <summary>
    /// Issues a 401 WWW-Authenticate Negotiate challenge.
    /// </summary>
    /// <param name="properties"></param>
    /// <returns></returns>
    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        // We allow issuing a challenge from an HTTP/2 request. Browser clients will gracefully downgrade to HTTP/1.1.
        // SocketHttpHandler will not downgrade (https://github.com/dotnet/corefx/issues/35195), but WinHttpHandler will.
        var eventContext = new ChallengeContext(Context, Scheme, Options, properties);
        await Events.Challenge(eventContext);
        if (eventContext.Handled)
        {
            return;
        }

        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.Append(HeaderNames.WWWAuthenticate, NegotiateVerb);
        Logger.ChallengeNegotiate();
    }

    private AuthPersistence EstablishConnectionPersistence(IDictionary<object, object?> items)
    {
        Debug.Assert(!items.ContainsKey(AuthPersistenceKey), "This should only be registered once per connection");
        var persistence = new AuthPersistence();
        RegisterForConnectionDispose(persistence);
        items[AuthPersistenceKey] = persistence;
        return persistence;
    }

    private IDictionary<object, object?> GetConnectionItems()
    {
        return Context.Features.Get<IConnectionItemsFeature>()?.Items
            ?? throw new NotSupportedException($"Negotiate authentication requires a server that supports {nameof(IConnectionItemsFeature)} like Kestrel.");
    }

    private void RegisterForConnectionDispose(IDisposable authState)
    {
        var connectionCompleteFeature = Context.Features.Get<IConnectionCompleteFeature>()
            ?? throw new NotSupportedException($"Negotiate authentication requires a server that supports {nameof(IConnectionCompleteFeature)} like Kestrel.");
        connectionCompleteFeature.OnCompleted(DisposeState, authState);
    }

    private static Task DisposeState(object state)
    {
        ((IDisposable)state).Dispose();
        return Task.CompletedTask;
    }

    // This allows us to have one disposal registration per connection and limits churn on the Items collection.
    private sealed class AuthPersistence : IDisposable
    {
        internal INegotiateState? State { get; set; }

        public void Dispose()
        {
            State?.Dispose();
        }
    }
}
