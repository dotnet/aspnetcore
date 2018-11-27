// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;

namespace HtmlGenerationWebSite.Models
{
    public class Product
    {
        [Required]
        public string ProductName
        {
            get;
            set;
        }

        public int Number
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        public Uri HomePage
        {
            get;
            set;
        }
    }
}