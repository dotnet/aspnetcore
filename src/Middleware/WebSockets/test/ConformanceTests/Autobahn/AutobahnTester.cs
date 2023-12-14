// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.WebSockets.ConformanceTest.Autobahn;

public class AutobahnTester : IDisposable
{
    private readonly List<ApplicationDeployer> _deployers = new List<ApplicationDeployer>();
    private readonly List<DeploymentResult> _deployments = new List<DeploymentResult>();
    private readonly List<AutobahnExpectations> _expectations = new List<AutobahnExpectations>();
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;

    public AutobahnSpec Spec { get; }

    public AutobahnTester(ILoggerFactory loggerFactory, AutobahnSpec baseSpec)
    {
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory.CreateLogger("AutobahnTester");

        Spec = baseSpec;
    }

    public async Task<AutobahnResult> Run(CancellationToken cancellationToken)
    {
        var specFile = Path.GetTempFileName();
        try
        {
            // Start pinging the servers to see that they're still running
            var pingCts = new CancellationTokenSource();
            var pinger = new Timer(state => Pinger((CancellationToken)state), pingCts.Token, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));

            Spec.WriteJson(specFile);

            // Run the test (write something to the console so people know this will take a while...)
            _logger.LogInformation("Using 'wstest' from: {WsTestPath}", Wstest.Default.Location);
            _logger.LogInformation("Now launching Autobahn Test Suite. This will take a while.");
            var exitCode = await Wstest.Default.ExecAsync("-m fuzzingclient -s " + specFile, cancellationToken, _loggerFactory.CreateLogger("wstest"));
            if (exitCode != 0)
            {
                throw new Exception("wstest failed");
            }

            pingCts.Cancel();
        }
        finally
        {
            if (File.Exists(specFile))
            {
                File.Delete(specFile);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Parse the output.
        var outputFile = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", Spec.OutputDirectory, "index.json");
        using (var reader = new StreamReader(File.OpenRead(outputFile)))
        {
            return AutobahnResult.FromReportJson(JObject.Parse(await reader.ReadToEndAsync()));
        }
    }

    // Async void! It's OK here because we are running in a timer. We're just using async void to chain continuations.
    // There's nobody to await this, hence async void.
    private async void Pinger(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    foreach (var deployment in _deployments)
                    {
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }

                        var resp = await deployment.HttpClient.GetAsync("/ping", token);
                        if (!resp.IsSuccessStatusCode)
                        {
                            _logger.LogWarning("Non-successful response when pinging {url}: {statusCode} {reasonPhrase}", deployment.ApplicationBaseUri, resp.StatusCode, resp.ReasonPhrase);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // We don't want to throw when the token fires, just stop.
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while pinging servers");
        }
    }

    public void Verify(AutobahnResult result)
    {
        var failures = new StringBuilder();
        foreach (var serverResult in result.Servers)
        {
            var serverExpectation = _expectations.FirstOrDefault(e => e.Server == serverResult.Server && e.Ssl == serverResult.Ssl);
            if (serverExpectation == null)
            {
                failures.AppendLine(FormattableString.Invariant($"Expected no results for server: {serverResult.Name} but found results!"));
            }
            else
            {
                serverExpectation.Verify(serverResult, failures);
            }
        }

        Assert.True(failures.Length == 0, "Autobahn results did not meet expectations:" + Environment.NewLine + failures.ToString());
    }

    public async Task DeployTestAndAddToSpec(ServerType server, bool ssl, string environment, CancellationToken cancellationToken, Action<AutobahnExpectations> expectationConfig = null)
    {
        var sslNamePart = ssl ? "SSL" : "NoSSL";
        var name = $"{server}|{sslNamePart}|{environment}";
        var logger = _loggerFactory.CreateLogger($"AutobahnTestApp:{server}:{sslNamePart}:{environment}");

        var appPath = Helpers.GetApplicationPath("AutobahnTestApp");
        var configPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "Http.config");
        var parameters = new DeploymentParameters(appPath, server, RuntimeFlavor.CoreClr, RuntimeArchitectures.Current)
        {
            Scheme = (ssl ? Uri.UriSchemeHttps : Uri.UriSchemeHttp),
            ApplicationType = ApplicationType.Portable,
            TargetFramework = "Net9.0",
            EnvironmentName = environment,
            SiteName = "HttpTestSite", // This is configured in the Http.config
            ServerConfigTemplateContent = (server == ServerType.IISExpress) ? File.ReadAllText(configPath) : null,
        };

        var deployer = ApplicationDeployerFactory.Create(parameters, _loggerFactory);
        var result = await deployer.DeployAsync();
        _deployers.Add(deployer);
        _deployments.Add(result);
        cancellationToken.ThrowIfCancellationRequested();

        var handler = new HttpClientHandler();
        // Win7 HttpClient on NetCoreApp2.2 defaults to TLS 1.0 and won't connect to Kestrel. https://github.com/dotnet/corefx/issues/28733
        // Mac HttpClient on NetCoreApp2.0 doesn't alow you to set some combinations.
        // https://github.com/dotnet/corefx/blob/586cffcdfdf23ad6c193a4bf37fce88a1bf69508/src/System.Net.Http/src/System/Net/Http/CurlHandler/CurlHandler.SslProvider.OSX.cs#L104-L106
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
        handler.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;
#pragma warning restore SYSLIB0039
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        var client = result.CreateHttpClient(handler);

        // Make sure the server works
        var resp = await RetryHelper.RetryRequest(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return client.GetAsync(result.ApplicationBaseUri);
        }, logger, CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, result.HostShutdownToken).Token);
        resp.EnsureSuccessStatusCode();

        cancellationToken.ThrowIfCancellationRequested();

        // Add to the current spec
        var wsUrl = result.ApplicationBaseUri.Replace("https://", "wss://").Replace("http://", "ws://");
        Spec.WithServer(name, wsUrl);

        var expectations = new AutobahnExpectations(server, ssl, environment);
        expectationConfig?.Invoke(expectations);
        _expectations.Add(expectations);

        cancellationToken.ThrowIfCancellationRequested();
    }

    public void Dispose()
    {
        foreach (var deployer in _deployers)
        {
            deployer.Dispose();
        }
    }
}
