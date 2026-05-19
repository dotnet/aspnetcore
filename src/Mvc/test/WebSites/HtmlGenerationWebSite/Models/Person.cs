// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace HtmlGenerationWebSite.Models;

public class Person
{
    [HiddenInput(DisplayValue = false)]
    [Range(1, 100)]
    public int Number
    {
        get;
        set;
    }

    public string Name
    {
        get;
        set;
    }

    [Required]
    public string Password
    {
        get;
        set;
    }

    [EnumDataType(typeof(Gender))]
    [UIHint("GenderUsingTagHelpers")]
    public Gender Gender
    {
        get;
        set;
    }

    public string PhoneNumber
    {
        get;
        set;
    }

    [DataType(DataType.EmailAddress)]
    public string Email
    {
        get;
        set;
    }
}
