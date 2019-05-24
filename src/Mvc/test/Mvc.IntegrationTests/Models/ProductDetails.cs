// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests
{
    public class ProductDetails
    {
        [Required]
        public string Detail1 { get; set; }

        [Required]
        public string Detail2 { get; set; }

        [Required]
        public string Detail3 { get; set; }
    }
}
