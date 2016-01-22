// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests
{
    [ModelMetadataType(typeof(Product))]
    public class ProductViewModel
    {
        public string Name { get; set; }

        [Required]
        public string Contact { get; set; }

        [Range(20, 100)]
        public int Price { get; set; }

        [RegularExpression("^[a-zA-Z]*$")]
        [Required]
        public string Category { get; set; }

        public string CompanyName { get; set; }

        public string Country { get; set; }

        public ProductDetails ProductDetails { get; set; }
    }
}
