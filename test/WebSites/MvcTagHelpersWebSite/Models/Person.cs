// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace MvcTagHelpersWebSite.Models
{
    public class Person
    {
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