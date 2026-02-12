// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using ConsoleValidationSample.Models;
using ConsoleValidationSample.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace ConsoleValidationSample.Validators;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class BannedCustomerAttribute : ValidationAttribute
{
    public string BannedName { get; }

    public BannedCustomerAttribute(string bannedName)
    {
        BannedName = bannedName;
        ErrorMessage ??= "The customer {1} is banned";
    }

    public override string FormatErrorMessage(string name) => string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, BannedName);

    public override bool IsValid(object? value) => value is not Customer customer || customer.Name != BannedName;
}
