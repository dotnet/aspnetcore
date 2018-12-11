// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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
        private const string DefaultFramework = "netcoreapp3.0";
        private const string ListeningMessagePrefix = "Now listening on: ";
        private static int Port = 5000 + new Random().Next(3000);

        private readonly ProcessEx _process;
        private readonly Uri _listeningUri;
        private readonly HttpClient _httpClient;
        private readonly ITestOutputHelper _output;

        public AspNetProcess(ITestOutputHelper output, string workingDirectory, string projectName, bool publish)
        {
            _output = output;
            _httpClient = new HttpClient(new HttpClientHandler()
            {
                AllowAutoRedirect = true,
                UseCookies = true,
                CookieContainer = new CookieContainer(),
                ServerCertificateCustomValidationCallback = (m, c, ch, p) => true
            });

            var now = DateTimeOffset.Now;
            new CertificateManager().EnsureAspNetCoreHttpsDevelopmentCertificate(now, now.AddYears(1));

            if (publish)
            {
                output.WriteLine("Publishing ASP.NET application...");

                // Workaround for issue with runtime store not yet being published
                // https://github.com/aspnet/Home/issues/2254#issuecomment-339709628
                var extraArgs = "-p:PublishWithAspNetCoreTargetManifest=false";

                ProcessEx
                    .Run(output, workingDirectory, DotNetMuxer.MuxerPathOrDefault(), $"publish -c Release {extraArgs}")
                    .WaitForExit(assertSuccess: true);
                workingDirectory = Path.Combine(workingDirectory, "bin", "Release", DefaultFramework, "publish");
                if (File.Exists(Path.Combine(workingDirectory, "ClientApp", "package.json")))
                {
                    Npm.RestoreWithRetry(output, Path.Combine(workingDirectory, "ClientApp"));
                }
            }
            else
            {
                output.WriteLine("Building ASP.NET application...");
                ProcessEx
                    .Run(output, workingDirectory, DotNetMuxer.MuxerPathOrDefault(), "build -c Debug")
                    .WaitForExit(assertSuccess: true);
            }

            var envVars = new Dictionary<string, string>
            {
                { "ASPNETCORE_URLS", $"http://127.0.0.1:0;https://127.0.0.1:0" }
            };

            if (!publish)
            {
                envVars["ASPNETCORE_ENVIRONMENT"] = "Development";
            }

            output.WriteLine("Running ASP.NET application...");

            var dllPath = publish ? $"{projectName}.dll" : $"bin/Debug/{DefaultFramework}/{projectName}.dll";
            _process = ProcessEx.Run(output, workingDirectory, DotNetMuxer.MuxerPathOrDefault(), $"exec {dllPath}", envVars: envVars);
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
            var listeningMessage = _process
                .OutputLinesAsEnumerable
                .Where(line => line != null)
                .FirstOrDefault(line => line.StartsWith(ListeningMessagePrefix, StringComparison.Ordinal));
            Assert.True(!string.IsNullOrEmpty(listeningMessage), $"ASP.NET process exited without listening for requests.\nOutput: { _process.Output }\nError: { _process.Error }");

            // Verify we have a valid URL to make requests to
            var listeningUrlString = listeningMessage.Substring(ListeningMessagePrefix.Length);
            output.WriteLine($"Detected that ASP.NET application is accepting connections on: {listeningUrlString}");
            listeningUrlString = listeningUrlString.Substring(0, listeningUrlString.IndexOf(':')) +
                "://localhost" +
                listeningUrlString.Substring(listeningUrlString.LastIndexOf(':'));

            output.WriteLine("Sending requests to " + listeningUrlString);
            return new Uri(listeningUrlString, UriKind.Absolute);
        }

        public void AssertOk(string requestUrl)
            => AssertStatusCode(requestUrl, HttpStatusCode.OK);

        public void AssertNotFound(string requestUrl)
            => AssertStatusCode(requestUrl, HttpStatusCode.NotFound);

        public void AssertStatusCode(string requestUrl, HttpStatusCode statusCode, string acceptContentType = null)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                new Uri(_listeningUri, requestUrl));

            if (!string.IsNullOrEmpty(acceptContentType))
            {
                request.Headers.Add("Accept", acceptContentType);
            }

            var response = _httpClient.SendAsync(request).Result;
            Assert.Equal(statusCode, response.StatusCode);
        }

        public void Dispose()
        {
            _httpClient.Dispose();
            _process.Dispose();
        }
    }
}
