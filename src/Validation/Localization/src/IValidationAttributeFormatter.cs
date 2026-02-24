// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Microsoft.Extensions.Validation.Localization;

/// <summary>
/// Formats a validation error message template with attribute-specific arguments.
/// The default validation localization pipeline uses <see cref="ValidationAttributeFormatterProvider"/>
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
///     <see cref="ValidationAttributeFormatterProvider"/> checks for this first and uses the
///     attribute as its own formatter automatically.
///     </description>
///   </item>
///   <item>
///     <description>
///     Create a separate <see cref="IValidationAttributeFormatter"/> implementation and a custom
///     <see cref="IValidationAttributeFormatterProvider"/> that returns it. Derive from
///     <see cref="ValidationAttributeFormatterProvider"/> and fall back to the base implementation
///     for all other attributes.
///     </description>
///   </item>
/// </list>
/// <example>
/// The following example shows a formatter provider that handles a custom attribute and delegates
/// to the base class for everything else:
/// <code>
/// public class MyAttributeFormatter(MyAttribute attribute) : IValidationAttributeFormatter
/// {
///     public string FormatErrorMessage(CultureInfo culture, string messageTemplate, string displayName)
///         => string.Format(culture, messageTemplate, displayName, attribute.CustomProperty);
/// }
///
/// public class CustomFormatterProvider : ValidationAttributeFormatterProvider
/// {
///     public override IValidationAttributeFormatter GetFormatter(ValidationAttribute attribute)
///     {
///         if (attribute is MyAttribute myAttribute)
///         {
///             return new MyAttributeFormatter(myAttribute);
///         }
///
///         return base.GetFormatter(attribute);
///     }
/// }
/// </code>
/// Register the provider into dependency injection to make it available to the localization pipeline:
/// <code>
/// builder.Services.AddSingleton&lt;IValidationAttributeFormatterProvider, CustomFormatterProvider&gt;();
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
