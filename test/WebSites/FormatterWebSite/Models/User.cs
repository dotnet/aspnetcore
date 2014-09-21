// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace FormatterWebSite
{
    public class User
    {
        [Required, Range(1, 2000)]
        public int Id { get; set; }

        [Required, MinLength(5)]
        public string Name { get; set; }

        [StringLength(15, MinimumLength = 3)]
        public string Alias { get; set; }

        [RegularExpression("[0-9a-zA-Z]*")]
        public string Designation { get; set; }

        public string description { get; set; }
    }
}