// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASP0029

using System.ComponentModel.DataAnnotations;
using ConsoleValidationSample.Resources;
using ConsoleValidationSample.Validators;
using Microsoft.Extensions.Validation;

namespace ConsoleValidationSample.Models;

[ValidatableType]
[BannedCustomer("Bob")]
[BannedCustomer("Ted")]
public class Customer
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }

    [Display(Name = "CustomerName")]
    [Required(ErrorMessage = "RequiredError")]
    public string? Name { get; set; }
}
