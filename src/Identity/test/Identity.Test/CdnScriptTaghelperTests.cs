// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Identity.Test
{
    public class CdnScriptTagTests
    {
        private readonly ITestOutputHelper _output;

        public CdnScriptTagTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task IdentityUI_ScriptTags_SubresourceIntegrityCheck()
        {
            var slnDir = GetSolutionDir();
            var sourceDir = Path.Combine(slnDir, "UI", "src");
            var cshtmlFiles = Directory.GetFiles(sourceDir, "*.cshtml", SearchOption.AllDirectories);

            var scriptTags = new List<ScriptTag>();
            foreach (var cshtmlFile in cshtmlFiles)
            {
                scriptTags.AddRange(GetScriptTags(cshtmlFile));
            }

            Assert.NotEmpty(scriptTags);

            var shasum = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var client = new HttpClient(new RetryHandler(new HttpClientHandler(), _output)))
            {
                foreach (var script in scriptTags)
                {
                    if (shasum.ContainsKey(script.Src))
                    {
                        continue;
                    }

                    using (var resp = await client.GetStreamAsync(script.Src))
                    using (var alg = SHA384.Create())
                    {
                        var hash = alg.ComputeHash(resp);
                        shasum.Add(script.Src, "sha384-" + Convert.ToBase64String(hash));
                    }
                }
            }

            Assert.All(scriptTags, t =>
            {
                Assert.True(shasum[t.Src] == t.Integrity, userMessage: $"Expected integrity on script tag to be {shasum[t.Src]} but it was {t.Integrity}. {t.FileName}");
            });
        }

        private class RetryHandler : DelegatingHandler
        {
            private readonly ITestOutputHelper _output;

            public RetryHandler(HttpMessageHandler innerHandler, ITestOutputHelper output) : base(innerHandler)
            {
                _output = output;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                HttpResponseMessage result = null;
                var method = request.Method;
                var url = request.RequestUri;
                var waitIntervalBeforeRetry = 1;

                // Try 6 times with 1, 2, 4, 8, 16 seconds between attempts. Last attempt may throw or report
                // error to caller.
                for (var i = 0; i < 5; i++)
                {
                    try
                    {
                        _output.WriteLine($"Sending request '{method} - {url}' {i+1} attempt.");
                        result = await base.SendAsync(request, cancellationToken);
                        if (result.IsSuccessStatusCode)
                        {
                            return result;
                        }
                        else
                        {
                            _output.WriteLine($"Request '{method} - {url}' failed with {result.StatusCode}.");
                        }
                    }
                    catch (Exception e)
                    {
                        _output.WriteLine($"Request '{method} - {url}' failed with {e.ToString()}");
                    }
                    finally
                    {
                        await Task.Delay(TimeSpan.FromSeconds(waitIntervalBeforeRetry), cancellationToken);
                        waitIntervalBeforeRetry = waitIntervalBeforeRetry * 2;
                    }
                }

                // Try one last time to show the actual error.
                return await base.SendAsync(request, cancellationToken);
            }
        }

        private struct ScriptTag
        {
            public string Src;
            public string Integrity;
            public string FileName;
        }

        private static readonly Regex _scriptRegex = new Regex(@"<script[^>]*src=""(?'src'http[^""]+)""[^>]*integrity=""(?'integrity'[^""]+)""([^>]*)>", RegexOptions.Multiline);

        private IEnumerable<ScriptTag> GetScriptTags(string cshtmlFile)
        {
            string contents;
            using (var reader = new StreamReader(File.OpenRead(cshtmlFile)))
            {
                contents = reader.ReadToEnd();
            }

            var match = _scriptRegex.Match(contents);
            while (match != null && match != Match.Empty)
            {
                var tag = new ScriptTag
                {
                    Src = match.Groups["src"].Value,
                    Integrity = match.Groups["integrity"].Value,
                    FileName = Path.GetFileName(cshtmlFile)
                };
                yield return tag;
                _output.WriteLine($"Found script tag in '{tag.FileName}', src='{tag.Src}' integrity='{tag.Integrity}'");
                match = match.NextMatch();
            }
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
    }
}
