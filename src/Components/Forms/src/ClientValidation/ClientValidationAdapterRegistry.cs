// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Registry of <see cref="IClientValidationAdapter"/> factories keyed by
/// <see cref="ValidationAttribute"/> type.
/// </summary>
/// <remarks>
/// <para>
/// Resolution order:
/// </para>
/// <list type="number">
///   <item>
///     <description>
///     If the attribute implements <see cref="IClientValidationAdapter"/> itself (self-adapting),
///     it is returned directly.
///     </description>
///   </item>
///   <item>
///     <description>
///     If a factory is registered for the attribute's type via
///     <see cref="AddAdapter{TAttribute}(Func{TAttribute, IClientValidationAdapter})"/>,
///     it is used to create an adapter.
///     </description>
///   </item>
///   <item>
///     <description>
///     Otherwise, <see langword="null"/> is returned, indicating that no adapter
///     is available for the attribute.
///     </description>
///   </item>
/// </list>
/// <para>
/// Built-in adapters for standard validation attributes (such as <see cref="RequiredAttribute"/>,
/// <see cref="RangeAttribute"/>, <see cref="StringLengthAttribute"/>, etc.) are registered
/// automatically when <c>AddClientSideValidation</c> is called.
/// Later registrations for the same attribute type replace earlier ones.
/// </para>
/// <example>
/// <code>
/// builder.Services.AddClientValidationAdapter&lt;CreditCardAttribute&gt;(
///     attribute =&gt; new CreditCardAttributeAdapter(attribute));
/// </code>
/// </example>
/// </remarks>
public sealed class ClientValidationAdapterRegistry
{
    private readonly Dictionary<Type, Func<ValidationAttribute, IClientValidationAdapter>> _factories = new();

    /// <summary>
    /// Registers an adapter factory for the specified validation attribute type.
    /// Later registrations for the same type replace earlier ones.
    /// </summary>
    /// <typeparam name="TAttribute">The validation attribute type to register an adapter for.</typeparam>
    /// <param name="factory">
    /// A factory delegate that creates an <see cref="IClientValidationAdapter"/>
    /// from the attribute instance.
    /// </param>
    public void AddAdapter<TAttribute>(Func<TAttribute, IClientValidationAdapter> factory)
        where TAttribute : ValidationAttribute
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factories[typeof(TAttribute)] = attribute => factory((TAttribute)attribute);
    }

    /// <summary>
    /// Returns an <see cref="IClientValidationAdapter"/> for the specified <paramref name="attribute"/>.
    /// If the attribute implements <see cref="IClientValidationAdapter"/> itself, it is returned directly.
    /// Otherwise, the registry is consulted. Returns <see langword="null"/> if no adapter is registered
    /// for the attribute's type.
    /// </summary>
    /// <param name="attribute">The validation attribute to get an adapter for.</param>
    /// <returns>
    /// An <see cref="IClientValidationAdapter"/> if the attribute self-adapts or a factory
    /// is registered; otherwise, <see langword="null"/>.
    /// </returns>
    public IClientValidationAdapter? GetAdapter(ValidationAttribute attribute)
    {
        if (attribute is IClientValidationAdapter selfAdapter)
        {
            return selfAdapter;
        }

        if (_factories.TryGetValue(attribute.GetType(), out var factory))
        {
            return factory(attribute);
        }

        return null;
    }
}
