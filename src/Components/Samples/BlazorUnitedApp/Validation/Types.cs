// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.Validation;

namespace BlazorUnitedApp.Validation;

#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
[ValidatableType]
#pragma warning restore ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
public class GuestbookEntry
{
    [Required]
    public Author Author { get; set; } = new Author();

    [Required]
    public string? Title { get; set; }

    public List<Message> Messages { get; set; } = [];
}

public class Message
{
    [Required]
    [StringLength(50, ErrorMessage = "Name is too long.")]
    public string? Text { get; set; }
}

public class Author
{
    [Required]
    public string? Name { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public Address Address { get; set; } = new Address();

    [Range(0, 150)]
    public int? Age { get; set; }
}

public class Address
{
    [Required]
    public string? City { get; set; }

    public string? Street { get; set; }
}
