// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace HtmlGenerationWebSite.Models;

public class Item
{
    [UIHint("Common")]
    [Display(Name = "ItemName")]
    public string Name { get; set; }

    [UIHint("Common")]
    [Display(Name = "ItemNo")]
    public int Id { get; set; }

    [Display(Name = "ItemDesc")]
    public string Description { get; set; }
}
