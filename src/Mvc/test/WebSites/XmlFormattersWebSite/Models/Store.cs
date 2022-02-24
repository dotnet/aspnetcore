// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace XmlFormattersWebSite;

public class Store
{
    [Required]
    public int Id { get; set; }

    [Required]
    public Address Address { get; set; }
}
