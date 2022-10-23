// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Rewrite.IISUrlRewrite;
using Microsoft.AspNetCore.Rewrite.PatternSegments;
using Microsoft.AspNetCore.Rewrite.Tests.IISUrlRewrite;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Rewrite.Tests.UrlRewrite;

public class InputParserTests
{
    [Fact]
    public void InputParser_ParseLiteralString()
    {
        var testString = "hello/hey/what";
        var result = new InputParser().ParseInputString(testString, UriMatchPart.Path);
        Assert.Equal(1, result.PatternSegments.Count);
    }

    [Theory]
    [InlineData("foo/bar/{R:1}/what", 3)]
    [InlineData("foo/{R:1}", 2)]
    [InlineData("foo/{R:1}/{C:2}", 4)]
    [InlineData("foo/{R:1}{C:2}", 3)]
    [InlineData("foo/", 1)]
    public void InputParser_ParseStringWithBackReference(string testString, int expected)
    {
        var result = new InputParser().ParseInputString(testString, UriMatchPart.Path);
        Assert.Equal(expected, result.PatternSegments.Count);
    }

    // Test actual evaluation of the types, verifying the correct string comes from the evalation
    [Theory]
    [InlineData("hey/hello/what", "hey/hello/what")]
    [InlineData("hey/{R:1}/what", "hey/foo/what")]
    [InlineData("hey/{R:2}/what", "hey/bar/what")]
    [InlineData("hey/{R:3}/what", "hey/baz/what")]
    [InlineData("hey/{C:1}/what", "hey/foo/what")]
    [InlineData("hey/{C:2}/what", "hey/bar/what")]
    [InlineData("hey/{C:3}/what", "hey/baz/what")]
    [InlineData("hey/{R:1}/{C:1}", "hey/foo/foo")]
    public void EvaluateBackReferenceRule(string testString, string expected)
    {
        var middle = new InputParser().ParseInputString(testString, UriMatchPart.Path);
        var result = middle.Evaluate(CreateTestRewriteContext(), CreateTestRuleBackReferences(), CreateTestCondBackReferences());
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("hey/{ToLower:HEY}", "hey/hey")]
    [InlineData("hey/{ToLower:{R:1}}", "hey/foo")]
    [InlineData("hey/{ToLower:{C:1}}", "hey/foo")]
    [InlineData("hey/{ToLower:{C:1}/what}", "hey/foo/what")]
    [InlineData("hey/ToLower:/what", "hey/ToLower:/what")]
    public void EvaluatToLowerRule(string testString, string expected)
    {
        var middle = new InputParser().ParseInputString(testString, UriMatchPart.Path);
        var result = middle.Evaluate(CreateTestRewriteContext(), CreateTestRuleBackReferences(), CreateTestCondBackReferences());
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("hey/{UrlEncode:<hey>}", "hey/%3Chey%3E")]
    [InlineData("hey/?returnUrl={UrlEncode:http://domain.com?query=résumé}", "hey/?returnUrl=http%3A%2F%2Fdomain.com%3Fquery%3Dr%C3%A9sum%C3%A9")]
    public void EvaluatUriEncodeRule(string testString, string expected)
    {
        var middle = new InputParser().ParseInputString(testString, UriMatchPart.Path);
        var result = middle.Evaluate(CreateTestRewriteContext(), CreateTestRuleBackReferences(), CreateTestCondBackReferences());
        Assert.Equal(expected, result);
    }
    [Theory]
    [InlineData("hey/{UrlDecode:%3Chey%3E}","hey/<hey>")]
    [InlineData("{UrlDecode:http%3A%2F%2Fdomain.com%3Fquery%3Dr%C3%A9sum%C3%A9}", "http://domain.com?query=résumé")]
    public void EvaluateUriDecodeRule(string testString, string expected)
    {
        var middle = new InputParser().ParseInputString(testString, UriMatchPart.Path);
        var result = middle.Evaluate(CreateTestRewriteContext(), CreateTestRuleBackReferences(), CreateTestCondBackReferences());
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("hey/{HTTP_URL}","hey/TEST_VARIABLE")]
    public void ParseString_WithContextContainingServerVariableString_ShouldReturnResultContainingValueOfVariable(string testString, string expected)
    {
        var variablesDict = new Dictionary<string, string>()
        {
            { "HTTP_URL", "TEST_VARIABLE"}
        };
        var features = new FeatureCollection(1);
        features.Set<IServerVariablesFeature>(new TestServerVariablesFeature(variablesDict));

        var rewriteContext= new RewriteContext { HttpContext = new DefaultHttpContext(features), StaticFileProvider = null, Logger = NullLogger.Instance };

        var middle = new InputParser().ParseInputString(testString, UriMatchPart.Path);
        var result = middle.Evaluate(rewriteContext, CreateTestRuleBackReferences(), CreateTestCondBackReferences());

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("{:}")]
    [InlineData("{R:")]
    [InlineData("{R:1")]
    [InlineData("{R:A}")]
    [InlineData("{R:10}")]
    [InlineData("{R:-1}")]
    [InlineData("{foo:1")]
    [InlineData("{UrlEncode:{R:}}")]
    [InlineData("{UrlEncode:{R:1}")]
    [InlineData("{HTTPS")]
    public void FormatExceptionsOnBadSyntax(string testString)
    {
        Assert.Throws<FormatException>(() => new InputParser().ParseInputString(testString, UriMatchPart.Path));
    }

