// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;

namespace Microsoft.AspNetCore.Authentication.Core.Test;

public class AuthenticationTicketTests
{
    [Fact]
    public void Clone_Copies()
    {
        var items = new Dictionary<string, string?>
        {
            ["foo"] = "bar",
        };
        var value = "value";
        var parameters = new Dictionary<string, object?>
        {
            ["foo2"] = value,
        };
        var props = new AuthenticationProperties(items, parameters);
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, props, "scheme");

        Assert.Same(items, ticket.Properties.Items);
        Assert.Same(parameters, ticket.Properties.Parameters);
        var copy = ticket.Clone();
        Assert.NotSame(ticket.Principal, copy.Principal);
        Assert.NotSame(ticket.Properties.Items, copy.Properties.Items);
        Assert.NotSame(ticket.Properties.Parameters, copy.Properties.Parameters);
        // Objects in the dictionaries will still be the same
        Assert.Equal(ticket.Properties.Items, copy.Properties.Items);
        Assert.Equal(ticket.Properties.Parameters, copy.Properties.Parameters);
        props.Items["change"] = "good";
        props.Parameters["something"] = "bad";
        Assert.NotEqual(ticket.Properties.Items, copy.Properties.Items);
        Assert.NotEqual(ticket.Properties.Parameters, copy.Properties.Parameters);
        identity.AddClaim(new Claim("name", "value"));
        Assert.True(ticket.Principal.HasClaim("name", "value"));
        Assert.False(copy.Principal.HasClaim("name", "value"));
    }
}
