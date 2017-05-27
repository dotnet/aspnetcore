// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ApplicationInsightsJavaScriptSnippetTest
{
    public class Validator
    {
        private HttpClient _httpClient;

        private HttpClientHandler _httpClientHandler;

        private readonly ILogger _logger;

        private readonly DeploymentResult _deploymentResult;

        private static readonly Assembly _resourcesAssembly = typeof(JavaScriptSnippetTest).GetTypeInfo().Assembly;

        public Validator(
            HttpClient httpClient,
            HttpClientHandler httpClientHandler,
            ILogger logger,
            DeploymentResult deploymentResult)
        {
            _httpClient = httpClient;
            _httpClientHandler = httpClientHandler;
            _logger = logger;
            _deploymentResult = deploymentResult;
        }

        public async Task VerifyLayoutPage(HttpResponseMessage response)
        {
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogInformation("Layout page : {0}", responseContent);
            }

            await ValidateLayoutPage(responseContent);
        }

        public async Task VerifyLayoutPageBeforeScript(HttpResponseMessage response)
        {
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogInformation("Before app insights script : {0}", responseContent);
            }

            await ValidateLayoutPageBeforeScript(responseContent);
        }

        public async Task VerifyLayoutPageAfterScript(HttpResponseMessage response)
        {
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogInformation("After app insights script : {0}", responseContent);
            }

            await ValidateLayoutPageAfterScript(responseContent);
        }

        // Does not check the contents of the JavaScriptSnippet as it might change. Only checks the instrumentation key.
        private async Task ValidateLayoutPage(string responseContent)
        {
            var outputFile = "Rendered.html";
            var expectedContent = await ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);
            foreach (var substring in expectedContent)
            {
#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
                Assert.Contains(substring, responseContent);
#endif
            }
        }

        private async Task ValidateLayoutPageBeforeScript(string responseContent)
        {
            var outputFile = "BeforeScript.html";
            var expectedContent = await ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

            foreach (var substring in expectedContent)
            {
#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
                Assert.Contains(substring, responseContent);
#endif
            }
        }

        private async Task ValidateLayoutPageAfterScript(string responseContent)
        {
            var outputFile = "AfterScript.html";
            var expectedContent = await ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

            foreach (var substring in expectedContent)
            {
#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
                Assert.Contains(substring, responseContent);
#endif
            }
        }

        private static async Task<string> ReadResourceAsync(Assembly assembly, string resourceName, bool sourceFile)
        {
            using (var stream = GetResourceStream(assembly, resourceName, sourceFile))
            {
                if (stream == null)
                {
                    return null;
                }

                using (var streamReader = new StreamReader(stream))
                {
                    return await streamReader.ReadToEndAsync();
                }
            }
        }

        private static Stream GetResourceStream(Assembly assembly, string resourceName, bool sourceFile)
        {
            var fullName = $"{ assembly.GetName().Name }.{ resourceName.Replace('/', '.') }";
            if (!Exists(assembly, fullName))
            {
#if GENERATE_BASELINES
                if (sourceFile)
                {
                    // Even when generating baselines, a missing source file is a serious problem.
                    Assert.True(false, $"Manifest resource: { fullName } not found.");
                }
#else
                // When not generating baselines, a missing source or output file is always an error.
                Assert.True(false, $"Manifest resource '{ fullName }' not found.");
#endif

                return null;
            }

            var stream = assembly.GetManifestResourceStream(fullName);
            if (sourceFile)
            {
                // Normalize line endings to '\r\n' (CRLF). This removes core.autocrlf, core.eol, core.safecrlf, and
                // .gitattributes from the equation and treats "\r\n" and "\n" as equivalent. Does not handle
                // some line endings like "\r" but otherwise ensures checksums and line mappings are consistent.
                string text;
                using (var streamReader = new StreamReader(stream))
                {
                    text = streamReader.ReadToEnd().Replace("\r", "").Replace("\n", "\r\n");
                }

                var bytes = Encoding.UTF8.GetBytes(text);
                stream = new MemoryStream(bytes);
            }

            return stream;
        }

        private static bool Exists(Assembly assembly, string fullName)
        {
            var resourceNames = assembly.GetManifestResourceNames();
            foreach (var resourceName in resourceNames)
            {
                // Resource names are case-sensitive.
                if (string.Equals(fullName, resourceName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
