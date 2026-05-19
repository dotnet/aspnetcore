// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// An <see cref="IValueProvider"/> adapter for data stored in an <see cref="IFormCollection"/>.
/// </summary>
public class FormValueProvider : BindingSourceValueProvider, IEnumerableValueProvider
{
    private readonly IFormCollection _values;
    private readonly HashSet<string?>? _invariantValueKeys;
    private PrefixContainer? _prefixContainer;

    /// <summary>
    /// Creates a value provider for <see cref="IFormCollection"/>.
    /// </summary>
    /// <param name="bindingSource">The <see cref="BindingSource"/> for the data.</param>
    /// <param name="values">The key value pairs to wrap.</param>
    /// <param name="culture">The culture to return with ValueProviderResult instances.</param>
    public FormValueProvider(
        BindingSource bindingSource,
        IFormCollection values,
        CultureInfo? culture)
        : base(bindingSource)
    {
        ArgumentNullException.ThrowIfNull(bindingSource);
        ArgumentNullException.ThrowIfNull(values);

        _values = values;

        if (_values.TryGetValue(FormValueHelper.CultureInvariantFieldName, out var invariantKeys) && invariantKeys.Count > 0)
        {
            _invariantValueKeys = new(invariantKeys, StringComparer.OrdinalIgnoreCase);
        }

        Culture = culture;
    }

    /// <summary>
    /// The culture to use.
    /// </summary>
    public CultureInfo? Culture { get; }

    /// <summary>
    /// The prefix container.
    /// </summary>
    protected PrefixContainer PrefixContainer
    {
        get
        {
            if (_prefixContainer == null)
            {
                _prefixContainer = new PrefixContainer(_values.Keys);
            }

            return _prefixContainer;
        }
    }

    /// <inheritdoc />
    public override bool ContainsPrefix(string prefix)
    {
        return PrefixContainer.ContainsPrefix(prefix);
    }

    /// <inheritdoc />
    public virtual IDictionary<string, string> GetKeysFromPrefix(string prefix)
    {
        ArgumentNullException.ThrowIfNull(prefix);

        return PrefixContainer.GetKeysFromPrefix(prefix);
    }

    /// <inheritdoc />
    public override ValueProviderResult GetValue(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (key.Length == 0)
        {
            // Top level parameters will fall back to an empty prefix when the parameter name does not
            // appear in any value provider. This would result in the parameter binding to a form parameter
            // with a empty key (e.g. Request body looks like "=test") which isn't a scenario we want to support.
            // Return a "None" result in this event.
            return ValueProviderResult.None;
        }

        var values = _values[key];
        if (values.Count == 0)
        {
            return ValueProviderResult.None;
        }
        else
        {
            var culture = _invariantValueKeys?.Contains(key) == true ? CultureInfo.InvariantCulture : Culture;
            return new ValueProviderResult(values, culture);
        }
    }
}
