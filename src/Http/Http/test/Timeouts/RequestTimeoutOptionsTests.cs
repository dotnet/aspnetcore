// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Timeouts;

namespace Microsoft.AspNetCore.Http.Tests.Timeouts;

public class RequestTimeoutOptionsTests
{
    [Fact]
    public void AddPolicyTimeSpanWorks()
    {
        var options = new RequestTimeoutOptions();
        options.AddPolicy("policy1", TimeSpan.FromSeconds(47));

        var policy = options.Policies["policy1"];
        Assert.Equal(TimeSpan.FromSeconds(47), policy.Timeout);
    }

    [Fact]
    public void AddPolicyWorks()
    {
        var options = new RequestTimeoutOptions();
        var addedPolicy = new RequestTimeoutPolicy
        {
            Timeout = TimeSpan.FromSeconds(47)
        };

        options.AddPolicy("policy1", addedPolicy);

        var policy = options.Policies["policy1"];
        Assert.Equal(TimeSpan.FromSeconds(47), policy.Timeout);
    }

    [Fact]
    public void AddPolicyOverrideExistingPolicy()
    {
        var options = new RequestTimeoutOptions();
        options.AddPolicy("policy1", TimeSpan.FromSeconds(1));

        options.AddPolicy("policy1", TimeSpan.FromSeconds(47));
        Assert.Equal(TimeSpan.FromSeconds(47), options.Policies["policy1"].Timeout);
    }

    [Fact]
    public void AddNullPolicyThrows()
    {
        var options = new RequestTimeoutOptions();
        Assert.Throws<ArgumentException>(() => options.AddPolicy("", TimeSpan.FromSeconds(47)));
        Assert.Throws<ArgumentNullException>(() => options.AddPolicy(null, TimeSpan.FromSeconds(47)));

        Assert.Throws<ArgumentException>(() => options.AddPolicy("", new RequestTimeoutPolicy()));
        Assert.Throws<ArgumentNullException>(() => options.AddPolicy(null, new RequestTimeoutPolicy()));

        Assert.Throws<ArgumentNullException>(() => options.AddPolicy("policy1", null));
    }
}
