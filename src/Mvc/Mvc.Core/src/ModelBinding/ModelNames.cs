// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Globalization;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// Static class for helpers dealing with model names.
/// </summary>
public static class ModelNames
{
    /// <summary>
    /// Create an index model name from the parent name.
    /// </summary>
    /// <param name="parentName">The parent name.</param>
    /// <param name="index">The index.</param>
    /// <returns>The index model name.</returns>
    public static string CreateIndexModelName(string parentName, int index)
    {
        return CreateIndexModelName(parentName, index.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Create an index model name from the parent name.
    /// </summary>
    /// <param name="parentName">The parent name.</param>
    /// <param name="index">The index.</param>
    /// <returns>The index model name.</returns>
    public static string CreateIndexModelName(string parentName, string index)
    {
        return (parentName.Length == 0) ? "[" + index + "]" : parentName + "[" + index + "]";
    }

    /// <summary>
    /// Create a property model name with a prefix.
    /// </summary>
    /// <param name="prefix">The prefix to use.</param>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The property model name.</returns>
    public static string CreatePropertyModelName(string? prefix, string? propertyName)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            return propertyName ?? string.Empty;
        }

        if (string.IsNullOrEmpty(propertyName))
        {
            return prefix ?? string.Empty;
        }

        if (propertyName.StartsWith('['))
        {
            // The propertyName might represent an indexer access, in which case combining
            // with a 'dot' would be invalid. This case occurs only when called from ValidationVisitor.
            return prefix + propertyName;
        }

        return prefix + "." + propertyName;
    }
}
