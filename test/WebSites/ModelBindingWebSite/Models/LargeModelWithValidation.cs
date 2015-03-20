// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace ModelBindingWebSite.Models
{
    public class LargeModelWithValidation
    {
        [Required]
        public ModelWithValidation Field1 { get; set; }

        [Required]
        public ModelWithValidation Field2 { get; set; }

        [Required]
        public ModelWithValidation Field3 { get; set; }

        [Required]
        public ModelWithValidation Field4 { get; set; }

        [Required]
        public ModelWithValidation Field5 { get; set; }

        [Required]
        public ModelWithValidation Field6 { get; set; }

        [Required]
        public ModelWithValidation Field7 { get; set; }

        [Required]
        public ModelWithValidation Field8 { get; set; }
    }
}