// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ApiExplorerWebSite;

public class Product
{
    [BindRequired]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; }
}
