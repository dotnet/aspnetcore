// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Encodings.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.Certificate;

internal sealed class CertificateAuthenticationHandler : AuthenticationHandler<CertificateAuthenticationOptions>
{
    private static readonly Oid ClientCertificateOid = new Oid("1.3.6.1.5.5.7.3.2");
    private ICertificateValidationCache? _cache;

    public CertificateAuthenticationHandler(
        IOptionsMonitor<CertificateAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    /// <summary>
    /// The handler calls methods on the events which give the application control at certain points where processing is occurring.
    /// If it is not provided a default instance is supplied which does nothing when the methods are called.
    /// </summary>
    private new CertificateAuthenticationEvents Events
    {
        get { return (CertificateAuthenticationEvents)base.Events!; }
        set { base.Events = value; }
    }

    /// <summary>
    /// Creates a new instance of the events instance.
    /// </summary>
    /// <returns>A new instance of the events instance.</returns>
    protected override Task<object> CreateEventsAsync() => Task.FromResult<object>(new CertificateAuthenticationEvents());

    protected override Task InitializeHandlerAsync()
    {
        _cache = Context.RequestServices.GetService<ICertificateValidationCache>();
        return base.InitializeHandlerAsync();
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // You only get client certificates over HTTPS
        if (!Context.Request.IsHttps)
        {
            Logger.NotHttps();
            return AuthenticateResult.NoResult();
        }

        try
        {
            var clientCertificate = await Context.Connection.GetClientCertificateAsync();

            // This should never be the case, as cert authentication happens long before ASP.NET kicks in.
            if (clientCertificate == null)
            {
                Logger.NoCertificate();
                return AuthenticateResult.NoResult();
            }

            if (_cache != null)
            {
                var cacheHit = _cache.Get(Context, clientCertificate);
                if (cacheHit != null)
                {
                    return cacheHit;
                }
            }

            var result = await ValidateCertificateAsync(clientCertificate);

            // Invoke the failed handler if validation failed, before updating the cache
            if (result.Failure != null)
            {
                var authenticationFailedContext = await HandleFailureAsync(result.Failure);
                if (authenticationFailedContext.Result != null)
                {
                    result = authenticationFailedContext.Result;
                }
            }

            _cache?.Put(Context, clientCertificate, result);
            return result;
        }
        catch (Exception ex)
        {
            var authenticationFailedContext = await HandleFailureAsync(ex);
            if (authenticationFailedContext.Result != null)
            {
                return authenticationFailedContext.Result;
            }

            throw;
        }
    }

    private async Task<CertificateAuthenticationFailedContext> HandleFailureAsync(Exception error)
    {
        var authenticationFailedContext = new CertificateAuthenticationFailedContext(Context, Scheme, Options)
        {
            Exception = error
        };

        await Events.AuthenticationFailed(authenticationFailedContext);
        return authenticationFailedContext;
    }

    private async Task<AuthenticateResult> ValidateCertificateAsync(X509Certificate2 clientCertificate)
    {
        var isCertificateSelfSigned = clientCertificate.IsSelfSigned();

        // If we have a self signed cert, and they're not allowed, exit early and not bother with
        // any other validations.
        if (isCertificateSelfSigned &&
            !Options.AllowedCertificateTypes.HasFlag(CertificateTypes.SelfSigned))
        {
            Logger.CertificateRejected("Self signed", clientCertificate.Subject);
            return AuthenticateResults.NoSelfSigned;
        }

        // If we have a chained cert, and they're not allowed, exit early and not bother with
        // any other validations.
        if (!isCertificateSelfSigned &&
            !Options.AllowedCertificateTypes.HasFlag(CertificateTypes.Chained))
        {
            Logger.CertificateRejected("Chained", clientCertificate.Subject);
            return AuthenticateResults.NoChainedCertificates;
        }

        var chainPolicy = BuildChainPolicy(clientCertificate, isCertificateSelfSigned);
        using var chain = new X509Chain
        {
            ChainPolicy = chainPolicy
        };

        var certificateIsValid = chain.Build(clientCertificate);
        if (!certificateIsValid)
        {
            var chainErrors = new List<string>(chain.ChainStatus.Length);
            foreach (var validationFailure in chain.ChainStatus)
            {
                chainErrors.Add($"{validationFailure.Status} {validationFailure.StatusInformation}");
            }
            Logger.CertificateFailedValidation(clientCertificate.Subject, chainErrors);
            return AuthenticateResults.InvalidClientCertificate;
        }

        var certificateValidatedContext = new CertificateValidatedContext(Context, Scheme, Options)
        {
            ClientCertificate = clientCertificate,
            Principal = CreatePrincipal(clientCertificate)
        };

        await Events.CertificateValidated(certificateValidatedContext);

        if (certificateValidatedContext.Result != null)
        {
            return certificateValidatedContext.Result;
        }

        certificateValidatedContext.Success();
        return certificateValidatedContext.Result!;
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        var authenticationChallengedContext = new CertificateChallengeContext(Context, Scheme, Options, properties);
        await Events.Challenge(authenticationChallengedContext);

        if (authenticationChallengedContext.Handled)
        {
            return;
        }

        // Certificate authentication takes place at the connection level. We can't prompt once we're in
        // user code, so the best thing to do is Forbid, not Challenge.
        await HandleForbiddenAsync(properties);
    }

    private X509ChainPolicy BuildChainPolicy(X509Certificate2 certificate, bool isCertificateSelfSigned)
    {
        // Now build the chain validation options.
        X509RevocationFlag revocationFlag = Options.RevocationFlag;
        X509RevocationMode revocationMode = Options.RevocationMode;

        if (isCertificateSelfSigned)
        {
            // Turn off chain validation, because we have a self signed certificate.
            revocationFlag = X509RevocationFlag.EntireChain;
            revocationMode = X509RevocationMode.NoCheck;
        }

        var chainPolicy = new X509ChainPolicy
        {
            RevocationFlag = revocationFlag,
            RevocationMode = revocationMode,
        };

        if (Options.ValidateCertificateUse)
        {
            chainPolicy.ApplicationPolicy.Add(ClientCertificateOid);
        }

        if (isCertificateSelfSigned)
        {
            chainPolicy.VerificationFlags |= X509VerificationFlags.AllowUnknownCertificateAuthority;
            chainPolicy.VerificationFlags |= X509VerificationFlags.IgnoreEndRevocationUnknown;
            chainPolicy.ExtraStore.Add(certificate);
        }
        else
        {
            if (Options.CustomTrustStore != null)
            {
                chainPolicy.CustomTrustStore.AddRange(Options.CustomTrustStore);
            }

            chainPolicy.TrustMode = Options.ChainTrustValidationMode;
        }

        chainPolicy.ExtraStore.AddRange(Options.AdditionalChainCertificates);

        if (!Options.ValidateValidityPeriod)
        {
            chainPolicy.VerificationFlags |= X509VerificationFlags.IgnoreNotTimeValid;
        }

        return chainPolicy;
    }

    private ClaimsPrincipal CreatePrincipal(X509Certificate2 certificate)
    {
        var claims = new List<Claim>();

        var issuer = certificate.Issuer;
        claims.Add(new Claim("issuer", issuer, ClaimValueTypes.String, Options.ClaimsIssuer));

        var thumbprint = certificate.Thumbprint;
        claims.Add(new Claim(ClaimTypes.Thumbprint, thumbprint, ClaimValueTypes.Base64Binary, Options.ClaimsIssuer));

        var value = certificate.SubjectName.Name;
        if (!string.IsNullOrWhiteSpace(value))
        {
            claims.Add(new Claim(ClaimTypes.X500DistinguishedName, value, ClaimValueTypes.String, Options.ClaimsIssuer));
        }

        value = certificate.SerialNumber;
        if (!string.IsNullOrWhiteSpace(value))
        {
            claims.Add(new Claim(ClaimTypes.SerialNumber, value, ClaimValueTypes.String, Options.ClaimsIssuer));
        }

        value = certificate.GetNameInfo(X509NameType.DnsName, false);
        if (!string.IsNullOrWhiteSpace(value))
        {
            claims.Add(new Claim(ClaimTypes.Dns, value, ClaimValueTypes.String, Options.ClaimsIssuer));
        }

        value = certificate.GetNameInfo(X509NameType.SimpleName, false);
        if (!string.IsNullOrWhiteSpace(value))
        {
            claims.Add(new Claim(ClaimTypes.Name, value, ClaimValueTypes.String, Options.ClaimsIssuer));
        }

        value = certificate.GetNameInfo(X509NameType.EmailName, false);
        if (!string.IsNullOrWhiteSpace(value))
        {
            claims.Add(new Claim(ClaimTypes.Email, value, ClaimValueTypes.String, Options.ClaimsIssuer));
        }

        value = certificate.GetNameInfo(X509NameType.UpnName, false);
        if (!string.IsNullOrWhiteSpace(value))
        {
            claims.Add(new Claim(ClaimTypes.Upn, value, ClaimValueTypes.String, Options.ClaimsIssuer));
        }

        value = certificate.GetNameInfo(X509NameType.UrlName, false);
        if (!string.IsNullOrWhiteSpace(value))
        {
            claims.Add(new Claim(ClaimTypes.Uri, value, ClaimValueTypes.String, Options.ClaimsIssuer));
        }

        var identity = new ClaimsIdentity(claims, CertificateAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }
}
