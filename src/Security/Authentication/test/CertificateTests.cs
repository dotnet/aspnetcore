// Copyright (c) Barry Dorrans. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.Certificate.Test
{
    public class ClientCertificateAuthenticationTests
    {

        [Fact]
        public async Task VerifySchemeDefaults()
        {
            var services = new ServiceCollection();
            services.AddAuthentication().AddCertificate();
            var sp = services.BuildServiceProvider();
            var schemeProvider = sp.GetRequiredService<IAuthenticationSchemeProvider>();
            var scheme = await schemeProvider.GetSchemeAsync(CertificateAuthenticationDefaults.AuthenticationScheme);
            Assert.NotNull(scheme);
            Assert.Equal("CertificateAuthenticationHandler", scheme.HandlerType.Name);
            Assert.Null(scheme.DisplayName);
        }

        [Fact]
        public void VerifyIsSelfSignedExtensionMethod()
        {
            Assert.True(Certificates.SelfSignedValidWithNoEku.IsSelfSigned());
        }

        [Fact]
        public async Task VerifyValidSelfSignedWithClientEkuAuthenticates()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    AllowedCertificateTypes = CertificateTypes.SelfSigned,
                    Events = successfulValidationEvents
                },
                Certificates.SelfSignedValidWithClientEku);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task VerifyValidSelfSignedWithNoEkuAuthenticates()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    AllowedCertificateTypes = CertificateTypes.SelfSigned,
                    Events = successfulValidationEvents
                },
                Certificates.SelfSignedValidWithNoEku);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task VerifyValidSelfSignedWithClientEkuFailsWhenSelfSignedCertsNotAllowed()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    AllowedCertificateTypes = CertificateTypes.Chained
                },
                Certificates.SelfSignedValidWithClientEku);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task VerifyValidSelfSignedWithNoEkuFailsWhenSelfSignedCertsNotAllowed()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    AllowedCertificateTypes = CertificateTypes.Chained,
                    Events = successfulValidationEvents
                },
                Certificates.SelfSignedValidWithNoEku);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task VerifyValidSelfSignedWithServerFailsEvenIfSelfSignedCertsAreAllowed()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    AllowedCertificateTypes = CertificateTypes.SelfSigned,
                    Events = successfulValidationEvents
                },
                Certificates.SelfSignedValidWithServerEku);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task VerifyValidSelfSignedWithServerPassesWhenSelfSignedCertsAreAllowedAndPurposeValidationIsOff()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    AllowedCertificateTypes = CertificateTypes.SelfSigned,
                    ValidateCertificateUse = false,
                    Events = successfulValidationEvents
                },
                Certificates.SelfSignedValidWithServerEku);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task VerifyValidSelfSignedWithServerFailsPurposeValidationIsOffButSelfSignedCertsAreNotAllowed()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    AllowedCertificateTypes = CertificateTypes.Chained,
                    ValidateCertificateUse = false,
                    Events = successfulValidationEvents
                },
                Certificates.SelfSignedValidWithServerEku);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task VerifyExpiredSelfSignedFails()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    AllowedCertificateTypes = CertificateTypes.SelfSigned,
                    ValidateCertificateUse = false,
                    Events = successfulValidationEvents
                },
                Certificates.SelfSignedExpired);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task VerifyExpiredSelfSignedPassesIfDateRangeValidationIsDisabled()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    AllowedCertificateTypes = CertificateTypes.SelfSigned,
                    ValidateValidityPeriod = false,
                    Events = successfulValidationEvents
                },
                Certificates.SelfSignedExpired);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task VerifyNotYetValidSelfSignedFails()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    AllowedCertificateTypes = CertificateTypes.SelfSigned,
                    ValidateCertificateUse = false,
                    Events = successfulValidationEvents
                },
                Certificates.SelfSignedNotYetValid);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task VerifyNotYetValidSelfSignedPassesIfDateRangeValidationIsDisabled()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    AllowedCertificateTypes = CertificateTypes.SelfSigned,
                    ValidateValidityPeriod = false,
                    Events = successfulValidationEvents
                },
                Certificates.SelfSignedNotYetValid);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task VerifyFailingInTheValidationEventReturnsForbidden()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    ValidateCertificateUse = false,
                    Events = failedValidationEvents
                },
                Certificates.SelfSignedValidWithServerEku);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DoingNothingInTheValidationEventReturnsOK()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    AllowedCertificateTypes = CertificateTypes.SelfSigned,
                    ValidateCertificateUse = false,
                    Events = unprocessedValidationEvents
                },
                Certificates.SelfSignedValidWithServerEku);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task VerifyNotSendingACertificateEndsUpInForbidden()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    Events = successfulValidationEvents
                });

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task VerifyUntrustedClientCertEndsUpInForbidden()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    Events = successfulValidationEvents
                }, Certificates.SignedClient);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task VerifyClientCertWithUntrustedRootAndTrustedChainEndsUpInForbidden()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    Events = successfulValidationEvents,
                    CustomTrustStore = new X509Certificate2Collection() { Certificates.SignedSecondaryRoot },
                    ChainTrustValidationMode = X509ChainTrustMode.CustomRootTrust,
                    RevocationMode = X509RevocationMode.NoCheck
                }, Certificates.SignedClient);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task VerifyValidClientCertWithTrustedChainAuthenticates()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    Events = successfulValidationEvents,
                    CustomTrustStore = new X509Certificate2Collection() { Certificates.SelfSignedPrimaryRoot, Certificates.SignedSecondaryRoot },
                    ChainTrustValidationMode = X509ChainTrustMode.CustomRootTrust,
                    RevocationMode = X509RevocationMode.NoCheck
                }, Certificates.SignedClient);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task VerifyHeaderIsUsedIfCertIsNotPresent()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    AllowedCertificateTypes = CertificateTypes.SelfSigned,
                    Events = successfulValidationEvents
                },
                wireUpHeaderMiddleware: true);

            var client = server.CreateClient();
            client.DefaultRequestHeaders.Add("X-Client-Cert", Convert.ToBase64String(Certificates.SelfSignedValidWithNoEku.RawData));
            var response = await client.GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task VerifyHeaderEncodedCertFailsOnBadEncoding()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    Events = successfulValidationEvents
                },
                wireUpHeaderMiddleware: true);

            var client = server.CreateClient();
            client.DefaultRequestHeaders.Add("X-Client-Cert", "OOPS" + Convert.ToBase64String(Certificates.SelfSignedValidWithNoEku.RawData));
            var response = await client.GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task VerifySettingTheAzureHeaderOnTheForwarderOptionsWorks()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    AllowedCertificateTypes = CertificateTypes.SelfSigned,
                    Events = successfulValidationEvents
                },
                wireUpHeaderMiddleware: true,
                headerName: "X-ARR-ClientCert");

            var client = server.CreateClient();
            client.DefaultRequestHeaders.Add("X-ARR-ClientCert", Convert.ToBase64String(Certificates.SelfSignedValidWithNoEku.RawData));
            var response = await client.GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task VerifyACustomHeaderFailsIfTheHeaderIsNotPresent()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    Events = successfulValidationEvents
                },
                wireUpHeaderMiddleware: true,
                headerName: "X-ARR-ClientCert");

            var client = server.CreateClient();
            client.DefaultRequestHeaders.Add("random-Weird-header", Convert.ToBase64String(Certificates.SelfSignedValidWithNoEku.RawData));
            var response = await client.GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task VerifyNoEventWireupWithAValidCertificateCreatesADefaultUser()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    AllowedCertificateTypes = CertificateTypes.SelfSigned
                },
                Certificates.SelfSignedValidWithNoEku);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            XElement responseAsXml = null;
            if (response.Content != null &&
                response.Content.Headers.ContentType != null &&
                response.Content.Headers.ContentType.MediaType == "text/xml")
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                responseAsXml = XElement.Parse(responseContent);
            }

            Assert.NotNull(responseAsXml);

            // There should always be an Issuer and a Thumbprint.
            var actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == "issuer");
            Assert.Single(actual);
            Assert.Equal(Certificates.SelfSignedValidWithNoEku.Issuer, actual.First().Value);

            actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.Thumbprint);
            Assert.Single(actual);
            Assert.Equal(Certificates.SelfSignedValidWithNoEku.Thumbprint, actual.First().Value);

            // Now the optional ones
            if (!string.IsNullOrEmpty(Certificates.SelfSignedValidWithNoEku.SubjectName.Name))
            {
                actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.X500DistinguishedName);
                if (actual.Count() > 0)
                {
                    Assert.Single(actual);
                    Assert.Equal(Certificates.SelfSignedValidWithNoEku.SubjectName.Name, actual.First().Value);
                }
            }

            if (!string.IsNullOrEmpty(Certificates.SelfSignedValidWithNoEku.SerialNumber))
            {
                actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.SerialNumber);
                if (actual.Count() > 0)
                {
                    Assert.Single(actual);
                    Assert.Equal(Certificates.SelfSignedValidWithNoEku.SerialNumber, actual.First().Value);
                }
            }

            if (!string.IsNullOrEmpty(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.DnsName, false)))
            {
                actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.Dns);
                if (actual.Count() > 0)
                {
                    Assert.Single(actual);
                    Assert.Equal(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.DnsName, false), actual.First().Value);
                }
            }

            if (!string.IsNullOrEmpty(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.EmailName, false)))
            {
                actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.Email);
                if (actual.Count() > 0)
                {
                    Assert.Single(actual);
                    Assert.Equal(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.EmailName, false), actual.First().Value);
                }
            }

            if (!string.IsNullOrEmpty(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.SimpleName, false)))
            {
                actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.Name);
                if (actual.Count() > 0)
                {
                    Assert.Single(actual);
                    Assert.Equal(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.SimpleName, false), actual.First().Value);
                }
            }

            if (!string.IsNullOrEmpty(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.UpnName, false)))
            {
                actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.Upn);
                if (actual.Count() > 0)
                {
                    Assert.Single(actual);
                    Assert.Equal(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.UpnName, false), actual.First().Value);
                }
            }

            if (!string.IsNullOrEmpty(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.UrlName, false)))
            {
                actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.Uri);
                if (actual.Count() > 0)
                {
                    Assert.Single(actual);
                    Assert.Equal(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.UrlName, false), actual.First().Value);
                }
            }
        }

        [Fact]
        public async Task VerifyValidationEventPrincipalIsPropogated()
        {
            const string Expected = "John Doe";

            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    AllowedCertificateTypes = CertificateTypes.SelfSigned,
                    Events = new CertificateAuthenticationEvents
                    {
                        OnCertificateValidated = context =>
                        {
                            // Make sure we get the validated principal
                            Assert.NotNull(context.Principal);
                            var claims = new[]
                            {
                                new Claim(ClaimTypes.Name, Expected, ClaimValueTypes.String, context.Options.ClaimsIssuer)
                            };

                            context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, context.Scheme.Name));
                            context.Success();
                            return Task.CompletedTask;
                        }
                    }
                },
                Certificates.SelfSignedValidWithNoEku);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            XElement responseAsXml = null;
            if (response.Content != null &&
                response.Content.Headers.ContentType != null &&
                response.Content.Headers.ContentType.MediaType == "text/xml")
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                responseAsXml = XElement.Parse(responseContent);
            }

            Assert.NotNull(responseAsXml);
            var actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.Name);
            Assert.Single(actual);
            Assert.Equal(Expected, actual.First().Value);
            Assert.Single(responseAsXml.Elements("claim"));
        }

        private static TestServer CreateServer(
            CertificateAuthenticationOptions configureOptions,
            X509Certificate2 clientCertificate = null,
            Func<HttpContext, bool> handler = null,
            Uri baseAddress = null,
            bool wireUpHeaderMiddleware = false,
            string headerName = "")
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.Use((context, next) =>
                    {
                        if (clientCertificate != null)
                        {
                            context.Connection.ClientCertificate = clientCertificate;
                        }
                        return next();
                    });


                    if (wireUpHeaderMiddleware)
                    {
                        app.UseCertificateForwarding();
                    }

                    app.UseAuthentication();

                    app.Use(async (context, next) =>
                    {
                        var request = context.Request;
                        var response = context.Response;

                        var authenticationResult = await context.AuthenticateAsync();

                        if (authenticationResult.Succeeded)
                        {
                            response.StatusCode = (int)HttpStatusCode.OK;
                            response.ContentType = "text/xml";

                            await response.WriteAsync("<claims>");
                            foreach (Claim claim in context.User.Claims)
                            {
                                await response.WriteAsync($"<claim Type=\"{claim.Type}\" Issuer=\"{claim.Issuer}\">{claim.Value}</claim>");
                            }
                            await response.WriteAsync("</claims>");
                        }
                        else
                        {
                            await context.ChallengeAsync();
                        }
                    });
                })
            .ConfigureServices(services =>
            {
                if (configureOptions != null)
                {
                    services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme).AddCertificate(options =>
                    {
                        options.CustomTrustStore = configureOptions.CustomTrustStore;
                        options.ChainTrustValidationMode = configureOptions.ChainTrustValidationMode;
                        options.AllowedCertificateTypes = configureOptions.AllowedCertificateTypes;
                        options.Events = configureOptions.Events;
                        options.ValidateCertificateUse = configureOptions.ValidateCertificateUse;
                        options.RevocationFlag = configureOptions.RevocationFlag;
                        options.RevocationMode = configureOptions.RevocationMode;
                        options.ValidateValidityPeriod = configureOptions.ValidateValidityPeriod;
                    });
                }
                else
                {
                    services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme).AddCertificate();
                }

                if (wireUpHeaderMiddleware && !string.IsNullOrEmpty(headerName))
                {
                    services.AddCertificateForwarding(options =>
                    {
                        options.CertificateHeader = headerName;
                    });
                }
            });

            var server = new TestServer(builder)
            {
                BaseAddress = baseAddress
            };

            return server;
        }

        private CertificateAuthenticationEvents successfulValidationEvents = new CertificateAuthenticationEvents()
        {
            OnCertificateValidated = context =>
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, context.ClientCertificate.Subject, ClaimValueTypes.String, context.Options.ClaimsIssuer),
                    new Claim(ClaimTypes.Name, context.ClientCertificate.Subject, ClaimValueTypes.String, context.Options.ClaimsIssuer)
                };

                context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, context.Scheme.Name));
                context.Success();
                return Task.CompletedTask;
            }
        };

        private CertificateAuthenticationEvents failedValidationEvents = new CertificateAuthenticationEvents()
        {
            OnCertificateValidated = context =>
            {
                context.Fail("Not validated");
                return Task.CompletedTask;
            }
        };

        private CertificateAuthenticationEvents unprocessedValidationEvents = new CertificateAuthenticationEvents()
        {
            OnCertificateValidated = context =>
            {
                return Task.CompletedTask;
            }
        };

        private static class Certificates
        {
            public static X509Certificate2 SelfSignedPrimaryRoot { get; private set; } =
                new X509Certificate2(GetFullyQualifiedFilePath("validSelfSignedPrimaryRootCertificate.cer"));

            public static X509Certificate2 SignedSecondaryRoot { get; private set; } =
                new X509Certificate2(GetFullyQualifiedFilePath("validSignedSecondaryRootCertificate.cer"));

            public static X509Certificate2 SignedClient { get; private set; } =
                new X509Certificate2(GetFullyQualifiedFilePath("validSignedClientCertificate.cer"));

            public static X509Certificate2 SelfSignedValidWithClientEku { get; private set; } =
                new X509Certificate2(GetFullyQualifiedFilePath("validSelfSignedClientEkuCertificate.cer"));

            public static X509Certificate2 SelfSignedValidWithNoEku { get; private set; } =
                new X509Certificate2(GetFullyQualifiedFilePath("validSelfSignedNoEkuCertificate.cer"));

            public static X509Certificate2 SelfSignedValidWithServerEku { get; private set; } =
                new X509Certificate2(GetFullyQualifiedFilePath("validSelfSignedServerEkuCertificate.cer"));

            public static X509Certificate2 SelfSignedNotYetValid { get; private set; } =
                new X509Certificate2(GetFullyQualifiedFilePath("selfSignedNoEkuCertificateNotValidYet.cer"));

            public static X509Certificate2 SelfSignedExpired { get; private set; } =
                new X509Certificate2(GetFullyQualifiedFilePath("selfSignedNoEkuCertificateExpired.cer"));

            private static string GetFullyQualifiedFilePath(string filename)
            {
                var filePath = Path.Combine(AppContext.BaseDirectory, filename);
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException(filePath);
                }
                return filePath;
            }
        }
    }
}

