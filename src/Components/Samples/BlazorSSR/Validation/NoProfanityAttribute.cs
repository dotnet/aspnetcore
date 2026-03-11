// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;

namespace BlazorSSR.Validation;

/// <summary>
/// Validates that the value does not contain any blocked words.
/// Demonstrates a custom validation attribute with both server-side
/// and client-side validation support via <see cref="IClientValidationAdapter"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class NoProfanityAttribute : ValidationAttribute, IClientValidationAdapter
{
    /// <summary>
    /// Comma-separated list of blocked words.
    /// </summary>
    public string BlockedWords { get; set; } = "spam,scam,fake";

    /// <inheritdoc />
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text))
        {
            return ValidationResult.Success;
        }

        var words = BlockedWords.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var word in words)
        {
            if (text.Contains(word, StringComparison.OrdinalIgnoreCase))
            {
                return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
            }
        }

        return ValidationResult.Success;
    }

    /// <summary>
    /// Emits data-val-noprofanity and data-val-noprofanity-words attributes
    /// for client-side validation.
    /// </summary>
    public void AddClientValidation(in ClientValidationContext context, string errorMessage)
    {
        context.MergeAttribute("data-val", "true");
        context.MergeAttribute("data-val-noprofanity", errorMessage);
        context.MergeAttribute("data-val-noprofanity-words", BlockedWords);
    }
}
