// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

namespace RoutingWebSite;

public class SlugifyParameterTransformer : IOutboundParameterTransformer
{
    public string TransformOutbound(object value)
    {
        // Slugify value
        return value == null ? null : Regex.Replace(value.ToString(), "([a-z])([A-Z])", "$1-$2", RegexOptions.None, TimeSpan.FromSeconds(10)).ToLowerInvariant();
    }
}
