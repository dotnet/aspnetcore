// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Models;

public class Contact
{
    public int ContactId { get; set; }

    [StringLength(30, MinimumLength = 5)]
    public string Name { get; set; }

    public GenderType Gender { get; set; }

    public string Address { get; set; }

    public string City { get; set; }

    public string State { get; set; }

    [RegularExpression(@"\d{5}")]
    public string Zip { get; set; }

    public string Email { get; set; }

    public string Twitter { get; set; }

    public string Self { get; set; }
}

public class ContactRequest
{
    [FromRoute]
    public int Id { get; set; }

    [FromBody]
    public Contact ContactInfo { get; set; }
}
