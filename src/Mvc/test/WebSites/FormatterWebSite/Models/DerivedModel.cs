// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace FormatterWebSite.Models;

public class DerivedModel : BaseModel, IModel
{
    [Required]
    [StringLength(10)]
    public string DerivedProperty { get; set; }
}
