// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace BasicTestApp.ValidationModels;

public class AddressModel
{
    [Required(ErrorMessage = "Street is required.")]
    public string Street { get; set; }

    [Required(ErrorMessage = "Zip Code is required.")]
    [StringLength(10, MinimumLength = 5, ErrorMessage = "Zip Code must be between 5 and 10 characters.")]
    public string ZipCode { get; set; }
}