    [Fact]
    public void Should_throw_FormatException_if_no_rewrite_maps_are_defined()
    {
        Assert.Throws<FormatException>(() => new InputParser(null, false).ParseInputString("{apiMap:{R:1}}", UriMatchPart.Path));
    }

    [Fact]
    public void Should_throw_FormatException_if_rewrite_map_not_found()
    {
        const string definedMapName = "testMap";
        const string undefinedMapName = "apiMap";
        var map = new IISRewriteMap(definedMapName);
        var maps = new IISRewriteMapCollection { map };
        Assert.Throws<FormatException>(() => new InputParser(maps, false).ParseInputString($"{{{undefinedMapName}:{{R:1}}}}", UriMatchPart.Path));
    }

    [Fact]
    public void Should_parse_RewriteMapSegment_and_successfully_evaluate_result()
    {
        const string expectedMapName = "apiMap";
        const string expectedKey = "api.test.com";
        const string expectedValue = "test.com/api";
        var map = new IISRewriteMap(expectedMapName);
        map[expectedKey] = expectedValue;
        var maps = new IISRewriteMapCollection { map };

        var inputString = $"{{{expectedMapName}:{{R:1}}}}";
        var pattern = new InputParser(maps, false).ParseInputString(inputString, UriMatchPart.Path);
        Assert.Equal(1, pattern.PatternSegments.Count);

        var segment = pattern.PatternSegments.Single();
        var rewriteMapSegment = segment as RewriteMapSegment;
        Assert.NotNull(rewriteMapSegment);

        var result = rewriteMapSegment.Evaluate(CreateTestRewriteContext(), CreateRewriteMapRuleMatch(expectedKey).BackReferences, CreateRewriteMapConditionMatch(inputString).BackReferences);
        Assert.Equal(expectedValue, result);
    }

    private static RewriteContext CreateTestRewriteContext()
    {
        var context = new DefaultHttpContext();
        return new RewriteContext { HttpContext = context, StaticFileProvider = null, Logger = NullLogger.Instance };
    }

    private static BackReferenceCollection CreateTestRuleBackReferences()
    {
        var match = Regex.Match("foo/bar/baz", "(.*)/(.*)/(.*)");
        return new BackReferenceCollection(match.Groups);
    }

    private static BackReferenceCollection CreateTestCondBackReferences()
    {
        var match = Regex.Match("foo/bar/baz", "(.*)/(.*)/(.*)");
        return new BackReferenceCollection(match.Groups);
    }

    private static MatchResults CreateRewriteMapRuleMatch(string input)
    {
        var match = Regex.Match(input, "([^/]*)/?(.*)");
        return new MatchResults(match.Success, new BackReferenceCollection(match.Groups));
    }

    private static MatchResults CreateRewriteMapConditionMatch(string input)
    {
        var match = Regex.Match(input, "(.+)");
        return new MatchResults(match.Success, new BackReferenceCollection(match.Groups));
    }
}
