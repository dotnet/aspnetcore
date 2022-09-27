// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace FormatterWebSite;

public class Product
{
    public string Name { get; set; }

    public List<Review> Reviews { get; set; } = new List<Review>();
}
