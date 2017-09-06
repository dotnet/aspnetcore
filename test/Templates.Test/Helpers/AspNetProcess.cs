using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using Xunit;

namespace Templates.Test.Helpers
{
    public class AspNetProcess : IDisposable
    {
        private const string DefaultFramework = "netcoreapp2.0";
        private const string ListeningMessagePrefix = "Now listening on: ";

        private readonly ProcessEx _process;
        private readonly Uri _listeningUri;
        private readonly HttpClient _httpClient;

        public AspNetProcess(string workingDirectory, string projectName, string targetFrameworkOverride)
        {
            _httpClient = new HttpClient();

            var buildProcess = ProcessEx.Run(workingDirectory, "dotnet", "build --no-restore -c Debug");
            buildProcess.WaitForExit(assertSuccess: true);

            var envVars = new Dictionary<string, string>
            {
                { "ASPNETCORE_URLS", "http://127.0.0.1:0" }
            };

            var framework = string.IsNullOrEmpty(targetFrameworkOverride) ? DefaultFramework : targetFrameworkOverride;
            if (framework.StartsWith("netcore"))
            {
                _process = ProcessEx.Run(workingDirectory, "dotnet", $"exec bin/Debug/{framework}/{projectName}.dll", envVars: envVars);
            }
            else
            {
                var exeFullPath = Path.Combine(workingDirectory, "bin", "Debug", framework, $"{projectName}.exe");
                _process = ProcessEx.Run(workingDirectory, exeFullPath, envVars: envVars);
            }
            
            // Wait until the app is accepting HTTP requests
            var listeningMessage = _process
                .OutputLinesAsEnumerable
                .Where(line => line != null)
                .FirstOrDefault(line => line.StartsWith(ListeningMessagePrefix, StringComparison.Ordinal));
            Assert.True(!string.IsNullOrEmpty(listeningMessage), $"ASP.NET process exited without listening for requests.\nOutput: { _process.Output }\nError: { _process.Error }");

            // Verify we have a valid URL to make requests to            
            var listeningUrlString = listeningMessage.Substring(ListeningMessagePrefix.Length);
            _listeningUri = new Uri(listeningUrlString, UriKind.Absolute);
        }

        public void AssertOk(string requestUrl)
            => AssertStatusCode(requestUrl, HttpStatusCode.OK);

        public void AssertNotFound(string requestUrl)
            => AssertStatusCode(requestUrl, HttpStatusCode.NotFound);

        public void AssertStatusCode(string requestUrl, HttpStatusCode statusCode)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                new Uri(_listeningUri, requestUrl));

            var response = _httpClient.SendAsync(request).Result;
            Assert.Equal(statusCode, response.StatusCode);
        }

        public IWebDriver VisitInBrowser()
        {
            var driver = WebDriverFactory.CreateWebDriver();
            driver.Navigate().GoToUrl(_listeningUri);
            return driver;
        }

        public void Dispose()
        {
            _httpClient.Dispose();
            _process.Dispose();
        }
    }
}
