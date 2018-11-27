// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace FormatterWebSite
{
    public class Developer
    {
        [Required]
        public string Name { get; set; }

        public string Alias { get; set; }
    }
}