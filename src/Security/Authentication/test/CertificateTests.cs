// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.Certificate.Test;

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
        using var host = await CreateHost(
            new CertificateAuthenticationOptions
            {
                AllowedCertificateTypes = CertificateTypes.SelfSigned,
                Events = successfulValidationEvents
            },
            Certificates.SelfSignedValidWithClientEku);

        using var server = host.GetTestServer();
        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VerifyValidSelfSignedWithNoEkuAuthenticates()
    {
        using var host = await CreateHost(
            new CertificateAuthenticationOptions
            {
                AllowedCertificateTypes = CertificateTypes.SelfSigned,
                Events = successfulValidationEvents
            },
            Certificates.SelfSignedValidWithNoEku);

        using var server = host.GetTestServer();
        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VerifyValidSelfSignedWithClientEkuFailsWhenSelfSignedCertsNotAllowed()
    {
        using var host = await CreateHost(
            new CertificateAuthenticationOptions
            {
                AllowedCertificateTypes = CertificateTypes.Chained
            },
            Certificates.SelfSignedValidWithClientEku);

        using var server = host.GetTestServer();
        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VerifyValidSelfSignedWithNoEkuFailsWhenSelfSignedCertsNotAllowed()
    {
        using var host = await CreateHost(
            new CertificateAuthenticationOptions
            {
                AllowedCertificateTypes = CertificateTypes.Chained,
                Events = successfulValidationEvents
            },
            Certificates.SelfSignedValidWithNoEku);

        using var server = host.GetTestServer();
        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VerifyValidSelfSignedWithServerFailsEvenIfSelfSignedCertsAreAllowed()
    {
        using var host = await CreateHost(
            new CertificateAuthenticationOptions
            {
                AllowedCertificateTypes = CertificateTypes.SelfSigned,
                Events = successfulValidationEvents
            },
            Certificates.SelfSignedValidWithServerEku);

        using var server = host.GetTestServer();
        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VerifyValidSelfSignedWithServerPassesWhenSelfSignedCertsAreAllowedAndPurposeValidationIsOff()
    {
        using var host = await CreateHost(
            new CertificateAuthenticationOptions
            {
                AllowedCertificateTypes = CertificateTypes.SelfSigned,
                ValidateCertificateUse = false,
                Events = successfulValidationEvents
            },
            Certificates.SelfSignedValidWithServerEku);

        using var server = host.GetTestServer();
        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VerifyValidSelfSignedWithServerFailsPurposeValidationIsOffButSelfSignedCertsAreNotAllowed()
    {
        using var host = await CreateHost(
            new CertificateAuthenticationOptions
            {
                AllowedCertificateTypes = CertificateTypes.Chained,
                ValidateCertificateUse = false,
                Events = successfulValidationEvents
            },
            Certificates.SelfSignedValidWithServerEku);

        using var server = host.GetTestServer();
        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VerifyExpiredSelfSignedFails()
    {
        using var host = await CreateHost(
            new CertificateAuthenticationOptions
            {
                AllowedCertificateTypes = CertificateTypes.SelfSigned,
                ValidateCertificateUse = false,
                Events = successfulValidationEvents
            },
            Certificates.SelfSignedExpired);

        using var server = host.GetTestServer();
        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VerifyExpiredSelfSignedPassesIfDateRangeValidationIsDisabled()
    {
        using var host = await CreateHost(
            new CertificateAuthenticationOptions
            {
                AllowedCertificateTypes = CertificateTypes.SelfSigned,
                ValidateValidityPeriod = false,
                Events = successfulValidationEvents
            },
            Certificates.SelfSignedExpired);

        using var server = host.GetTestServer();
        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [ConditionalFact]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/32813")]
    public async Task VerifyNotYetValidSelfSignedFails()
    {
        using var host = await CreateHost(
            new CertificateAuthenticationOptions
            {
                AllowedCertificateTypes = CertificateTypes.SelfSigned,
                ValidateCertificateUse = false,
                Events = successfulValidationEvents
            },
            Certificates.SelfSignedNotYetValid);

        using var server = host.GetTestServer();
        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VerifyNotYetValidSelfSignedPassesIfDateRangeValidationIsDisabled()
    {
        using var host = await CreateHost(
            new CertificateAuthenticationOptions
            {
                AllowedCertificateTypes = CertificateTypes.SelfSigned,
                ValidateValidityPeriod = false,
                Events = successfulValidationEvents
            },
            Certificates.SelfSignedNotYetValid);

        using var server = host.GetTestServer();
        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VerifyFailingInTheValidationEventReturnsForbidden()
    {
        using var host = await CreateHost(
            new CertificateAuthenticationOptions
            {
                ValidateCertificateUse = false,
                Events = failedValidationEvents
            },
            Certificates.SelfSignedValidWithServerEku);

        using var server = host.GetTestServer();
        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DoingNothingInTheValidationEventReturnsOK()
    {
        using var host = await CreateHost(
            new CertificateAuthenticationOptions
            {
                AllowedCertificateTypes = CertificateTypes.SelfSigned,
                ValidateCertificateUse = false,
                Events = unprocessedValidationEvents
            },
            Certificates.SelfSignedValidWithServerEku);

        using var server = host.GetTestServer();
        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VerifyNotSendingACertificateEndsUpInForbidden()
    {
        using var host = await CreateHost(
            new CertificateAuthenticationOptions
            {
                Events = successfulValidationEvents
            });

        using var server = host.GetTestServer();
        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VerifyUntrustedClientCertEndsUpInForbidden()
    {
        using var host = await CreateHost(
            new CertificateAuthenticationOptions
            {
                Events = successfulValidationEvents
            }, Certificates.SignedClient);

        using var server = host.GetTestServer();
        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VerifyValidationFailureCanBeHandled()
    {
        var failCalled = false;
        using var host = await CreateHost(
            new CertificateAuthenticationOptions
            {
                Events = new CertificateAuthenticationEvents()
                {
                    OnAuthenticationFailed = context =>
                    {
                        context.Fail("Validation failed: " + context.Exception);
                        failCalled = true;
                        return Task.CompletedTask;
                    }
                }
            }, Certificates.SignedClient);

        using var server = host.GetTestServer();
        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.True(failCalled);
    }

    [Fact]
    public async Task VerifyClientCertWithUntrustedRootAndTrustedChainEndsUpInForbidden()
    {
        using var host = await CreateHost(
            new CertificateAuthenticationOptions
            {
                Events = successfulValidationEvents,
                CustomTrustStore = new X509Certificate2Collection() { Certificates.SignedSecondaryRoot },
                ChainTrustValidationMode = X509ChainTrustMode.CustomRootTrust,
                RevocationMode = X509RevocationMode.NoCheck
            }, Certificates.SignedClient);

        using var server = host.GetTestServer();
        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact(Skip = "https://github.com/dotnet/aspnetcore/issues/39669")]
    public async Task VerifyValidClientCertWithTrustedChainAuthenticates()
    {
        using var host = await CreateHost(
            new CertificateAuthenticationOptions
            {
                Events = successfulValidationEvents,
                CustomTrustStore = new X509Certificate2Collection() { Certificates.SelfSignedPrimaryRoot, Certificates.SignedSecondaryRoot },
                ChainTrustValidationMode = X509ChainTrustMode.CustomRootTrust,
                RevocationMode = X509RevocationMode.NoCheck
            }, Certificates.SignedClient);

        using var server = host.GetTestServer();
        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact(Skip = "https://github.com/dotnet/aspnetcore/issues/39669")]
    public async Task VerifyValidClientCertWithAdditionalCertificatesAuthenticates()
    {
        using var host = await CreateHost(
            new CertificateAuthenticationOptions
            {
                Events = successfulValidationEvents,
                ChainTrustValidationMode = X509ChainTrustMode.CustomRootTrust,
                CustomTrustStore = new X509Certificate2Collection() { Certificates.SelfSignedPrimaryRoot, },
                AdditionalChainCertificates = new X509Certificate2Collection() { Certificates.SignedSecondaryRoot },
                RevocationMode = X509RevocationMode.NoCheck
            }, Certificates.SignedClient);

        using var server = host.GetTestServer();
        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VerifyValidClientCertFailsWithoutAdditionalCertificatesAuthenticates()
    {
        using var host = await CreateHost(
            new CertificateAuthenticationOptions
            {
                Events = successfulValidationEvents,
                ChainTrustValidationMode = X509ChainTrustMode.CustomRootTrust,
                CustomTrustStore = new X509Certificate2Collection() { Certificates.SelfSignedPrimaryRoot, },
                RevocationMode = X509RevocationMode.NoCheck
            }, Certificates.SignedClient);

        using var server = host.GetTestServer();
        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VerifyHeaderIsUsedIfCertIsNotPresent()
    {
        using var host = await CreateHost(
            new CertificateAuthenticationOptions
            {
                AllowedCertificateTypes = CertificateTypes.SelfSigned,
                Events = successfulValidationEvents
            },
            wireUpHeaderMiddleware: true);

        using var server = host.GetTestServer();
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Add("X-Client-Cert", Convert.ToBase64String(Certificates.SelfSignedValidWithNoEku.RawData));
        var response = await client.GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VerifyHeaderEncodedCertFailsOnBadEncoding()
    {
        using var host = await CreateHost(
            new CertificateAuthenticationOptions
            {
                Events = successfulValidationEvents
            },
            wireUpHeaderMiddleware: true);

        using var server = host.GetTestServer();
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Add("X-Client-Cert", "OOPS" + Convert.ToBase64String(Certificates.SelfSignedValidWithNoEku.RawData));
        var response = await client.GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VerifySettingTheAzureHeaderOnTheForwarderOptionsWorks()
    {
        using var host = await CreateHost(
            new CertificateAuthenticationOptions
            {
                AllowedCertificateTypes = CertificateTypes.SelfSigned,
                Events = successfulValidationEvents
            },
            wireUpHeaderMiddleware: true,
            headerName: "X-ARR-ClientCert");

        using var server = host.GetTestServer();
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Add("X-ARR-ClientCert", Convert.ToBase64String(Certificates.SelfSignedValidWithNoEku.RawData));
        var response = await client.GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VerifyACustomHeaderFailsIfTheHeaderIsNotPresent()
    {
        using var host = await CreateHost(
            new CertificateAuthenticationOptions
            {
                Events = successfulValidationEvents
            },
            wireUpHeaderMiddleware: true,
            headerName: "X-ARR-ClientCert");

        using var server = host.GetTestServer();
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Add("random-Weird-header", Convert.ToBase64String(Certificates.SelfSignedValidWithNoEku.RawData));
        var response = await client.GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VerifyNoEventWireupWithAValidCertificateCreatesADefaultUser()
    {
        using var host = await CreateHost(
            new CertificateAuthenticationOptions
            {
                AllowedCertificateTypes = CertificateTypes.SelfSigned
            },
            Certificates.SelfSignedValidWithNoEku);

        using var server = host.GetTestServer();
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
            if (actual.Any())
            {
                Assert.Single(actual);
                Assert.Equal(Certificates.SelfSignedValidWithNoEku.SubjectName.Name, actual.First().Value);
            }
        }

        if (!string.IsNullOrEmpty(Certificates.SelfSignedValidWithNoEku.SerialNumber))
        {
            actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.SerialNumber);
            if (actual.Any())
            {
                Assert.Single(actual);
                Assert.Equal(Certificates.SelfSignedValidWithNoEku.SerialNumber, actual.First().Value);
            }
        }

        if (!string.IsNullOrEmpty(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.DnsName, false)))
        {
            actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.Dns);
            if (actual.Any())
            {
                Assert.Single(actual);
                Assert.Equal(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.DnsName, false), actual.First().Value);
            }
        }

        if (!string.IsNullOrEmpty(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.EmailName, false)))
        {
            actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.Email);
            if (actual.Any())
            {
                Assert.Single(actual);
                Assert.Equal(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.EmailName, false), actual.First().Value);
            }
        }

        if (!string.IsNullOrEmpty(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.SimpleName, false)))
        {
            actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.Name);
            if (actual.Any())
            {
                Assert.Single(actual);
                Assert.Equal(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.SimpleName, false), actual.First().Value);
            }
        }

        if (!string.IsNullOrEmpty(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.UpnName, false)))
        {
            actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.Upn);
            if (actual.Any())
            {
                Assert.Single(actual);
                Assert.Equal(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.UpnName, false), actual.First().Value);
            }
        }

        if (!string.IsNullOrEmpty(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.UrlName, false)))
        {
            actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.Uri);
            if (actual.Any())
            {
                Assert.Single(actual);
                Assert.Equal(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.UrlName, false), actual.First().Value);
            }
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task VerifyValidationResultCanBeCached(bool cache)
    {
        const string Expected = "John Doe";
        var validationCount = 0;

        using var host = await CreateHost(
            new CertificateAuthenticationOptions
            {
                AllowedCertificateTypes = CertificateTypes.SelfSigned,
                Events = new CertificateAuthenticationEvents
                {
                    OnCertificateValidated = context =>
                    {
                        validationCount++;

                        // Make sure we get the validated principal
                        Assert.NotNull(context.Principal);

                        var claims = new[]
                        {
                                new Claim(ClaimTypes.Name, Expected, ClaimValueTypes.String, context.Options.ClaimsIssuer),
                                new Claim("ValidationCount", validationCount.ToString(CultureInfo.InvariantCulture), ClaimValueTypes.String, context.Options.ClaimsIssuer)
                        };

                        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, context.Scheme.Name));
                        context.Success();
                        return Task.CompletedTask;
                    }
                }
            },
            Certificates.SelfSignedValidWithNoEku, null, null, false, "", cache);

        using var server = host.GetTestServer();
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
        var name = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.Name);
        Assert.Single(name);
        Assert.Equal(Expected, name.First().Value);
        var count = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == "ValidationCount");
        Assert.Single(count);
        Assert.Equal("1", count.First().Value);

        // Second request should not trigger validation if caching
        response = await server.CreateClient().GetAsync("https://example.com/");
        responseAsXml = null;
        if (response.Content != null &&
            response.Content.Headers.ContentType != null &&
            response.Content.Headers.ContentType.MediaType == "text/xml")
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            responseAsXml = XElement.Parse(responseContent);
        }

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        name = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.Name);
        Assert.Single(name);
        Assert.Equal(Expected, name.First().Value);
        count = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == "ValidationCount");
        Assert.Single(count);
        var expected = cache ? "1" : "2";
        Assert.Equal(expected, count.First().Value);
    }

    [Fact]
    public async Task VerifyValidationEventPrincipalIsPropogated()
    {
        const string Expected = "John Doe";

        using var host = await CreateHost(
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

        using var server = host.GetTestServer();
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task VerifyValidationResultNeverCachedAfter30Min(bool cache)
    {
        const string Expected = "John Doe";
        var validationCount = 0;
        var clock = new TestClock();
        clock.UtcNow = DateTime.UtcNow;

        using var host = await CreateHost(
            new CertificateAuthenticationOptions
            {
                AllowedCertificateTypes = CertificateTypes.SelfSigned,
                Events = new CertificateAuthenticationEvents
                {
                    OnCertificateValidated = context =>
                    {
                        validationCount++;

                        // Make sure we get the validated principal
                        Assert.NotNull(context.Principal);

                        var claims = new[]
                        {
                            new Claim(ClaimTypes.Name, Expected, ClaimValueTypes.String, context.Options.ClaimsIssuer),
                            new Claim("ValidationCount", validationCount.ToString(CultureInfo.InvariantCulture), ClaimValueTypes.String, context.Options.ClaimsIssuer)
                        };

                        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, context.Scheme.Name));
                        context.Success();
                        return Task.CompletedTask;
                    }
                }
            },
            Certificates.SelfSignedValidWithNoEku, null, null, false, "", cache, clock);

        using var server = host.GetTestServer();
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
        var name = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.Name);
        Assert.Single(name);
        Assert.Equal(Expected, name.First().Value);
        var count = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == "ValidationCount");
        Assert.Single(count);
        Assert.Equal("1", count.First().Value);

        // Second request should not trigger validation if caching
        response = await server.CreateClient().GetAsync("https://example.com/");
        responseAsXml = null;
        if (response.Content != null &&
            response.Content.Headers.ContentType != null &&
            response.Content.Headers.ContentType.MediaType == "text/xml")
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            responseAsXml = XElement.Parse(responseContent);
        }

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        name = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.Name);
        Assert.Single(name);
        Assert.Equal(Expected, name.First().Value);
        count = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == "ValidationCount");
        Assert.Single(count);
        var expected = cache ? "1" : "2";
        Assert.Equal(expected, count.First().Value);

        clock.Add(TimeSpan.FromMinutes(31));

        // Third request should always trigger validation even if caching
        response = await server.CreateClient().GetAsync("https://example.com/");
        responseAsXml = null;
        if (response.Content != null &&
            response.Content.Headers.ContentType != null &&
            response.Content.Headers.ContentType.MediaType == "text/xml")
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            responseAsXml = XElement.Parse(responseContent);
        }

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        name = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.Name);
        Assert.Single(name);
        Assert.Equal(Expected, name.First().Value);
        count = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == "ValidationCount");
        Assert.Single(count);

        var laterExpected = cache ? "2" : "3";
        Assert.Equal(laterExpected, count.First().Value);
    }

    private static async Task<IHost> CreateHost(
        CertificateAuthenticationOptions configureOptions,
        X509Certificate2 clientCertificate = null,
        Func<HttpContext, bool> handler = null,
        Uri baseAddress = null,
        bool wireUpHeaderMiddleware = false,
        string headerName = "",
        bool useCache = false,
        ISystemClock clock = null)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(builder =>
                builder.UseTestServer()
                    .Configure(app =>
                    {
                        app.Use((context, next) =>
                        {
                            if (clientCertificate != null)
                            {
                                context.Connection.ClientCertificate = clientCertificate;
                            }
                            return next(context);
                        });


                        if (wireUpHeaderMiddleware)
                        {
                            app.UseCertificateForwarding();
                        }

                        app.UseAuthentication();

                        app.Run(async (context) =>
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
                    AuthenticationBuilder authBuilder;
                    if (configureOptions != null)
                    {
                        authBuilder = services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme).AddCertificate(options =>
                        {
                            options.CustomTrustStore = configureOptions.CustomTrustStore;
                            options.ChainTrustValidationMode = configureOptions.ChainTrustValidationMode;
                            options.AllowedCertificateTypes = configureOptions.AllowedCertificateTypes;
                            options.Events = configureOptions.Events;
                            options.ValidateCertificateUse = configureOptions.ValidateCertificateUse;
                            options.RevocationFlag = configureOptions.RevocationFlag;
                            options.RevocationMode = configureOptions.RevocationMode;
                            options.ValidateValidityPeriod = configureOptions.ValidateValidityPeriod;
                            options.AdditionalChainCertificates = configureOptions.AdditionalChainCertificates;
                        });
                    }
                    else
                    {
                        authBuilder = services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme).AddCertificate();
                    }
                    if (useCache)
                    {
                        if (clock != null)
                        {
                            services.AddSingleton<ICertificateValidationCache>(new CertificateValidationCache(Options.Create(new CertificateValidationCacheOptions()), clock));
                        }
                        else
                        {
                            authBuilder.AddCertificateCache();
                        }
                    }

                    if (wireUpHeaderMiddleware && !string.IsNullOrEmpty(headerName))
                    {
                        services.AddCertificateForwarding(options =>
                        {
                            options.CertificateHeader = headerName;
                        });
                    }

                    if (clock != null)
                    {
                        services.AddSingleton(clock);
                    }

                }))
            .Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        server.BaseAddress = baseAddress;
        return host;
    }

    private readonly CertificateAuthenticationEvents successfulValidationEvents = new CertificateAuthenticationEvents()
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

    private readonly CertificateAuthenticationEvents failedValidationEvents = new CertificateAuthenticationEvents()
    {
        OnCertificateValidated = context =>
        {
            context.Fail("Not validated");
            return Task.CompletedTask;
        }
    };

    private readonly CertificateAuthenticationEvents unprocessedValidationEvents = new CertificateAuthenticationEvents()
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

