// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests;

[ModelMetadataType(typeof(Product))]
public class ProductViewModel
{
    public string Name { get; set; }

    [Required]
    public string Contact { get; set; }

    [Range(20, 100)]
    public int Price { get; set; }

    [RegularExpression("^[a-zA-Z]*$")]
    [Required]
    public string Category { get; set; }

    public string CompanyName { get; set; }

    public string Country { get; set; }

    public ProductDetails ProductDetails { get; set; }
}
