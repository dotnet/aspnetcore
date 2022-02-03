// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace ApiExplorerWebSite;

public class OrderDTO
{
    public string CustomerId { get; set; }

    [FromHeader(Name = "Referrer")]
    public string ReferrerId { get; set; }

    public OrderDetailsDTO Details { get; set; }

    [FromForm]
    public CustomerCommentsDTO Comments { get; set; }
}
