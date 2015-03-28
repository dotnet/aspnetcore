using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Framework.Logging;
using Xunit;

namespace E2ETests
{
    public partial class SmokeTests
    {
        [ConditionalTheory]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.DesktopClr, RuntimeArchitecture.x86, "http://localhost:5001/", false)]
        // [InlineData(ServerType.IISExpress, RuntimeFlavor.DesktopClr, RuntimeArchitecture.x86, "http://localhost:5001/", true)]
        public void Publish_And_Run_Tests_On_X86(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, bool noSource)
        {
            Publish_And_Run_Tests(serverType, runtimeFlavor, architecture, applicationBaseUrl, noSource);
        }

        [ConditionalTheory]
        [FrameworkSkipCondition(RuntimeFrameworks.DotNet)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.Mono, RuntimeArchitecture.x86, "http://localhost:5004/", false)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.Mono, RuntimeArchitecture.x86, "http://localhost:5004/", true)]
        public void Publish_And_Run_Tests_On_Mono(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, bool noSource)
        {
            Publish_And_Run_Tests(serverType, runtimeFlavor, architecture, applicationBaseUrl, noSource);
        }

        [ConditionalTheory]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.DesktopClr, RuntimeArchitecture.amd64, "http://localhost:5002/", false)]
        //https://github.com/aspnet/KRuntime/issues/642
        //[InlineData(ServerType.Helios, RuntimeFlavor.CoreClr, RuntimeArchitecture.amd64, "http://localhost:5001/")]
        public void Publish_And_Run_Tests_On_AMD64(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, bool noSource)
        {
            Publish_And_Run_Tests(serverType, runtimeFlavor, architecture, applicationBaseUrl, noSource);
        }

        private void Publish_And_Run_Tests(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, bool noSource)
        {
            using (_logger.BeginScope("Publish_And_Run_Tests"))
            {
                _logger.LogInformation("Variation Details : HostType = {hostType}, RuntimeFlavor = {flavor}, Architecture = {arch}, applicationBaseUrl = {appBase}",
                    serverType, runtimeFlavor, architecture, applicationBaseUrl);

                _startParameters = new StartParameters
                {
                    ServerType = serverType,
                    RuntimeFlavor = runtimeFlavor,
                    RuntimeArchitecture = architecture,
                    PublishApplicationBeforeStart = true,
                    PublishWithNoSource = noSource
                };

                var stopwatch = Stopwatch.StartNew();
                var musicStoreDbName = Guid.NewGuid().ToString().Replace("-", string.Empty);

                _logger.LogInformation("Pointing MusicStore DB to '{connString}'", string.Format(CONNECTION_STRING_FORMAT, musicStoreDbName));

                //Override the connection strings using environment based configuration
                Environment.SetEnvironmentVariable("SQLAZURECONNSTR_DefaultConnection", string.Format(CONNECTION_STRING_FORMAT, musicStoreDbName));

                _applicationBaseUrl = applicationBaseUrl;
                Process hostProcess = null;
                bool testSuccessful = false;

                try
                {
                    hostProcess = DeploymentUtility.StartApplication(_startParameters, _logger);

                    _httpClientHandler = new HttpClientHandler() { UseDefaultCredentials = true };
                    _httpClient = new HttpClient(_httpClientHandler) { BaseAddress = new Uri(applicationBaseUrl) };

                    HttpResponseMessage response = null;
                    string responseContent = null;

                    //Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                    //Add retry logic since tests are flaky on mono due to connection issues
                    Helpers.Retry(() =>
                    {
                        response = _httpClient.GetAsync(string.Empty).Result;
                        responseContent = response.Content.ReadAsStringAsync().Result;
                        _logger.LogInformation("[Time]: Approximate time taken for application initialization : '{t}' seconds", stopwatch.Elapsed.TotalSeconds);
                    }, logger: _logger);

                    VerifyHomePage(response, responseContent, true);

                    //Static files are served?
                    VerifyStaticContentServed();

                    if (serverType != ServerType.IISExpress)
                    {
                        if (Directory.GetFiles(_startParameters.ApplicationPath, "*.cmd", SearchOption.TopDirectoryOnly).Length > 0)
                        {
                            throw new Exception("publishExclude parameter values are not honored.");
                        }
                    }

                    stopwatch.Stop();
                    _logger.LogInformation("[Time]: Total time taken for this test variation '{t}' seconds.", stopwatch.Elapsed.TotalSeconds);
                    testSuccessful = true;
                }
                finally
                {
                    if (!testSuccessful)
                    {
                        _logger.LogError("Some tests failed. Proceeding with cleanup.");
                    }

                    DeploymentUtility.CleanUpApplication(_startParameters, hostProcess, musicStoreDbName, _logger);
                }
            }
        }
    }
}