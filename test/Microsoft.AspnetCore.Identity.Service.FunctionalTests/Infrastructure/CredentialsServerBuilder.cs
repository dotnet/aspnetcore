using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Identity.OpenIdConnect.WebSite;
using Identity.OpenIdConnect.WebSite.Identity.Data;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.Service.IntegratedWebClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspnetCore.Identity.Service.FunctionalTests
{
    public class CredentialsServerBuilder
    {
        private readonly DelegatingHandler _loopBackHandler = new LoopBackHandler();

        public CredentialsServerBuilder()
        {
            Server = new MvcWebApplicationBuilder<Startup>()
                .UseSolutionRelativeContentRoot(@".\test\WebSites\Identity.OpenIdConnect.WebSite")
                .UseApplicationAssemblies();
        }

        public CredentialsServerBuilder ConfigureReferenceData(Action<ReferenceData> action)
        {
            var referenceData = new ReferenceData();
            action(referenceData);
            Server.ConfigureBeforeStartup(s => s.TryAddSingleton(referenceData));

            return this;
        }

        public CredentialsServerBuilder ConfigureInMemoryEntityFrameworkStorage(string dbName = "test")
        {
            Server.ConfigureBeforeStartup(services =>
            {
                services.TryAddEnumerable(ServiceDescriptor.Transient<IStartupFilter, EntityFrameworkSeedReferenceData>());
                services.AddDbContext<IdentityServiceDbContext>(options =>
                    options.UseInMemoryDatabase("test", memoryOptions => { }));
            });

            return this;
        }

        public CredentialsServerBuilder ConfigureMvcAutomaticSignIn()
        {
            Server.ConfigureBeforeStartup(s => s.Configure<MvcOptions>(o => o.Filters.Add(new AutoSignInFilter())));
            return this;
        }

        public CredentialsServerBuilder ConfigureOpenIdConnectClient(Action<OpenIdConnectOptions> action)
        {
            Server.ConfigureAfterStartup(services =>
            {
                services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
                {
                    options.BackchannelHttpHandler = _loopBackHandler;
                    options.CorrelationCookie.Path = "/";
                    options.NonceCookie.Path = "/";
                });

                services.Configure(OpenIdConnectDefaults.AuthenticationScheme, action);
            });

            return this;
        }

        public CredentialsServerBuilder ConfigureIntegratedClient(string clientId)
        {
            Server.ConfigureAfterStartup(services =>
            {
                services.Configure<IntegratedWebClientOptions>(options => options.ClientId = clientId);
            });

            return this;
        }

        public CredentialsServerBuilder EnsureDeveloperCertificate()
        {
            try
            {
                using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.ReadOnly);
                    var certificates = store.Certificates.OfType<X509Certificate2>().ToList();
                    var development = certificates.FirstOrDefault(c => c.Subject == "CN=Identity.Development" &&
                    c.GetRSAPrivateKey() != null &&
                    c.NotAfter > DateTimeOffset.UtcNow);

                    if (development == null)
                    {
                        CreateDevelopmentCertificate();
                    }
                }
            }
            catch (Exception)
            {
                throw new InvalidOperationException("There was an error ensuring the presence of the developer certificate.");
            }

            return this;

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
                };
            }
#elif NET461
#else
#error The target frameworks need to be updated.
#endif
            }
        }

        public MvcWebApplicationBuilder<Startup> Server { get; }

        public HttpClient Build()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

            var host = Server.Build();
            host.BaseAddress = new Uri("https://localhost");

            var clientHandler = host.CreateHandler();
            _loopBackHandler.InnerHandler = clientHandler;

            var cookieHandler = new CookieContainerHandler(clientHandler);

            var client = new HttpClient(cookieHandler);
            client.BaseAddress = new Uri("https://localhost");

            return client;
        }
    }
}
