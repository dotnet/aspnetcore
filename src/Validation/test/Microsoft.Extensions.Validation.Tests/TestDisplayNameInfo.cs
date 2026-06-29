// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Microsoft.Extensions.Validation.Localization;

namespace Microsoft.Extensions.Validation.Tests;

/// <summary>
/// Test double for the SG-emitted <c>LiteralDisplayName</c> strategy. Carries a literal
/// display name and runs it through <see cref="IValidationLocalizer"/> when one is configured
/// on <see cref="ValidationOptions.Localizer"/>; otherwise returns the literal.
/// </summary>
internal sealed class TestLiteralDisplayName(string literal) : DisplayNameInfo
{
    public override string? GetDisplayName(ValidateContext context, string memberName, Type? type)
    {
        var localizer = context.ValidationOptions.Localizer;
        if (localizer is null)
        {
            return literal;
        }

        // Literal acts as both lookup key and fallback display name when the localizer doesn't translate.
        return localizer.ResolveDisplayName(new DisplayNameLocalizationContext
        {
            Type = type,
            DisplayName = literal,
            MemberName = memberName,
        }) ?? literal;
    }
}

/// <summary>
/// Test double for the SG-emitted <c>PropertyResourceDisplayName</c> / <c>TypeResourceDisplayName</c>
/// strategies. Delegates to a <see cref="Func{TResult}"/> that simulates a static resource accessor
/// (e.g. <c>DisplayAttribute.GetName()</c>) and intentionally bypasses the localizer to mirror the
/// canonical resource-attribute path.
/// </summary>
internal sealed class TestResourceDisplayName(Func<string?> accessor) : DisplayNameInfo
{
    public override string? GetDisplayName(ValidateContext context, string memberName, Type? declaringType)
        => accessor();
}
