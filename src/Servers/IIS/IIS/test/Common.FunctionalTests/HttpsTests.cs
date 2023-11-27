
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.Common;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Xunit;

#if !IIS_FUNCTIONALS
using Microsoft.AspNetCore.Server.IIS.FunctionalTests;

#if IISEXPRESS_FUNCTIONALS
namespace Microsoft.AspNetCore.Server.IIS.IISExpress.FunctionalTests;
#elif NEWHANDLER_FUNCTIONALS
namespace Microsoft.AspNetCore.Server.IIS.NewHandler.FunctionalTests;
#elif NEWSHIM_FUNCTIONALS
namespace Microsoft.AspNetCore.Server.IIS.NewShim.FunctionalTests;
#endif

#else
namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests;
#endif

[Collection(PublishedSitesCollection.Name)]
[SkipIfNotAdmin]
[SkipOnHelix("Unsupported queue", Queues = "Windows.Amd64.VS2022.Pre.Open;")]
public class ClientCertificateTests : IISFunctionalTestBase
{
    private readonly ClientCertificateFixture _certFixture;

    public ClientCertificateTests(PublishedSitesFixture fixture, ClientCertificateFixture certFixture) : base(fixture)
    {
        _certFixture = certFixture;
    }

    public static TestMatrix TestVariants
        => TestMatrix.ForServers(DeployerSelector.ServerType)
            .WithTfms(Tfm.Default)
            .WithApplicationTypes(ApplicationType.Portable)
            .WithAllHostingModels();

    [ConditionalTheory]
    [MemberData(nameof(TestVariants))]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8)]
    public Task HttpsNoClientCert_NoClientCert(TestVariant variant)
    {
        return ClientCertTest(variant, sendClientCert: false);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestVariants))]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8)]
    public Task HttpsClientCert_GetCertInformation(TestVariant variant)
    {
        return ClientCertTest(variant, sendClientCert: true);
    }

    private async Task ClientCertTest(TestVariant variant, bool sendClientCert)
    {
        var port = TestPortHelper.GetNextSSLPort();
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(variant);
        deploymentParameters.ApplicationBaseUriHint = $"https://localhost:{port}/";
        deploymentParameters.AddHttpsWithClientCertToServerConfig();

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (a, b, c, d) => true,
            ClientCertificateOptions = ClientCertificateOption.Manual,
        };

        X509Certificate2 cert = null;
        if (sendClientCert)
        {
            cert = _certFixture.GetOrCreateCertificate();
            handler.ClientCertificates.Add(cert);
        }

        var deploymentResult = await DeployAsync(deploymentParameters);

        var client = deploymentResult.CreateClient(handler);
        var response = await client.GetAsync("GetClientCert");

        var responseText = await response.Content.ReadAsStringAsync();

        try
        {
            if (sendClientCert)
            {
                Assert.Equal($"Enabled;{cert.GetCertHashString()}", responseText);
            }
            else
            {
                Assert.Equal("Disabled", responseText);
            }
        }
        catch (Exception)
        {
            Logger.LogError($"Certificate is invalid. Issuer name: {cert?.Issuer}");
            using (var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine))
            {
                Logger.LogError($"List of current certificates in root store:");
                store.Open(OpenFlags.ReadWrite);
                foreach (var otherCert in store.Certificates)
                {
                    Logger.LogError(otherCert.Issuer);
                }
                store.Close();
            }
            throw;
        }
    }
}
