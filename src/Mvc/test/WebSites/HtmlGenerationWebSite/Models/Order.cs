// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace HtmlGenerationWebSite.Models;

public class Order
{
    public bool NeedSpecialHandle
    {
        get;
        set;
    }

    public DateTimeOffset OrderDate
    {
        get;
        set;
    }

    public ICollection<string> PaymentMethod
    {
        get;
        set;
    }

    public DateTime ShippingDateTime
    {
        get;
        set;
    }

    public string Shipping
    {
        get;
        set;
    }

    public IEnumerable<int> Products
    {
        get;
        set;
    }

    public IEnumerable<int> SubstituteProducts
    {
        get;
        set;
    }

    public Customer Customer
    {
        get;
        set;
    }

    public IList<Product> ProductDetails { get; } = new List<Product>();
}
