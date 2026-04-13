// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Registry of <see cref="IValidationAttributeFormatter"/> factories keyed by
/// <see cref="ValidationAttribute"/> type. Used by the validation localization pipeline
/// to format localized error message templates with attribute-specific arguments.
/// </summary>
/// <remarks>
/// <para>
/// Resolution order:
/// </para>
/// <list type="number">
///   <item>
///     <description>
///     If the attribute implements <see cref="IValidationAttributeFormatter"/> itself (self-formatting),
///     it is returned directly.
///     </description>
///   </item>
///   <item>
///     <description>
///     If a factory is registered for the attribute's type via
///     <see cref="AddFormatter{TAttribute}(Func{TAttribute, IValidationAttributeFormatter})"/>,
///     it is used to create a formatter.
///     </description>
///   </item>
///   <item>
///     <description>
///     Otherwise, <see langword="null"/> is returned, indicating that no attribute-specific
///     formatter is available.
///     </description>
///   </item>
/// </list>
/// <para>
/// Built-in formatters for standard validation attributes (such as <see cref="RangeAttribute"/>,
/// <see cref="MinLengthAttribute"/>, <see cref="StringLengthAttribute"/>, etc.) are registered
/// automatically. Later registrations for the same attribute type replace earlier ones.
/// </para>
/// <example>
/// <code>
/// builder.Services.AddValidationAttributeFormatter&lt;CreditCardAttribute&gt;(
///     attribute =&gt; new CreditCardAttributeFormatter(attribute));
/// </code>
/// </example>
/// </remarks>
public sealed class ValidationAttributeFormatterRegistry
{
    private readonly Dictionary<Type, Func<ValidationAttribute, IValidationAttributeFormatter>> _factories = new();

    /// <summary>
    /// Creates a new instance of <see cref="ValidationAttributeFormatterRegistry"/>
    /// with built-in formatters for standard validation attributes.
    /// </summary>
    public ValidationAttributeFormatterRegistry()
    {
        AddFormatter<RangeAttribute>(a => new RangeAttributeFormatter(a));
        AddFormatter<MinLengthAttribute>(a => new MinLengthAttributeFormatter(a));
        AddFormatter<MaxLengthAttribute>(a => new MaxLengthAttributeFormatter(a));
        AddFormatter<LengthAttribute>(a => new LengthAttributeFormatter(a));
        AddFormatter<StringLengthAttribute>(a => new StringLengthAttributeFormatter(a));
        AddFormatter<RegularExpressionAttribute>(a => new RegularExpressionAttributeFormatter(a));
        AddFormatter<FileExtensionsAttribute>(a => new FileExtensionsAttributeFormatter(a));
        AddFormatter<CompareAttribute>(a => new CompareAttributeFormatter(a));
    }

    /// <summary>
    /// Registers a formatter factory for the specified validation attribute type.
    /// Later registrations for the same type replace earlier ones.
    /// </summary>
    /// <typeparam name="TAttribute">The validation attribute type to register a formatter for.</typeparam>
    /// <param name="factory">
    /// A factory delegate that creates an <see cref="IValidationAttributeFormatter"/>
    /// from the attribute instance.
    /// </param>
    public void AddFormatter<TAttribute>(Func<TAttribute, IValidationAttributeFormatter> factory)
        where TAttribute : ValidationAttribute
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factories[typeof(TAttribute)] = attribute => factory((TAttribute)attribute);
    }

    /// <summary>
    /// Returns an <see cref="IValidationAttributeFormatter"/> for the specified <paramref name="attribute"/>.
    /// If the attribute implements <see cref="IValidationAttributeFormatter"/> itself, it is returned directly.
    /// Otherwise, the registry is consulted. Returns <see langword="null"/> if no formatter is registered
    /// for the attribute's type.
    /// </summary>
    /// <param name="attribute">The validation attribute to get a formatter for.</param>
    /// <returns>
    /// An <see cref="IValidationAttributeFormatter"/> if the attribute self-formats or a factory
    /// is registered; otherwise, <see langword="null"/>.
    /// </returns>
    public IValidationAttributeFormatter? GetFormatter(ValidationAttribute attribute)
    {
        if (attribute is IValidationAttributeFormatter selfFormatter)
        {
            return selfFormatter;
        }

        if (_factories.TryGetValue(attribute.GetType(), out var factory))
        {
            return factory(attribute);
        }

        return null;
    }
}
