using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNetCore.WebSockets.ConformanceTest.Autobahn
{
    public class AutobahnTester : IDisposable
    {
        private int _nextPort;
        private readonly List<IApplicationDeployer> _deployers = new List<IApplicationDeployer>();
        private readonly List<AutobahnExpectations> _expectations = new List<AutobahnExpectations>();
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        public AutobahnSpec Spec { get; }

        public AutobahnTester(ILoggerFactory loggerFactory, AutobahnSpec baseSpec) : this(7000, loggerFactory, baseSpec) { }

        public AutobahnTester(int startPort, ILoggerFactory loggerFactory, AutobahnSpec baseSpec)
        {
            _nextPort = startPort;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger("AutobahnTester");

            Spec = baseSpec;
        }

        public async Task<AutobahnResult> Run(CancellationToken cancellationToken)
        {
            var specFile = Path.GetTempFileName();
            try
            {
                Spec.WriteJson(specFile);

                // Run the test (write something to the console so people know this will take a while...)
                _logger.LogInformation("Now launching Autobahn Test Suite. This will take a while.");
                var exitCode = await Wstest.Default.ExecAsync("-m fuzzingclient -s " + specFile, cancellationToken);
                if (exitCode != 0)
                {
                    throw new Exception("wstest failed");
                }
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
            var outputFile = Path.Combine(Directory.GetCurrentDirectory(), Spec.OutputDirectory, "index.json");
            using (var reader = new StreamReader(File.OpenRead(outputFile)))
            {
                return AutobahnResult.FromReportJson(JObject.Parse(await reader.ReadToEndAsync()));
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
                    failures.AppendLine($"Expected no results for server: {serverResult.Name} but found results!");
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
            var port = Interlocked.Increment(ref _nextPort);
            var baseUrl = ssl ? $"https://localhost:{port}" : $"http://localhost:{port}";
            var sslNamePart = ssl ? "SSL" : "NoSSL";
            var name = $"{server}|{sslNamePart}|{environment}";
            var logger = _loggerFactory.CreateLogger($"AutobahnTestApp:{server}:{sslNamePart}:{environment}");

            var appPath = Helpers.GetApplicationPath("AutobahnTestApp");
            var parameters = new DeploymentParameters(appPath, server, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64)
            {
                ApplicationBaseUriHint = baseUrl,
                ApplicationType = ApplicationType.Portable,
                TargetFramework = "netcoreapp1.1",
                EnvironmentName = environment,
                SiteName = "HttpTestSite", // This is configured in the Http.config
                ServerConfigTemplateContent = (server == ServerType.IISExpress) ? File.ReadAllText("Http.config") : null,
            };

            var deployer = ApplicationDeployerFactory.Create(parameters, logger);
            var result = deployer.Deploy();
            _deployers.Add(deployer);
            cancellationToken.ThrowIfCancellationRequested();

#if NET451
            System.Net.ServicePointManager.ServerCertificateValidationCallback = (_, __, ___, ____) => true;
            var client = new HttpClient();
#else
            var handler = new HttpClientHandler();
            if (ssl)
            {
                // Don't take this out of the "if(ssl)". If we set it on some platforms, it crashes
                // So we avoid running SSL tests on those platforms (for now).
                // See https://github.com/dotnet/corefx/issues/9728
                handler.ServerCertificateCustomValidationCallback = (_, __, ___, ____) => true;
            }
            var client = new HttpClient(handler);
#endif

            // Make sure the server works
            var resp = await RetryHelper.RetryRequest(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return client.GetAsync(result.ApplicationBaseUri);
            }, logger, CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, result.HostShutdownToken).Token, retryCount: 5);
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
}
