// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Keyed registry of attribute formatter factories. Built-in formatters for standard
/// attributes with multi-placeholder templates are registered automatically.
/// </summary>
[Experimental("ASP0030")]
public sealed class ValidationAttributeFormatterRegistry
{
    private readonly List<(Type Type, Func<ValidationAttribute, IValidationAttributeFormatter> Factory)> _factories = new();

    public void AddFormatter<TAttribute>(Func<TAttribute, IValidationAttributeFormatter> factory)
        where TAttribute : ValidationAttribute
    {
        _factories.Add(((typeof(TAttribute)), factoryNonGeneric));

        IValidationAttributeFormatter factoryNonGeneric(ValidationAttribute attribute)
            => factory((TAttribute)attribute);
    }

    public IValidationAttributeFormatter? GetFormatter(ValidationAttribute attribute)
    {
        if (attribute is IValidationAttributeFormatter validationAttributeFormatter)
        {
            return validationAttributeFormatter;
        }

        foreach (var (type, factory) in _factories)
        {
            if (attribute.GetType().IsAssignableTo(type))
            {
                return factory(attribute);
            }
        }

        return null;
    }
}
