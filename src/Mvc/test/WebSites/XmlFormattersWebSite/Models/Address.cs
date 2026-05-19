// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace XmlFormattersWebSite;

public class Address
{
    [Required]
    public string State { get; set; }

    [Required]
    public int Zipcode { get; set; }
}
