// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace RazorWebSite.Models;

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
