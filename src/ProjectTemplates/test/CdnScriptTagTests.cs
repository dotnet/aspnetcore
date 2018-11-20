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
using System.Linq;
using System.Net.Http;
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

        public static ProjectFactoryFixture ProjectFactory { get; set; }
        public Project Project { get; set; }

        public static readonly IEnumerable<object[]> Templates = new object[][] {
            new object[]{ "blazorserverside", null, 0 },
            new object[]{ "razor", null, 4 },
            new object[] { "mvc", null, 4 },
            new object[] { "mvc", "F#", 4 } };

        public CdnScriptTagTests(ITestOutputHelper output, ProjectFactoryFixture projectFactory)
        {
            _output = output;
            _httpClient = new HttpClient();
            ProjectFactory = projectFactory;
        }

        [Theory]
        [MemberData(nameof(Templates))]
        public async Task CheckScriptSubresourceIntegrity(string templateName, string language, int expectedTags)
        {
            (IEnumerable<ScriptTag> scriptTags, List<LinkTag> _) = await GetTagsAsync(templateName, language, _output);
            scriptTags = scriptTags.Where(s => s.FallbackSrc != null);

            Assert.Equal(expectedTags, scriptTags.Count());
            foreach (var scriptTag in scriptTags)
            {
                var expectedIntegrity = await GetShaIntegrity(scriptTag);
                if (!string.Equals(expectedIntegrity, scriptTag.Integrity, StringComparison.OrdinalIgnoreCase))
                {
                    Assert.False(true, $"Expected {scriptTag.Src} to have Integrity '{expectedIntegrity}' but it had '{scriptTag.Integrity}'.");
                }
            }
        }

        [Theory]
        [MemberData(nameof(Templates))]
        public async Task CheckLinkSubresourceIntegrity(string templateName, string language, int expectedTags)
        {
            (IEnumerable<ScriptTag>_, IEnumerable<LinkTag> linkTags) = await GetTagsAsync(templateName, language, _output);
            linkTags = linkTags.Where(s => s.Integrity != null);

            Assert.Equal(expectedTags, linkTags.Count());
            foreach (var linkTag in linkTags)
            {
                string expectedIntegrity = await GetShaIntegrity(linkTag);
                if (!expectedIntegrity.Equals(linkTag.Integrity, StringComparison.OrdinalIgnoreCase))
                {
                    Assert.False(true, $"Expected {linkTag.HRef} to have Integrity '{expectedIntegrity}' but it had '{linkTag.Integrity}'.");
                }
            }
        }

        [Theory]
        [MemberData(nameof(Templates))]
        public async Task FallbackSrcContent_Matches_CDNContent(string templateName, string language, int expectedTags)
        {
            (IEnumerable<ScriptTag> scriptTags, List<LinkTag> _) = await GetTagsAsync(templateName, language, _output);
            scriptTags = scriptTags.Where(s => s.FallbackSrc != null);

            Assert.Equal(expectedTags, scriptTags.Count());
            foreach(var scriptTag in scriptTags)
            {
                var fallbackSrc = scriptTag.FallbackSrc
                    .TrimStart('~')
                    .TrimStart('/');

                var cdnContent = await GetStringFromCDN(scriptTag.Src);

                var fallbackSrcContent = await GetFileContentFromTemplateAsync(scriptTag, fallbackSrc);

                Assert.Equal(RemoveLineEndings(cdnContent), RemoveLineEndings(fallbackSrcContent));
            }
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
            public string Project;

            public override string ToString()
            {
                return $"{Src}, {FileName}";
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

        private async Task<string> GetFileContentFromTemplateAsync(ScriptTag scriptTag, string relativeFilePath)
        {
            var newResult = await Project.RunDotNetNewAsync(scriptTag.Project);
            Assert.Equal(0, newResult.ExitCode);

            var files = Directory.GetFiles(Project.TemplateOutputDir, relativeFilePath);
            var fileStr = Assert.Single(files);

            using (var reader = new StreamReader(File.Open(fileStr, FileMode.Open)))
            {
                return reader.ReadToEnd();
            }
        }

        private async Task<(List<ScriptTag> scripts, List<LinkTag> links)> GetTagsAsync(string templateName, string language, ITestOutputHelper output)
        {
            var scriptTags = new List<ScriptTag>();
            var linkTags = new List<LinkTag>();
            Project = await ProjectFactory.GetOrCreateProject($"{templateName}{language}.CdnCheck", output);
            var createResult = await Project.RunDotNetNewAsync(templateName, language: language);
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", Project, createResult));

            var buildResult = await Project.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", Project, buildResult));

            foreach(var file in Project.GetFiles())
            {
                if (!string.Equals(".cshtml", Path.GetExtension(file), StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(".razor", Path.GetExtension(file), StringComparison.OrdinalIgnoreCase))
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
                using (var stream = File.Open(file, FileMode.Open))
                {
                    htmlDocument = htmlParser.Parse(stream);
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
                        FileName = file,
                        Project = templateName
                    });
                }
            }

            return (scriptTags, linkTags);
        }

        private static string EntryToProjectName(ScriptTag scriptTag)
        {
            switch(scriptTag.Project)
            {
                case "RazorPagesWeb-CSharp":
                    return "razor";
                case "StarterWeb-CSharp":
                    return "mvc";
                default:
                    throw new NotSupportedException($"{scriptTag.Project} isn't supported yet.");

            }
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
