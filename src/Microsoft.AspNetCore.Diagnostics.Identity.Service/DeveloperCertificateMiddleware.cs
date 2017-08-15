// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Diagnostics.Identity.Service
{
    public class DeveloperCertificateMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHostingEnvironment _environment;
        private readonly IOptions<DeveloperCertificateOptions> _options;
        private readonly ITimeStampManager _timeStampManager;
        private readonly IConfiguration _configuration;

        public DeveloperCertificateMiddleware(
            RequestDelegate next,
            IOptions<DeveloperCertificateOptions> options,
            ITimeStampManager timeStampManager,
            IHostingEnvironment environment,
            IConfiguration configuration)
        {
            _next = next;
            _options = options;
            _environment = environment;
            _timeStampManager = timeStampManager;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {

            var credentialsProvider = context.RequestServices.GetService<DeveloperCertificateSigningCredentialsSource>();
            if (credentialsProvider == null)
            {
                await _next(context);
                return;
            }
            if (_environment.IsDevelopment() &&
                context.Request.Path.Equals(_options.Value.ListeningEndpoint))
            {
                if (context.Request.Method.Equals(HttpMethods.Get))
                {
                    var credentials = await credentialsProvider.GetCredentials();
                    bool hasDevelopmentCertificate = await IsDevelopmentCertificateConfiguredAndValid();
                    var foundDeveloperCertificate = FoundDeveloperCertificate();
                    if (!foundDeveloperCertificate || !hasDevelopmentCertificate)
                    {
                        var page = new DeveloperCertificateErrorPage();
                        page.Model = new DeveloperCertificateViewModel()
                        {
                            CertificateExists = foundDeveloperCertificate,
                            CertificateIsInvalid = !hasDevelopmentCertificate,
                            Options = _options.Value
                        };

                        await page.ExecuteAsync(context);
                        return;
                    }
                }
                if (context.Request.Method.Equals(HttpMethods.Post))
                {
                    CreateDevelopmentCertificate();
                    return;
                }
            }

            await _next(context);
            void CreateDevelopmentCertificate()
            {
#if NETCOREAPP2_0
                using (var rsa = RSA.Create(2048))
                {
                    var signingRequest = new CertificateRequest(
                        new X500DistinguishedName("CN=Identity.Development"), rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    var enhacedKeyUsage = new OidCollection();
                    enhacedKeyUsage.Add(new Oid("1.3.6.1.5.5.7.3.1", "Server Authentication"));
                    signingRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(enhacedKeyUsage, critical: true));
                    signingRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, critical: true));

                    var certificate = signingRequest.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1));
                    certificate.FriendlyName = "Identity Service developer certificate";

                    // We need to take this step so that the key gets persisted.
                    var export = certificate.Export(X509ContentType.Pkcs12, "");
                    var imported = new X509Certificate2(export, "", X509KeyStorageFlags.PersistKeySet);
                    Array.Clear(export, 0, export.Length);

                    using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
                    {
                        store.Open(OpenFlags.ReadWrite);
                        store.Add(imported);
                        store.Close();

                        context.Response.StatusCode = StatusCodes.Status204NoContent;
                    };
                }
#elif NETSTANDARD2_0
#else
#error The target frameworks need to be updated.
#endif
            }

            bool FoundDeveloperCertificate()
            {
                using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.ReadOnly);
                    var developmentCertificate = store.Certificates.Find(
                        X509FindType.FindBySubjectName,
                        "Identity.Development",
                        validOnly: false);

                    store.Close();
                    return developmentCertificate.OfType<X509Certificate2>().Any();
                }
            }

            async Task<bool> IsDevelopmentCertificateConfiguredAndValid()
            {
                var certificates = await credentialsProvider.GetCredentials();
                return certificates.Any(
                    c => _timeStampManager.IsValidPeriod(c.NotBefore, c.Expires) &&
                        c.Credentials.Key is X509SecurityKey key &&
                        key.Certificate.Subject.Equals("CN=Identity.Development"));
            }
        }
    }
}
