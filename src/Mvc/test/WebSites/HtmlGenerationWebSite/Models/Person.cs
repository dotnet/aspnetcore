// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace HtmlGenerationWebSite.Models
{
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
}