using System;
using System.Diagnostics;
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
        [InlineData(ServerType.IISExpress, RuntimeFlavor.DesktopClr, RuntimeArchitecture.x86, "http://localhost:5001/")]
        public void OpenIdConnect_OnX86(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            OpenIdConnectTestSuite(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }

        [ConditionalTheory]
        [FrameworkSkipCondition(RuntimeFrameworks.DotNet)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.Mono, RuntimeArchitecture.x86, "http://localhost:5004/")]
        public void OpenIdConnect_OnMono(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            OpenIdConnectTestSuite(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }

        private void OpenIdConnectTestSuite(ServerType serverType, RuntimeFlavor donetFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            using (_logger.BeginScope("OpenIdConnectTestSuite"))
            {
                _logger.LogInformation("Variation Details : HostType = {hostType}, DonetFlavor = {flavor}, Architecture = {arch}, applicationBaseUrl = {appBase}",
                    serverType, donetFlavor, architecture, applicationBaseUrl);

                _startParameters = new StartParameters
                {
                    ServerType = serverType,
                    RuntimeFlavor = donetFlavor,
                    RuntimeArchitecture = architecture,
                    EnvironmentName = "OpenIdConnectTesting"
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
#if DNX451
                    if (serverType == ServerType.IISNativeModule || serverType == ServerType.IIS)
                    {
                        // Accomodate the vdir name.
                        _applicationBaseUrl += _startParameters.IISApplication.VirtualDirectoryName + "/";
                    }
#endif
                    _httpClientHandler = new HttpClientHandler();
                    _httpClient = new HttpClient(_httpClientHandler) { BaseAddress = new Uri(_applicationBaseUrl) };

                    HttpResponseMessage response = null;
                    string responseContent = null;

                    //Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                    Helpers.Retry(() =>
                    {
                        response = _httpClient.GetAsync(string.Empty).Result;
                        responseContent = response.Content.ReadAsStringAsync().Result;
                        _logger.LogInformation("[Time]: Approximate time taken for application initialization : '{t}' seconds", stopwatch.Elapsed.TotalSeconds);
                    }, logger: _logger);

                    VerifyHomePage(response, responseContent);

                    // OpenIdConnect login.
                    LoginWithOpenIdConnect();

                    stopwatch.Stop();
                    _logger.LogInformation("[Time]: Total time taken for this test variation '{t}' seconds", stopwatch.Elapsed.TotalSeconds);
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