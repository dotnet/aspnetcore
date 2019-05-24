// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests
{
    [ProductValidator]
    public class Product
    {
        public string Name { get; set; }

        [StringLength(20)]
        [RegularExpression("^[0-9]*$")]
        [Display(Name = "Contact Us")]
        public string Contact { get; set; }

        [Range(0, 100)]
        public virtual int Price { get; set; }

        [CompanyName]
        public string CompanyName { get; set; }

        public string Country { get; set; }

        [Required]
        public ProductDetails ProductDetails { get; set; }
    }
}
