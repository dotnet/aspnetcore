// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace LocalizationWebSite.Models
{
    public class User
    {
        [MinLength(6, ErrorMessage = "NameError")]
        public string Name { get; set; }

        public Product Product { get; set; }
    }

    public class Product
    {
        [Required(ErrorMessage = "ProductName")]
        public string ProductName { get; set; }
    }
}
