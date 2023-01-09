// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// Extension methods for <see cref="IValueProviderFactory"/>.
/// </summary>
public static class ValueProviderFactoryExtensions
{
    /// <summary>
    /// Removes all value provider factories of the specified type.
    /// </summary>
    /// <param name="list">The list of <see cref="IValueProviderFactory"/>.</param>
    /// <typeparam name="TValueProviderFactory">The type to remove.</typeparam>
    public static void RemoveType<TValueProviderFactory>(this IList<IValueProviderFactory> list) where TValueProviderFactory : IValueProviderFactory
    {
        ArgumentNullException.ThrowIfNull(list);

        RemoveType(list, typeof(TValueProviderFactory));
    }

    /// <summary>
    /// Removes all value provider factories of the specified type.
    /// </summary>
    /// <param name="list">The list of <see cref="IValueProviderFactory"/>.</param>
    /// <param name="type">The type to remove.</param>
    public static void RemoveType(this IList<IValueProviderFactory> list, Type type)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(type);

        for (var i = list.Count - 1; i >= 0; i--)
        {
            var valueProviderFactory = list[i];
            if (valueProviderFactory.GetType() == type)
            {
                list.RemoveAt(i);
            }
        }
    }
}
