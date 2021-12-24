// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Globalization;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// An <see cref="IValueProvider"/> for jQuery formatted query string data.
/// </summary>
public class JQueryQueryStringValueProvider : JQueryValueProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JQueryQueryStringValueProvider"/> class.
    /// </summary>
    /// <param name="bindingSource">The <see cref="BindingSource"/> of the data.</param>
    /// <param name="values">The values.</param>
    /// <param name="culture">The culture to return with ValueProviderResult instances.</param>
    public JQueryQueryStringValueProvider(
        BindingSource bindingSource,
        IDictionary<string, StringValues> values,
        CultureInfo? culture)
        : base(bindingSource, values, culture)
    {
    }
}
