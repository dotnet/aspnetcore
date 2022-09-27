// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.IISUrlRewrite;

namespace Microsoft.AspNetCore.Rewrite.Tests.UrlRewrite;

// TODO add more of these
public class UrlRewriteApplicationTests
{
    [Fact]
    public void ApplyRule_AssertStopProcessingFlagWillTerminateOnNoAction()
    {
        var xml = new StringReader(@"<rewrite>
                <rules>
                <rule name=""Test"" stopProcessing=""true"">
                <match url = ""(.*)""/>
                <action type = ""None""/>
                </rule>
                </rules>
                </rewrite>");
        var rules = new UrlRewriteFileParser().Parse(xml, false);

        Assert.Equal(1, rules.Count);
        var context = new RewriteContext { HttpContext = new DefaultHttpContext() };
        rules.FirstOrDefault().ApplyRule(context);
        Assert.Equal(RuleResult.SkipRemainingRules, context.Result);
    }

    [Fact]
    public void ApplyRule_AssertNoTerminateFlagWillNotTerminateOnNoAction()
    {
        var xml = new StringReader(@"<rewrite>
                <rules>
                <rule name=""Test"">
                <match url = ""(.*)"" ignoreCase=""false"" />
                <action type = ""None""/>
                </rule>
                </rules>
                </rewrite>");
        var rules = new UrlRewriteFileParser().Parse(xml, false);

        Assert.Equal(1, rules.Count);
        var context = new RewriteContext { HttpContext = new DefaultHttpContext() };
        rules.FirstOrDefault().ApplyRule(context);
        Assert.Equal(RuleResult.ContinueRules, context.Result);
    }

    [Fact]
    public void ApplyRule_TrackAllCaptures()
    {
        var xml = new StringReader(@"<rewrite>
                <rules>
                <rule name=""Test"">
                <match url = ""(.*)"" ignoreCase=""false"" />
                <conditions trackAllCaptures = ""true"" >
                <add input = ""{REQUEST_URI}"" pattern = ""^/([a-zA-Z]+)/([0-9]+)/$"" />
                </conditions >
                <action type = ""None""/>
                </rule>
                </rules>
                </rewrite>");
        var rules = new UrlRewriteFileParser().Parse(xml, false);

        Assert.Equal(1, rules.Count);
        Assert.True(rules[0].Conditions.TrackAllCaptures);
        var context = new RewriteContext { HttpContext = new DefaultHttpContext() };
        rules.FirstOrDefault().ApplyRule(context);
        Assert.Equal(RuleResult.ContinueRules, context.Result);
    }
}
