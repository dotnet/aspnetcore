// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace RazorWebSite.Models
{
    public enum ModelEnum
    {
        [Display(Name = "FirstOptionDisplay")]
        FirstOption,
        SecondOptions
    }

    public class EnumModel
    {
        [Display(Name = "ModelEnum")]
        public ModelEnum Id { get; set; }
    }
}
