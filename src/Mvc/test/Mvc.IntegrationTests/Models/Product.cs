// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests;

[ProductValidator]
public class Product
{
    public string Name { get; set; }

    [StringLength(20)]
    [RegularExpression("^[0-9]*$")]
    [Display(Name = "Contact Us")]
    public string Contact { get; set; }

    [Range(0, 100)]
    public virtual int Price { get; set; }

    [CompanyName]
    public string CompanyName { get; set; }

    public string Country { get; set; }

    [Required]
    public ProductDetails ProductDetails { get; set; }
}
