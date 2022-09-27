// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.ApacheModRewrite;
using Microsoft.AspNetCore.Rewrite.UrlActions;

namespace Microsoft.AspNetCore.Rewrite.Tests;

public class RuleBuilderTest
{
    [Fact]
    // see https://httpd.apache.org/docs/2.4/rewrite/advanced.html#setenvvars
    public void AddAction_Throws_ChangeEnvNotSupported()
    {
        var builder = new RuleBuilder();
        var flags = new Flags();
        flags.SetFlag(FlagType.Env, "rewritten:1");

        var ex = Assert.Throws<NotSupportedException>(() => builder.AddAction(null, flags));
        Assert.Equal(Resources.Error_ChangeEnvironmentNotSupported, ex.Message);
    }

    [Fact]
    public void AddAction_DefaultRedirectStatusCode()
    {
        var builder = new RuleBuilder();
        var flags = new Flags();
        var pattern = new Pattern(new List<PatternSegment>());
        flags.SetFlag(FlagType.Redirect, string.Empty);

        builder.AddAction(pattern, flags);
        var redirectAction = (RedirectAction)builder._actions[0];

        Assert.Equal(StatusCodes.Status302Found, redirectAction.StatusCode);
    }
}
