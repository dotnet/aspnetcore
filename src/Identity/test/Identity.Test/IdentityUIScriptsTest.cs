// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Identity.Test
{
    public class IdentityUIScriptsTest : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly HttpClient _httpClient;

        public IdentityUIScriptsTest(ITestOutputHelper output)
        {
            _output = output;
            _httpClient = new HttpClient(new RetryHandler(new HttpClientHandler() { }));
        }

        public static IEnumerable<object[]> ScriptWithIntegrityData
        {
            get
            {
                return GetScriptTags()
                    .Where(st => st.Integrity != null)
                    .Select(st => new object[] { st });
            }
        }

        [Theory]
        [MemberData(nameof(ScriptWithIntegrityData))]
        public async Task IdentityUI_ScriptTags_SubresourceIntegrityCheck(ScriptTag scriptTag)
        {
            var sha256Integrity = await GetShaIntegrity(scriptTag, SHA256.Create(), "sha256");
            Assert.Equal(scriptTag.Integrity, sha256Integrity);
        }

        private async Task<string> GetShaIntegrity(ScriptTag scriptTag, HashAlgorithm algorithm, string prefix)
        {
            using (var respStream = await _httpClient.GetStreamAsync(scriptTag.Src))
            using (var alg = SHA256.Create())
            {
                var hash = alg.ComputeHash(respStream);
                return $"{prefix}-" + Convert.ToBase64String(hash);
            }
        }

        public static IEnumerable<object[]> ScriptWithFallbackSrcData
        {
            get
            {
                return GetScriptTags()
                    .Where(st => st.FallbackSrc != null)
                    .Select(st => new object[] { st });
            }
        }

        [Theory]
        [MemberData(nameof(ScriptWithFallbackSrcData))]
        public async Task IdentityUI_ScriptTags_FallbackSourceContent_Matches_CDNContent(ScriptTag scriptTag)
        {
            var slnDir = GetSolutionDir();
            var wwwrootDir = Path.Combine(slnDir, "src", "UI", "wwwroot", scriptTag.Version);

            var cdnContent = await _httpClient.GetStringAsync(scriptTag.Src);
            var fallbackSrcContent = File.ReadAllText(
                Path.Combine(wwwrootDir, scriptTag.FallbackSrc.TrimStart('~').TrimStart('/')));

            Assert.Equal(RemoveLineEndings(cdnContent), RemoveLineEndings(fallbackSrcContent));
        }

        public struct ScriptTag
        {
            public string Version;
            public string Src;
            public string Integrity;
            public string FallbackSrc;
            public string File;

            public override string ToString()
            {
                return Src;
            }
        }

        private static List<ScriptTag> GetScriptTags()
        {
            var slnDir = GetSolutionDir();
            var uiDirV3 = Path.Combine(slnDir, "src", "UI", "Areas", "Identity", "Pages", "V3");
            var uiDirV4 = Path.Combine(slnDir, "src", "UI", "Areas", "Identity", "Pages", "V4");
            var cshtmlFiles = GetRazorFiles(uiDirV3).Concat(GetRazorFiles(uiDirV4));

            var scriptTags = new List<ScriptTag>();
            foreach (var cshtmlFile in cshtmlFiles)
            {
                var tags = GetScriptTags(cshtmlFile);
                scriptTags.AddRange(tags);
            }

            Assert.NotEmpty(scriptTags);

            return scriptTags;

            IEnumerable<string> GetRazorFiles(string dir) => Directory.GetFiles(dir, "*.cshtml", SearchOption.AllDirectories);
        }

        private static List<ScriptTag> GetScriptTags(string cshtmlFile)
        {
            IHtmlDocument htmlDocument;
            var htmlParser = new HtmlParser();
            using (var stream = File.OpenRead(cshtmlFile))
            {
                htmlDocument = htmlParser.Parse(stream);
            }

            var scriptTags = new List<ScriptTag>();
            foreach (var scriptElement in htmlDocument.Scripts)
            {
                var fallbackSrcAttribute = scriptElement.Attributes
                    .FirstOrDefault(attr => string.Equals("asp-fallback-src", attr.Name, StringComparison.OrdinalIgnoreCase));

                scriptTags.Add(new ScriptTag
                {
                    Version = cshtmlFile.Contains("V3") ? "V3" : "V4",
                    Src = scriptElement.Source,
                    Integrity = scriptElement.Integrity,
                    FallbackSrc = fallbackSrcAttribute?.Value,
                    File = cshtmlFile
                });
            }
            return scriptTags;
        }

        private static string GetSolutionDir()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "Identity.sln")))
                {
                    break;
                }
                dir = dir.Parent;
            }
            return dir.FullName;
        }

        private static string RemoveLineEndings(string originalString)
        {
            return originalString.Replace("\r\n", "").Replace("\n", "");
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        class RetryHandler : DelegatingHandler
        {
            public RetryHandler(HttpMessageHandler innerHandler) : base(innerHandler) { }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                HttpResponseMessage result = null;
                for (var i = 0; i < 10; i++)
                {
                    result = await base.SendAsync(request, cancellationToken);
                    if (result.IsSuccessStatusCode)
                    {
                        return result;
                    }
                    await Task.Delay(1000);
                }
                return result;
            }
        }
    }
}
