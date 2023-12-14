// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// Extension methods for <see cref="IModelBinderProvider"/>.
/// </summary>
public static class ModelBinderProviderExtensions
{
    /// <summary>
    /// Removes all model binder providers of the specified type.
    /// </summary>
    /// <param name="list">The list of <see cref="IModelBinderProvider"/>s.</param>
    /// <typeparam name="TModelBinderProvider">The type to remove.</typeparam>
    public static void RemoveType<TModelBinderProvider>(this IList<IModelBinderProvider> list) where TModelBinderProvider : IModelBinderProvider
    {
        ArgumentNullException.ThrowIfNull(list);

        RemoveType(list, typeof(TModelBinderProvider));
    }

    /// <summary>
    /// Removes all model binder providers of the specified type.
    /// </summary>
    /// <param name="list">The list of <see cref="IModelBinderProvider"/>s.</param>
    /// <param name="type">The type to remove.</param>
    public static void RemoveType(this IList<IModelBinderProvider> list, Type type)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(type);

        for (var i = list.Count - 1; i >= 0; i--)
        {
            var modelBinderProvider = list[i];
            if (modelBinderProvider.GetType() == type)
            {
                list.RemoveAt(i);
            }
        }
    }
}
