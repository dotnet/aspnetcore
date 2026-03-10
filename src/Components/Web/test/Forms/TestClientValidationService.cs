// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Simple test implementation of IClientValidationService that uses reflection
/// to discover validation attributes and emits data-val-* attributes directly.
/// </summary>
internal sealed class TestClientValidationService : IClientValidationService
{
    public IReadOnlyDictionary<string, string> GetValidationAttributes(FieldIdentifier fieldIdentifier)
    {
        var modelType = fieldIdentifier.Model.GetType();
        var property = modelType.GetProperty(fieldIdentifier.FieldName);
        if (property is null)
        {
            return ImmutableDictionary<string, string>.Empty;
        }

        var validationAttributes = property.GetCustomAttributes<ValidationAttribute>(inherit: true);
        var displayName = ResolveDisplayName(property, fieldIdentifier.FieldName);
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var attr in validationAttributes)
        {
            var errorMessage = attr.FormatErrorMessage(displayName);

            switch (attr)
            {
                case RequiredAttribute:
                    attributes.TryAdd("data-val", "true");
                    attributes.TryAdd("data-val-required", errorMessage);
                    break;
                case StringLengthAttribute sl:
                    attributes.TryAdd("data-val", "true");
                    attributes.TryAdd("data-val-length", errorMessage);
                    if (sl.MaximumLength != int.MaxValue)
                    {
                        attributes.TryAdd("data-val-length-max", sl.MaximumLength.ToString(CultureInfo.InvariantCulture));
                    }
                    if (sl.MinimumLength != 0)
                    {
                        attributes.TryAdd("data-val-length-min", sl.MinimumLength.ToString(CultureInfo.InvariantCulture));
                    }
                    break;
                case EmailAddressAttribute:
                    attributes.TryAdd("data-val", "true");
                    attributes.TryAdd("data-val-email", errorMessage);
                    break;
                case RangeAttribute r:
                    attributes.TryAdd("data-val", "true");
                    attributes.TryAdd("data-val-range", errorMessage);
                    attributes.TryAdd("data-val-range-min", Convert.ToString(r.Minimum, CultureInfo.InvariantCulture)!);
                    attributes.TryAdd("data-val-range-max", Convert.ToString(r.Maximum, CultureInfo.InvariantCulture)!);
                    break;
                case CompareAttribute c:
                    attributes.TryAdd("data-val", "true");
                    attributes.TryAdd("data-val-equalto", errorMessage);
                    attributes.TryAdd("data-val-equalto-other", "*." + c.OtherProperty);
                    break;
                case RegularExpressionAttribute re:
                    attributes.TryAdd("data-val", "true");
                    attributes.TryAdd("data-val-regex", errorMessage);
                    attributes.TryAdd("data-val-regex-pattern", re.Pattern);
                    break;
            }
        }

        return attributes.Count > 0 ? attributes : ImmutableDictionary<string, string>.Empty;
    }

    private static string ResolveDisplayName(PropertyInfo property, string fieldName)
    {
        var displayAttr = property.GetCustomAttribute<DisplayAttribute>();
        if (displayAttr?.GetName() is { } name)
        {
            return name;
        }

        var displayNameAttr = property.GetCustomAttribute<DisplayNameAttribute>();
        if (displayNameAttr?.DisplayName is not null)
        {
            return displayNameAttr.DisplayName;
        }

        return fieldName;
    }
}
