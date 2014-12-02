using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using Xunit;

namespace E2ETests
{
    /// <summary>
    /// Summary description for PublishAndRunTests
    /// </summary>
    public partial class SmokeTests
    {
        [Theory]
        [InlineData(ServerType.Helios, KreFlavor.DesktopClr, KreArchitecture.x86, "http://localhost:5001/", false)]
        [InlineData(ServerType.WebListener, KreFlavor.DesktopClr, KreArchitecture.amd64, "http://localhost:5002/", false)]
        //https://github.com/aspnet/KRuntime/issues/642
        //[InlineData(ServerType.Helios, KreFlavor.CoreClr, KreArchitecture.amd64, "http://localhost:5001/", false)]
        [InlineData(ServerType.Kestrel, KreFlavor.Mono, KreArchitecture.x86, "http://localhost:5004/", true)]
        public void PublishAndRunTests(ServerType serverType, KreFlavor kreFlavor, KreArchitecture architecture, string applicationBaseUrl, bool RunTestOnMono = false)
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
                PackApplicationBeforeStart = true
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

                HttpResponseMessage response = null;
                string responseContent = null;
                var initializationCompleteTime = DateTime.MinValue;

                //Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                //Add retry logic since tests are flaky on mono due to connection issues
                for (int retryCount = 0; retryCount < 3; retryCount++)
                {
                    try
                    {
                        response = httpClient.GetAsync(string.Empty).Result;
                        responseContent = response.Content.ReadAsStringAsync().Result;
                        initializationCompleteTime = DateTime.Now;
                        Console.WriteLine("[Time]: Approximate time taken for application initialization : '{0}' seconds", (initializationCompleteTime - testStartTime).TotalSeconds);
                        break; //Went through successfully
                    }
                    catch (AggregateException exception)
                    {
                        // Both type exceptions thrown by Mono which are resolved by retry logic
                        if (exception.InnerException is HttpRequestException || exception.InnerException is WebException)
                        {
                            Console.WriteLine("Failed to complete the request with error: {0}", exception.ToString());
                            Console.WriteLine("Retrying request..");
                            Thread.Sleep(1 * 1000); //Wait for a second before retry
                        }
                    }
                }

                VerifyHomePage(response, responseContent, true);

                //Static files are served?
                VerifyStaticContentServed();

                if (serverType != ServerType.Helios)
                {
                    if (Directory.GetFiles(startParameters.ApplicationPath, "*.cmd", SearchOption.TopDirectoryOnly).Length > 0)
                    {
                        throw new Exception("packExclude parameter values are not honored");
                    }
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