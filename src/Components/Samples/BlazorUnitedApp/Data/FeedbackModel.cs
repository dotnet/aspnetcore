// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace BlazorUnitedApp.Data;

// A second, deliberately different model so navigating between the two client-validation form
// pages with enhanced navigation exercises the JS reconcile (a reused carrier whose payload
// changes must drop the old form's rules and register the new ones).
public class FeedbackModel
{
    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(50, ErrorMessage = "Subject must be at most 50 characters.")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Message is required.")]
    [RegularExpression(@"^[\w\s.,!?]+$", ErrorMessage = "Message may only contain letters, numbers, spaces and . , ! ? characters.")]
    public string Message { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public int Rating { get; set; }
}
