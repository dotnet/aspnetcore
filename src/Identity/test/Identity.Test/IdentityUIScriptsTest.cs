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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Xunit;
using Xunit.Abstractions;
using System.Reflection;

namespace Microsoft.AspNetCore.Identity.Test
{
    public class IdentityUIScriptsTest : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly HttpClient _httpClient;

        public IdentityUIScriptsTest(ITestOutputHelper output)
        {
            _output = output;
            _httpClient = new HttpClient(new RetryHandler(new HttpClientHandler() { }, output, TimeSpan.FromSeconds(1), 5));
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
            var integrity = await GetShaIntegrity(scriptTag);
            Assert.Equal(scriptTag.Integrity, integrity);
        }

        private async Task<string> GetShaIntegrity(ScriptTag scriptTag)
        {
            var isSha256 = scriptTag.Integrity.StartsWith("sha256");
            var prefix = isSha256 ? "sha256" : "sha384";
            using (var respStream = await _httpClient.GetStreamAsync(scriptTag.Src))
            using (var alg256 = SHA256.Create())
            using (var alg384 = SHA384.Create())
            {
                byte[] hash;
                if (isSha256)
                {
                    hash = alg256.ComputeHash(respStream);
                }
                else
                {
                    hash = alg384.ComputeHash(respStream);
                }
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
        [QuarantinedTest("https://github.com/dotnet/aspnetcore-internal/issues/2267")]
        public async Task IdentityUI_ScriptTags_FallbackSourceContent_Matches_CDNContent(ScriptTag scriptTag)
        {
            var wwwrootDir = Path.Combine(GetProjectBasePath(), "wwwroot", scriptTag.Version);

            var cdnContent = await _httpClient.GetStringAsync(scriptTag.Src);
            var fallbackSrcContent = File.ReadAllText(
                Path.Combine(wwwrootDir, scriptTag.FallbackSrc.Replace("Identity", "").TrimStart('~').TrimStart('/')));

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
            var uiDirV3 = Path.Combine(GetProjectBasePath(), "Areas", "Identity", "Pages", "V3");
            var uiDirV4 = Path.Combine(GetProjectBasePath(), "Areas", "Identity", "Pages", "V4");
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

        private static string RemoveLineEndings(string originalString)
        {
            return originalString.Replace("\r\n", "").Replace("\n", "");
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        private static string GetProjectBasePath()
        {
            var projectPath = typeof(IdentityUIScriptsTest).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(a => a.Key == "Microsoft.AspNetCore.Testing.DefaultUIProjectPath").Value;
            return Directory.Exists(projectPath) ? projectPath : Path.Combine(FindHelixSlnFileDirectory(), "UI");
        }

        private static string FindHelixSlnFileDirectory()
        {
            var applicationPath = Path.GetDirectoryName(typeof(IdentityUIScriptsTest).Assembly.Location);
            var directoryInfo = new DirectoryInfo(applicationPath);
            do
            {
                var solutionPath = Directory.EnumerateFiles(directoryInfo.FullName, "*.sln").FirstOrDefault();
                if (solutionPath != null)
                {
                    return directoryInfo.FullName;
                }

                directoryInfo = directoryInfo.Parent;
            }
            while (directoryInfo.Parent != null);

            throw new InvalidOperationException($"Solution root could not be located using application root {applicationPath}.");
        }
    }
}
