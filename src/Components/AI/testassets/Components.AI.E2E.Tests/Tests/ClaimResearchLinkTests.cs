// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using ComponentsAIClaimApp.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DojoClient.E2E.Tests.Tests;

[TestClass]
public class ClaimResearchLinkTests
{
    [TestMethod]
    public void ResolveClaimAgentBaseAddress_PreservesPathPrefixAndNormalizesSlash()
    {
        var result = ClaimAgentAddress.Resolve(
            "https://internal.example/proxy-prefix",
            "https://browser.example/");

        Assert.AreEqual(
            "https://internal.example/proxy-prefix/",
            result.AbsoluteUri);
    }

    [TestMethod]
    public void ResolveClaimAgentBaseAddress_UsesNavigationFallback()
    {
        var result = ClaimAgentAddress.Resolve(
            configuredBaseAddress: null,
            "https://browser.example/app/");

        Assert.AreEqual("https://browser.example/app/", result.AbsoluteUri);
    }

    [TestMethod]
    [DataRow("claim-agent")]
    [DataRow("javascript:alert(1)")]
    [DataRow("file:///claim-agent")]
    [DataRow("https://example.com/?route=claim-agent")]
    public void ResolveClaimAgentBaseAddress_RejectsInvalidValues(string value)
    {
        Assert.ThrowsExactly<InvalidOperationException>(
            () => ClaimAgentAddress.Resolve(value, "https://browser.example/"));
    }

    [TestMethod]
    [DataRow("https://example.com/part", "https://example.com/part")]
    [DataRow("http://localhost:5000/catalog", "http://localhost:5000/catalog")]
    public void Normalize_AcceptsAbsoluteHttpLinks(string value, string expected)
    {
        Assert.AreEqual(expected, ClaimResearchLink.Normalize(value));
    }

    [TestMethod]
    [DataRow("javascript:alert(1)")]
    [DataRow("data:text/html,<script>alert(1)</script>")]
    [DataRow("file:///etc/passwd")]
    [DataRow("//example.com/part")]
    [DataRow("http:part")]
    [DataRow("not a uri")]
    public void Normalize_RejectsUnsafeOrNonAbsoluteLinks(string value)
    {
        Assert.IsNull(ClaimResearchLink.Normalize(value));
    }

    [TestMethod]
    public void Sanitize_AfterDeserializationPreservesPartDataAndRemovesUnsafeLinks()
    {
        var analysis = JsonSerializer.Deserialize<ClaimDamageAnalysis>(
            """
            {
              "replacementParts": [
                {
                  "name": "Front bumper cover",
                  "fitment": "Verify by VIN",
                  "sourceTitle": "Unsafe source",
                  "sourceUrl": "javascript:alert(1)"
                }
              ],
              "researchSources": [
                {
                  "title": "Unsafe citation",
                  "url": "data:text/html,unsafe"
                },
                {
                  "title": "Safe citation",
                  "url": "https://example.com/catalog"
                }
              ]
            }
            """,
            ClaimStateJson.Options)!;

        ClaimResearchLink.Sanitize(analysis);

        Assert.HasCount(1, analysis.ReplacementParts);
        var part = analysis.ReplacementParts[0];
        Assert.AreEqual("Front bumper cover", part.Name);
        Assert.AreEqual("Verify by VIN", part.Fitment);
        Assert.AreEqual(string.Empty, part.SourceUrl);
        Assert.HasCount(1, analysis.ResearchSources);
        var source = analysis.ResearchSources[0];
        Assert.AreEqual("Safe citation", source.Title);
        Assert.AreEqual("https://example.com/catalog", source.Url);
    }
}
