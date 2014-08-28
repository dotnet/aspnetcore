// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace ModelBindingWebSite
{
    public class ModelWithValidation
    {
        [Required]
        public string Field1 { get; set; }

        [Required]
        public string Field2 { get; set; }

        [Required]
        public string Field3 { get; set; }
    }
}