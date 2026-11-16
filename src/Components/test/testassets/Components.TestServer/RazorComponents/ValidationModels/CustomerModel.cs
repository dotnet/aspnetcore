// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;

namespace BasicTestApp.ValidationModels;

public class CustomerModel
{
    [Required(ErrorMessage = "Full Name is required.")]
    public string FullName { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid Email Address.")]
    public string Email { get; set; }

    public AddressModel PaymentAddress { get; set; } = new AddressModel();

    [SkipValidation]
    public AddressModel ShippingAddress { get; set; } = new AddressModel();

}
