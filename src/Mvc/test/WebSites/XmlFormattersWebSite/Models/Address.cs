// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace XmlFormattersWebSite
{
    public class Address
    {
        [Required]
        public string State { get; set; }

        [Required]
        public int Zipcode { get; set; }
    }
}