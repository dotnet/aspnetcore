using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using Xunit;

namespace E2ETests
{
    public partial class SmokeTests
    {
        [Theory]
        [InlineData(ServerType.Helios, KreFlavor.DesktopClr, KreArchitecture.x86, "http://localhost:5001/", false)]
        [InlineData(ServerType.WebListener, KreFlavor.DesktopClr, KreArchitecture.x86, "http://localhost:5002/", false)]
        [InlineData(ServerType.Helios, KreFlavor.DesktopClr, KreArchitecture.amd64, "http://localhost:5001/", false)]
        //WindowsIdentity not available on CoreCLR
        //[InlineData(ServerType.Helios, KreFlavor.CoreClr, KreArchitecture.x86, "http://localhost:5001/", false)]
        //[InlineData(ServerType.WebListener, KreFlavor.CoreClr, KreArchitecture.x86, "http://localhost:5002/", false)]
        public void NtlmAuthenticationTest(ServerType serverType, KreFlavor kreFlavor, KreArchitecture architecture, string applicationBaseUrl, bool RunTestOnMono = false)
        {
            Console.WriteLine("Variation Details : HostType = {0}, KreFlavor = {1}, Architecture = {2}, applicationBaseUrl = {3}", serverType, kreFlavor, architecture, applicationBaseUrl);

            if (Helpers.SkipTestOnCurrentConfiguration(RunTestOnMono, architecture))
            {
                Assert.True(true);
                return;
            }

            var startParameters = new StartParameters
            {
                ServerType = serverType,
                KreFlavor = kreFlavor,
                KreArchitecture = architecture,
                EnvironmentName = "NtlmAuthentication", //Will pick the Start class named 'StartupNtlmAuthentication'
                ApplicationHostConfigTemplateContent = (serverType == ServerType.Helios) ? File.ReadAllText("NtlmAuthentation.config") : null,
                SiteName = "MusicStoreNtlmAuthentication" //This is configured in the NtlmAuthentication.config
            };

            var testStartTime = DateTime.Now;
            var musicStoreDbName = Guid.NewGuid().ToString().Replace("-", string.Empty);

            Console.WriteLine("Pointing MusicStore DB to '{0}'", string.Format(Connection_string_Format, musicStoreDbName));

            //Override the connection strings using environment based configuration
            Environment.SetEnvironmentVariable("SQLAZURECONNSTR_DefaultConnection", string.Format(Connection_string_Format, musicStoreDbName));

            ApplicationBaseUrl = applicationBaseUrl;
            Process hostProcess = null;
            bool testSuccessful = false;

            try
            {
                hostProcess = DeploymentUtility.StartApplication(startParameters, musicStoreDbName);

                httpClientHandler = new HttpClientHandler() { UseDefaultCredentials = true };
                httpClient = new HttpClient(httpClientHandler) { BaseAddress = new Uri(applicationBaseUrl) };

                //Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                var response = httpClient.GetAsync(string.Empty).Result;
                var responseContent = response.Content.ReadAsStringAsync().Result;
                var initializationCompleteTime = DateTime.Now;
                Console.WriteLine("[Time]: Approximate time taken for application initialization : '{0}' seconds", (initializationCompleteTime - testStartTime).TotalSeconds);
                VerifyHomePage(response, responseContent, true);

                //Check if the user name appears in the page
                Assert.Contains(string.Format("{0}\\{1}", Environment.UserDomainName, Environment.UserName), responseContent, StringComparison.OrdinalIgnoreCase);

                if (serverType != ServerType.Helios)
                {
                    //https://github.com/aspnet/Helios/issues/53
                    //Should be able to access the store as the Startup adds necessary permissions for the current user
                    AccessStoreWithPermissions();
                }

                var testCompletionTime = DateTime.Now;
                Console.WriteLine("[Time]: All tests completed in '{0}' seconds", (testCompletionTime - initializationCompleteTime).TotalSeconds);
                Console.WriteLine("[Time]: Total time taken for this test variation '{0}' seconds", (testCompletionTime - testStartTime).TotalSeconds);
                testSuccessful = true;
            }
            finally
            {
                if (!testSuccessful)
                {
                    Console.WriteLine("Some tests failed. Proceeding with cleanup.");
                }

                DeploymentUtility.CleanUpApplication(startParameters, hostProcess, musicStoreDbName);
            }
        }
    }
}