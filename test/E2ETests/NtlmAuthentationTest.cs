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
        [OSSkipCondition(OperatingSystems.Unix | OperatingSystems.MacOSX)]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, "http://localhost:5001/")]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.DesktopClr, RuntimeArchitecture.amd64, "http://localhost:5001/")]
        [InlineData(ServerType.WebListener, RuntimeFlavor.CoreClr, RuntimeArchitecture.amd64, "http://localhost:5002/")]
        public void NtlmAuthenticationTest(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            using (_logger.BeginScope("NtlmAuthenticationTest"))
            {
                _logger.LogInformation("Variation Details : HostType = {hostType}, RuntimeFlavor = {flavor}, Architecture = {arch}, applicationBaseUrl = {appBase}",
                    serverType, runtimeFlavor, architecture, applicationBaseUrl);

                _startParameters = new StartParameters
                {
                    ServerType = serverType,
                    RuntimeFlavor = runtimeFlavor,
                    RuntimeArchitecture = architecture,
                    EnvironmentName = "NtlmAuthentication", //Will pick the Start class named 'StartupNtlmAuthentication'
                    ApplicationHostConfigTemplateContent = (serverType == ServerType.IISExpress) ? File.ReadAllText("NtlmAuthentation.config") : null,
                    SiteName = "MusicStoreNtlmAuthentication" //This is configured in the NtlmAuthentication.config
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
                    Helpers.Retry(() =>
                    {
                        response = _httpClient.GetAsync(string.Empty).Result;
                        responseContent = response.Content.ReadAsStringAsync().Result;
                        _logger.LogInformation("[Time]: Approximate time taken for application initialization : '{t}' seconds", stopwatch.Elapsed.TotalSeconds);
                    }, logger: _logger);

                    VerifyHomePage(response, responseContent, true);

                    //Check if the user name appears in the page
                    Assert.Contains(
                        string.Format("{0}\\{1}", Environment.GetEnvironmentVariable("USERDOMAIN"), Environment.GetEnvironmentVariable("USERNAME")),
                        responseContent, StringComparison.OrdinalIgnoreCase);

                    //Should be able to access the store as the Startup adds necessary permissions for the current user
                    AccessStoreWithPermissions();

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