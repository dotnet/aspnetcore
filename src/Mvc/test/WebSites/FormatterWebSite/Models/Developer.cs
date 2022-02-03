// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace FormatterWebSite;

public class Developer
{
    [Required]
    public string Name { get; set; }

    public string Alias { get; set; }
}
