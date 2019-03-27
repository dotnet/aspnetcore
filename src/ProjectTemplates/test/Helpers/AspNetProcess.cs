// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Certificates.Generation;
using Microsoft.Extensions.CommandLineUtils;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test.Helpers
{
    public class AspNetProcess : IDisposable
    {
        private const string ListeningMessagePrefix = "Now listening on: ";
        private readonly Uri _listeningUri;
        private readonly HttpClient _httpClient;
        private readonly ITestOutputHelper _output;

        internal ProcessEx Process { get; }

        public AspNetProcess(
            ITestOutputHelper output,
            string workingDirectory,
            string dllPath,
            IDictionary<string, string> environmentVariables)
        {
            _output = output;
            _httpClient = new HttpClient(new HttpClientHandler()
            {
                AllowAutoRedirect = true,
                UseCookies = true,
                CookieContainer = new CookieContainer(),
                ServerCertificateCustomValidationCallback = (m, c, ch, p) => true,
            })
            {
                Timeout = TimeSpan.FromMinutes(2)
            };

            var now = DateTimeOffset.Now;
            new CertificateManager().EnsureAspNetCoreHttpsDevelopmentCertificate(now, now.AddYears(1));


            output.WriteLine("Running ASP.NET application...");

            Process = ProcessEx.Run(output, workingDirectory, DotNetMuxer.MuxerPathOrDefault(), $"exec {dllPath}", envVars: environmentVariables);
            _listeningUri = GetListeningUri(output);
        }


        public void VisitInBrowser(IWebDriver driver)
        {
            _output.WriteLine($"Opening browser at {_listeningUri}...");
            driver.Navigate().GoToUrl(_listeningUri);

            if (driver is EdgeDriver)
            {
                // Workaround for untrusted ASP.NET Core development certificates.
                // The edge driver doesn't supported skipping the SSL warning page.

                if (driver.Title.Contains("Certificate error", StringComparison.OrdinalIgnoreCase))
                {
                    _output.WriteLine("Page contains certificate error. Attempting to get around this...");
                    driver.Click(By.Id("moreInformationDropdownSpan"));
                    var continueLink = driver.FindElement(By.Id("invalidcert_continue"));
                    if (continueLink != null)
                    {
                        _output.WriteLine($"Clicking on link '{continueLink.Text}' to skip invalid certificate error page.");
                        continueLink.Click();
                        driver.Navigate().GoToUrl(_listeningUri);
                    }
                    else
                    {
                        _output.WriteLine("Could not find link to skip certificate error page.");
                    }
                }
            }
        }

        private Uri GetListeningUri(ITestOutputHelper output)
        {
            // Wait until the app is accepting HTTP requests
            output.WriteLine("Waiting until ASP.NET application is accepting connections...");
            var listeningMessage = Process
                .OutputLinesAsEnumerable
                .Where(line => line != null)
                .FirstOrDefault(line => line.Trim().StartsWith(ListeningMessagePrefix, StringComparison.Ordinal));

            if (!string.IsNullOrEmpty(listeningMessage))
            {
                listeningMessage = listeningMessage.Trim();
                // Verify we have a valid URL to make requests to
                var listeningUrlString = listeningMessage.Substring(ListeningMessagePrefix.Length);
                output.WriteLine($"Detected that ASP.NET application is accepting connections on: {listeningUrlString}");
                listeningUrlString = listeningUrlString.Substring(0, listeningUrlString.IndexOf(':')) +
                    "://localhost" +
                    listeningUrlString.Substring(listeningUrlString.LastIndexOf(':'));

                output.WriteLine("Sending requests to " + listeningUrlString);
                return new Uri(listeningUrlString, UriKind.Absolute);
            }
            else
            {
                return null;
            }
        }

        public Task AssertOk(string requestUrl)
            => AssertStatusCode(requestUrl, HttpStatusCode.OK);

        public Task AssertNotFound(string requestUrl)
            => AssertStatusCode(requestUrl, HttpStatusCode.NotFound);

        internal Task<HttpResponseMessage> SendRequest(string path)
        {
            return _httpClient.GetAsync(new Uri(_listeningUri, path));
        }

        public async Task AssertStatusCode(string requestUrl, HttpStatusCode statusCode, string acceptContentType = null)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                new Uri(_listeningUri, requestUrl));

            if (!string.IsNullOrEmpty(acceptContentType))
            {
                request.Headers.Add("Accept", acceptContentType);
            }

            var response = await _httpClient.SendAsync(request);
            Assert.Equal(statusCode, response.StatusCode);
        }

        public void Dispose()
        {
            _httpClient.Dispose();
            Process.Dispose();
        }
    }
}
