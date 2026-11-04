// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace BasicWebSite.Models;

[DisplayColumn("Name")]
public class User
{
    [Required]
    [MinLength(4)]
    public string Name { get; set; }
    public string Address { get; set; }
}
