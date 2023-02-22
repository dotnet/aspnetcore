// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Http.HttpResults;

internal sealed class TestLinkGenerator : LinkGenerator
{
    public string Url { get; set; }
    public RouteValuesAddress RouteValuesAddress { get; set; }

    public override string GetPathByAddress<TAddress>(HttpContext httpContext, TAddress address, RouteValueDictionary values, RouteValueDictionary ambientValues = null, PathString? pathBase = null, FragmentString fragment = default, LinkOptions options = null)
    {
        throw new NotImplementedException();
    }

    public override string GetPathByAddress<TAddress>(TAddress address, RouteValueDictionary values, PathString pathBase = default, FragmentString fragment = default, LinkOptions options = null)
    {
        throw new NotImplementedException();
    }

    public override string GetUriByAddress<TAddress>(HttpContext httpContext, TAddress address, RouteValueDictionary values, RouteValueDictionary ambientValues = null, string scheme = null, HostString? host = null, PathString? pathBase = null, FragmentString fragment = default, LinkOptions options = null)
        => AssertAddressAndReturnUrl(address);

    public override string GetUriByAddress<TAddress>(TAddress address, RouteValueDictionary values, string scheme, HostString host, PathString pathBase = default, FragmentString fragment = default, LinkOptions options = null)
        => AssertAddressAndReturnUrl(address);

    private string AssertAddressAndReturnUrl<TAddress>(TAddress address)
    {
        RouteValuesAddress = Assert.IsType<RouteValuesAddress>(address);
        return Url;
    }
}
