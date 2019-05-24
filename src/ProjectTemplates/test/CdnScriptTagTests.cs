// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class CdnScriptTagTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly HttpClient _httpClient;
        private static List<ScriptTag> _scriptTags;
        private static List<LinkTag> _linkTags;
        private static readonly string[] _packages;

        static CdnScriptTagTests()
        {
            var searchPattern = "*.nupkg";
            _packages = Directory.EnumerateFiles(
                    ResolveFolder("ArtifactsShippingPackagesDir"),
                    searchPattern)
                .Concat(Directory.EnumerateFiles(
                    ResolveFolder("ArtifactsNonShippingPackagesDir"),
                    searchPattern))
                .ToArray();

            _scriptTags = new List<ScriptTag>();
            _linkTags = new List<LinkTag>();
            foreach (var packagePath in _packages)
            {
                var tags = GetTags(packagePath);
                _scriptTags.AddRange(tags.scripts);
                _linkTags.AddRange(tags.links);
            }
        }

        private static string ResolveFolder(string folder) =>
            typeof(CdnScriptTagTests).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(a => a.Key == folder).Value;

        public CdnScriptTagTests(ITestOutputHelper output)
        {
            _output = output;
            _httpClient = new HttpClient();
        }

        public static IEnumerable<object[]> SubresourceIntegrityCheckScriptData
        {
            get
            {
                var scriptTags = _scriptTags
                    .Where(st => st.FallbackSrc != null)
                    .Select(st => new object[] { st });
                Assert.NotEmpty(scriptTags);
                return scriptTags;
            }
        }

        public static IEnumerable<object[]> SubresourceIntegrityCheckLinkData
        {
            get
            {
                var linkTags = _linkTags
                    .Where(st => st.FallbackHRef != null)
                    .Select(st => new object[] { st });
                Assert.NotEmpty(linkTags);
                return linkTags;
            }
        }

        [Theory]
        [MemberData(nameof(SubresourceIntegrityCheckScriptData))]
        public async Task CheckScriptSubresourceIntegrity(ScriptTag scriptTag)
        {
            var expectedIntegrity = await GetShaIntegrity(scriptTag);
            if (!string.Equals(expectedIntegrity, scriptTag.Integrity, StringComparison.OrdinalIgnoreCase))
            {
                Assert.False(true, $"Expected {scriptTag.Src} to have Integrity '{expectedIntegrity}' but it had '{scriptTag.Integrity}'.");
            }
        }

        [Theory]
        [MemberData(nameof(SubresourceIntegrityCheckLinkData))]
        public async Task CheckLinkSubresourceIntegrity(LinkTag linkTag)
        {
            string expectedIntegrity = await GetShaIntegrity(linkTag);
            if (!expectedIntegrity.Equals(linkTag.Integrity, StringComparison.OrdinalIgnoreCase))
            {
                Assert.False(true, $"Expected {linkTag.HRef} to have Integrity '{expectedIntegrity}' but it had '{linkTag.Integrity}'.");
            }
        }

        public static IEnumerable<object[]> FallbackSrcCheckData
        {
            get
            {
                var scriptTags = _scriptTags
                    .Where(st => st.FallbackSrc != null)
                    .Select(st => new object[] { st });
                Assert.NotEmpty(scriptTags);
                return scriptTags;
            }
        }

        [Theory]
        [MemberData(nameof(FallbackSrcCheckData))]
        public async Task FallbackSrcContent_Matches_CDNContent(ScriptTag scriptTag)
        {
            var fallbackSrc = scriptTag.FallbackSrc
                .TrimStart('~')
                .TrimStart('/');

            var cdnContent = await GetStringFromCDN(scriptTag.Src);
            var fallbackSrcContent = GetFileContentFromArchive(scriptTag, fallbackSrc);

            Assert.Equal(RemoveLineEndings(cdnContent), RemoveLineEndings(fallbackSrcContent));
        }

        public struct LinkTag
        {
            public string Rel;
            public string HRef;
            public string FallbackHRef;
            public string Integrity;

            public override string ToString()
            {
                return $"{HRef}, {Integrity}";
            }
        }

        public struct ScriptTag
        {
            public string Src;
            public string Integrity;
            public string FallbackSrc;
            public string FileName;
            public string Entry;

            public override string ToString()
            {
                return $"{Src}, {Entry}";
            }
        }

        private async Task<string> GetStringFromCDN(string src)
        {
            var response = await GetFromCDN(src);
            return await response.Content.ReadAsStringAsync();
        }

        private async Task<byte[]> GetByteArrayFromCDN(string src)
        {
            var response = await GetFromCDN(src);
            return await response.Content.ReadAsByteArrayAsync();
        }

        private async Task<HttpResponseMessage> GetFromCDN(string src)
        {
            var logger = NullLogger.Instance;
            return await RetryHelper.RetryRequest(async () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, new Uri(src));
                return await _httpClient.SendAsync(request);
            }, logger);
        }

        private Task<string> GetShaIntegrity(ScriptTag scriptTag)
        {
            return GetShaIntegrity(scriptTag.Integrity, scriptTag.Src);
        }

        private Task<string> GetShaIntegrity(LinkTag linkTag)
        {
            return GetShaIntegrity(linkTag.Integrity, linkTag.HRef);
        }

        private async Task<string> GetShaIntegrity(string integrity, string src)
        {
            var prefix = integrity.Substring(0, 6);
            var respStream = await GetByteArrayFromCDN(src);
            using (HashAlgorithm alg = string.Equals(prefix, "sha256") ? (HashAlgorithm)SHA256.Create() : (HashAlgorithm)SHA384.Create())
            {
                var hash = alg.ComputeHash(respStream);
                return $"{prefix}-" + Convert.ToBase64String(hash);
            }
        }

        private static string GetFileContentFromArchive(ScriptTag scriptTag, string relativeFilePath)
        {
            var file = _packages.Single(f => f.EndsWith(scriptTag.FileName));
            using (var zip = new ZipArchive(File.OpenRead(file), ZipArchiveMode.Read, leaveOpen: false))
            {
                var entry = zip.Entries
                    .Where(e => e.FullName.EndsWith(relativeFilePath, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();

                if (entry != null)
                {
                    using (var reader = new StreamReader(entry.Open()))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            return null;
        }

        private static (List<ScriptTag> scripts, List<LinkTag> links) GetTags(string zipFile)
        {
            var scriptTags = new List<ScriptTag>();
            var linkTags = new List<LinkTag>();
            using (var zip = new ZipArchive(File.OpenRead(zipFile), ZipArchiveMode.Read, leaveOpen: false))
            {
                foreach (var entry in zip.Entries)
                {
                    if (!string.Equals(".cshtml", Path.GetExtension(entry.Name), StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    IHtmlDocument htmlDocument;
                    var options = new HtmlParserOptions
                    {
                        IsStrictMode = false,
                        IsEmbedded = false,
                    };
                    var config = Configuration.Default;
                    var htmlParser = new HtmlParser(options, config);
                    using (var reader = new StreamReader(entry.Open()))
                    {
                        htmlDocument = htmlParser.Parse(entry.Open());
                    }

                    foreach (IElement link in htmlDocument.Body.GetElementsByTagName("link"))
                    {
                        linkTags.Add(new LinkTag
                        {
                            HRef = link.GetAttribute("href"),
                            Integrity = link.GetAttribute("integrity"),
                            FallbackHRef = link.GetAttribute("asp-fallback-href"),
                        });
                    }

                    foreach (var scriptElement in htmlDocument.Scripts)
                    {
                        var fallbackSrcAttribute = scriptElement.Attributes
                            .FirstOrDefault(attr => string.Equals("asp-fallback-src", attr.Name, StringComparison.OrdinalIgnoreCase));

                        scriptTags.Add(new ScriptTag
                        {
                            Src = scriptElement.Source,
                            Integrity = scriptElement.Integrity,
                            FallbackSrc = fallbackSrcAttribute?.Value,
                            FileName = Path.GetFileName(zipFile),
                            Entry = entry.FullName
                        });
                    }

                }
            }
            return (scriptTags, linkTags);
        }

        private static string RemoveLineEndings(string originalString)
        {
            return originalString.Replace("\r\n", "").Replace("\n", "");
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
