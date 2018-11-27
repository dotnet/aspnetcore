// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace BasicWebSite.Models
{
    public class Contact
    {
        public int ContactId { get; set; }

        [StringLength(30, MinimumLength = 5)]
        public string Name { get; set; }

        public GenderType Gender { get; set; }

        public string Address { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        [RegularExpression(@"\d{5}")]
        public string Zip { get; set; }

        public string Email { get; set; }

        public string Twitter { get; set; }

        public string Self { get; set; }
    }
}