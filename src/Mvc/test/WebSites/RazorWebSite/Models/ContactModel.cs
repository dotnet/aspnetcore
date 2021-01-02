// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace RazorWebSite.Models
{
    public class ContactModel
    {
        [Display(Name = "Name")]
        [Required(ErrorMessage = "Required")]
        [StringLength(80, MinimumLength = 3, ErrorMessage = "StringLength")]
        public string Name { get; set; }

        [Display(Name = "Email")]
        [DataType(DataType.EmailAddress)]
        [Required(ErrorMessage = "Required")]
        [EmailAddress(ErrorMessage = "EmailAddress")]
        [StringLength(80, ErrorMessage = "StringLength")]
        public string Email { get; set; }
    }
}
