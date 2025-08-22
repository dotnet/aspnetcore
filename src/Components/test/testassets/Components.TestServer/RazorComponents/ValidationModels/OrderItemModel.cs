// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace BasicTestApp.ValidationModels;

public class OrderItemModel
{
    [Required(ErrorMessage = "Product Name is required.")]
    public string ProductName { get; set; }

    [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100.")]
    public int Quantity { get; set; } = 1;
}
