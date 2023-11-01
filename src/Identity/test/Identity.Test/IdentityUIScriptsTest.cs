// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using Microsoft.AspNetCore.InternalTesting;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Identity.Test;

[SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/38542", Queues = "OSX.1015.Amd64.Open;OSX.1015.Amd64")] //slow
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
        var isSha256 = scriptTag.Integrity.StartsWith("sha256", StringComparison.Ordinal);
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
    public async Task IdentityUI_ScriptTags_FallbackSourceContent_Matches_CDNContent(ScriptTag scriptTag)
    {
        var wwwrootDir = Path.Combine(GetProjectBasePath(), "assets", scriptTag.Version);
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
        var scriptTags = new List<ScriptTag>();
        var uiDirV4 = Path.Combine(GetProjectBasePath(), "Areas", "Identity", "Pages", "V4");
        var uiDirV5 = Path.Combine(GetProjectBasePath(), "Areas", "Identity", "Pages", "V5");
        var cshtmlFiles = GetRazorFiles(uiDirV4).Concat(GetRazorFiles(uiDirV5));
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
                Version = cshtmlFile.Contains("V4") ? "V4" : "V5",
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
            .Single(a => a.Key == "Microsoft.AspNetCore.InternalTesting.DefaultUIProjectPath").Value;
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
