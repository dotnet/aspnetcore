// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
