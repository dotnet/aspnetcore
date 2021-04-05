// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class CdnScriptTagTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly HttpClient _httpClient;
        private static readonly string _solutionDir;
        private static readonly string _artifactsDir;
        private static List<ScriptTag> _scriptTags;

        static CdnScriptTagTests()
        {
            _solutionDir = GetSolutionDir();
            _artifactsDir = Path.Combine(_solutionDir, "artifacts", "build");
            var packages = Directory.GetFiles(_artifactsDir, "*.nupkg");

            _scriptTags = new List<ScriptTag>();
            foreach (var packagePath in packages)
            {
                _scriptTags.AddRange(GetScriptTags(packagePath));
            }
        }

        public CdnScriptTagTests(ITestOutputHelper output)
        {
            _output = output;
            _httpClient = new HttpClient(new RetryHandler(new HttpClientHandler()));
        }

        public static IEnumerable<object[]> SubresourceIntegrityCheckData
        {
            get
            {
                var scriptTags = _scriptTags
                    .Where(st => st.Integrity != null)
                    .Select(st => new object[] { st });
                Assert.NotEmpty(scriptTags);
                return scriptTags;
            }
        }

        [Theory]
        [MemberData(nameof(SubresourceIntegrityCheckData))]
        public async Task CheckSubresourceIntegrity(ScriptTag scriptTag)
        {
            string expectedIntegrity;
            using (var responseStream = await _httpClient.GetStreamAsync(scriptTag.Src))
            using (var alg = SHA384.Create())
            {
                var hash = alg.ComputeHash(responseStream);
                expectedIntegrity = "sha384-" + Convert.ToBase64String(hash);
            }

            Assert.Equal(expectedIntegrity, scriptTag.Integrity);
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

            var cdnContent = await _httpClient.GetStringAsync(scriptTag.Src);
            var fallbackSrcContent = GetFileContentFromArchive(scriptTag, fallbackSrc);

            Assert.Equal(RemoveLineEndings(cdnContent), RemoveLineEndings(fallbackSrcContent));
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

        private static string GetFileContentFromArchive(ScriptTag scriptTag, string relativeFilePath)
        {
            var file = Path.Combine(_artifactsDir, scriptTag.FileName);
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

        private static List<ScriptTag> GetScriptTags(string zipFile)
        {
            var scriptTags = new List<ScriptTag>();
            using (var zip = new ZipArchive(File.OpenRead(zipFile), ZipArchiveMode.Read, leaveOpen: false))
            {
                foreach (var entry in zip.Entries)
                {
                    if (!string.Equals(".cshtml", Path.GetExtension(entry.Name), StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    IHtmlDocument htmlDocument;
                    var htmlParser = new HtmlParser();
                    using (var reader = new StreamReader(entry.Open()))
                    {
                        htmlDocument = htmlParser.Parse(entry.Open());
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
            return scriptTags;
        }

        private static string GetSolutionDir()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "Templating.sln")))
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
    }
}
