// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ApiExplorerWebSite
{
    public class Product
    {
        [BindRequired]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }
    }
}