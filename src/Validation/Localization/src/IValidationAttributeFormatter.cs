// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Microsoft.Extensions.Validation.Localization;

/// <summary>
/// Formats a validation error message template with attribute-specific arguments.
/// The default validation localization pipeline uses <see cref="ValidationAttributeFormatterRegistry"/>
/// to retrieve a formatter for the built-in <see cref="System.ComponentModel.DataAnnotations.ValidationAttribute"/> types.
/// </summary>
/// <remarks>
/// <para>
/// To add formatting support for a custom validation attribute, you have two options:
/// </para>
/// <list type="number">
///   <item>
///     <description>
///     Implement <see cref="IValidationAttributeFormatter"/> directly on the attribute itself.
///     <see cref="ValidationAttributeFormatterRegistry"/> checks for this first and uses the
///     attribute as its own formatter automatically.
///     </description>
///   </item>
///   <item>
///     <description>
///     Create a separate <see cref="IValidationAttributeFormatter"/> implementation and register
///     it using the <c>AddValidationAttributeFormatter&lt;TAttribute&gt;</c> extension method.
///     </description>
///   </item>
/// </list>
/// <example>
/// The following example shows how to register a formatter for a custom attribute:
/// <code>
/// public class MyAttributeFormatter(MyAttribute attribute) : IValidationAttributeFormatter
/// {
///     public string FormatErrorMessage(CultureInfo culture, string messageTemplate, string displayName)
///         => string.Format(culture, messageTemplate, displayName, attribute.CustomProperty);
/// }
///
/// // Register it in Program.cs:
/// builder.Services.AddValidationAttributeFormatter&lt;MyAttribute&gt;(
///     attribute =&gt; new MyAttributeFormatter(attribute));
/// </code>
/// </example>
/// </remarks>
public interface IValidationAttributeFormatter
{
    /// <summary>
    /// Formats the specified <paramref name="messageTemplate"/> by substituting attribute-specific
    /// arguments alongside the <paramref name="displayName"/>.
    /// </summary>
    /// <param name="culture">The <see cref="CultureInfo"/> to use when formatting.</param>
    /// <param name="messageTemplate">The error message template containing format placeholders.</param>
    /// <param name="displayName">The resolved display name of the member being validated.</param>
    /// <returns>The fully formatted error message.</returns>
    string FormatErrorMessage(CultureInfo culture, string messageTemplate, string displayName);
}
