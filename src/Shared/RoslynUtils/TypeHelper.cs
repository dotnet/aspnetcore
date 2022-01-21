// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace System.Runtime.CompilerServices;

internal static class TypeHelper
{
    /// <summary>
    /// Checks to see if a given type is compiler generated.
    /// <remarks>
    /// The compiler will annotate either the target type or the declaring type
    /// with the CompilerGenerated attribute. We walk up the declaring types until
    /// we find a CompilerGenerated attribute or declare the type as not compiler
    /// generated otherwise.
    /// </remarks>
    /// </summary>
    /// <param name="type">The type to evaluate.</param>
    /// <returns><see langword="true" /> if <paramref name="type"/> is compiler generated.</returns>
    internal static bool IsCompilerGeneratedType(Type? type = null)
    {
        if (type is not null)
        {
            return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute)) || IsCompilerGeneratedType(type.DeclaringType);
        }
        return false;
    }

    /// <summary>
    /// Checks to see if a given method is compiler generated.
    /// </summary>
    /// <param name="method">The method to evaluate.</param>
    /// <returns><see langword="true" /> if <paramref name="method"/> is compiler generated.</returns>
    internal static bool IsCompilerGeneratedMethod(MethodInfo method)
    {
        return Attribute.IsDefined(method, typeof(CompilerGeneratedAttribute)) || IsCompilerGeneratedType(method.DeclaringType);
    }
}
