// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class CdnScriptTagTests
    {
        private readonly ITestOutputHelper _output;

        public CdnScriptTagTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task CheckSubresourceIntegrity()
        {
            var dir = GetSolutionDir();
            var artifactsDir = Path.Combine(dir, "artifacts", "build");
            var packages = Directory.GetFiles(artifactsDir, "*.nupkg");

            var scriptTags = new List<ScriptTag>();
            foreach (var packagePath in packages)
            {
                scriptTags.AddRange(GetScriptTags(packagePath));
            }

            Assert.NotEmpty(scriptTags);
            var shasum = new Dictionary<string, string>();

            var client = new HttpClient();
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

            Assert.All(scriptTags, t =>
            {
                Assert.True(shasum[t.Src] == t.Integrity, userMessage: $"Expected integrity on script tag to be {shasum[t.Src]} but it was {t.Integrity}. {t.FileName}:{t.Entry}");
            });
        }

        private struct ScriptTag
        {
            public string Src;
            public string Integrity;
            public string FileName;
            public string Entry;
        }

        private static readonly Regex _scriptRegex = new Regex(@"<script[^>]*src=""(?'src'http[^""]+)""[^>]*integrity=""(?'integrity'[^""]+)""([^>]*)>", RegexOptions.Multiline);

        private IEnumerable<ScriptTag> GetScriptTags(string zipFile)
        {
            using (var zip = new ZipArchive(File.OpenRead(zipFile), ZipArchiveMode.Read, leaveOpen: false))
            {
                foreach (var entry in zip.Entries)
                {
                    if (string.Equals(".cshtml", Path.GetExtension(entry.Name), StringComparison.OrdinalIgnoreCase))
                    {
                        string contents;
                        using (var reader = new StreamReader(entry.Open()))
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
                                FileName = Path.GetFileName(zipFile),
                                Entry = entry.FullName,
                            };
                            yield return tag;
                            _output.WriteLine($"Found script tag in {tag.FileName}:{tag.Entry}, src='{tag.Src}' integrity='{tag.Integrity}'");
                            match = match.NextMatch();
                        }
                    }
                }
            }
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
    }
}
