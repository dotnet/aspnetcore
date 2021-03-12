// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BasicApi.Models
{
    public class Pet
    {
        public int Id { get; set; }

        [Range(0, 150)]
        public int Age { get; set; }

        public Category Category { get; set; }

        public bool HasVaccinations { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string Name { get; set; }

        public List<Image> Images { get; set; }

        public List<Tag> Tags { get; set; }

        [Required]
        public string Status { get; set; }
    }
}
