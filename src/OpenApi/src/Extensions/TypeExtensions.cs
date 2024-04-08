// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OpenApi;

internal static class TypeExtensions
{
    /// <summary>
    /// Gets the schema reference identifier for the given <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to resolve a schema reference identifier for.</param>
    /// <returns>The schema reference identifier associated with <paramref name="type"/>.</returns>
    public static string GetSchemaReferenceId(this Type type)
    {
        var tnb = new TypeNameBuilder();
        tnb.AddAssemblyQualifiedName(type, TypeNameBuilder.Format.ToString);
        return tnb.ToString();
    }
}
