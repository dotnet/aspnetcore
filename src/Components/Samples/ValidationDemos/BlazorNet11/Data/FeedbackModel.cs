// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace BlazorNet11.Data;

// Convention-based error message localization:
// See the ErrorMessageKeyProvider configured in AddValidation().

public class FeedbackModel
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    [Display(Name = "Your Name")]
    public string Name { get; set; } = "";

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = "";

    [Required]
    [Range(1, 5)]
    [Display(Name = "Rating")]
    public int? Rating { get; set; }

    [Required]
    [StringLength(500, MinimumLength = 10)]
    [Display(Name = "Comments")]
    public string Comments { get; set; } = "";
}
