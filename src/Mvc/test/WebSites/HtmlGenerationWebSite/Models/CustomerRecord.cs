// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace HtmlGenerationWebSite.Models
{
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
    );
}