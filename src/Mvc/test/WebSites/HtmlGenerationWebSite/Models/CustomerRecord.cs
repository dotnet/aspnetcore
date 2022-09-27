// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace HtmlGenerationWebSite.Models;

public record CustomerRecord
(
    [Range(1, 100)]
        int Number,

    string Name,

    [Required]
        string Password,

    [EnumDataType(typeof(Gender))]
        Gender Gender,

    string PhoneNumber,

    [DataType(DataType.EmailAddress)]
        string Email,

    string Key
)
{
    [Required]
    public string Address { get; set; }
}
