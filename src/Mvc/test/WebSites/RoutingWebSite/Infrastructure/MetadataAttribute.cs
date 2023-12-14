// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Mvc.RoutingWebSite.Infrastructure;

internal class MetadataAttribute : Attribute
{
    public string Value { get; set; }

    public MetadataAttribute(string value)
    {
        Value = value;
    }
}

