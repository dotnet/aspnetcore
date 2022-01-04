// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc;

public class LinkBuilder
{
    public LinkBuilder(string url)
    {
        Url = url;

        Values = new Dictionary<string, object>
            {
                { "link", string.Empty }
            };
    }

    public string Url { get; set; }

    public Dictionary<string, object> Values { get; set; }

    public LinkBuilder To(object values)
    {
        var dictionary = new RouteValueDictionary(values);
        foreach (var kvp in dictionary)
        {
            Values.Add("link_" + kvp.Key, kvp.Value);
        }

        return this;
    }

    public override string ToString()
    {
        return Url + "?" + string.Join("&", Values.Select(kvp => kvp.Key + "=" + kvp.Value));
    }

    public static implicit operator string(LinkBuilder builder)
    {
        return builder.ToString();
    }
}
