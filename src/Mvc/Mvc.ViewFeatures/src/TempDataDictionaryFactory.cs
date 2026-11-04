// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <summary>
/// A default implementation of <see cref="ITempDataDictionaryFactory"/>.
/// </summary>
public class TempDataDictionaryFactory : ITempDataDictionaryFactory
{
    private static readonly object Key = typeof(ITempDataDictionary);

    private readonly ITempDataProvider _provider;

    /// <summary>
    /// Creates a new <see cref="TempDataDictionaryFactory"/>.
    /// </summary>
    /// <param name="provider">The <see cref="ITempDataProvider"/>.</param>
    public TempDataDictionaryFactory(ITempDataProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        _provider = provider;
    }

    /// <inheritdoc />
    public ITempDataDictionary GetTempData(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        object obj;
        ITempDataDictionary result;
        if (context.Items.TryGetValue(Key, out obj))
        {
            result = (ITempDataDictionary)obj;
        }
        else
        {
            result = new TempDataDictionary(context, _provider);
            context.Items.Add(Key, result);
        }

        return result;
    }
}
