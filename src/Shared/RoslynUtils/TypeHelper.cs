// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    internal static class TypeHelper
    {
        /// <summary>
        /// Checks to see if a given type is compiler generated.
        /// <remarks>
        /// The compiler doesn't always annotate every time it generates with the
        /// CompilerGeneratedAttribute so sometimes we have to check if the type's
        /// identifier represents a generated type. Follows the same heuristics seen
        /// in https://github.com/dotnet/roslyn/blob/b57c1f89c1483da8704cde7b535a20fd029748db/src/ExpressionEvaluator/Core/Source/ResultProvider/Helpers/GeneratedMetadataNames.cs#L19
        /// </remarks>
        /// </summary>
        /// <param name="type">The type to evaluate. Can be null if evaluating only on name. </param>
        /// <param name="name">The identifier associated wit the type.</param>
        /// <returns><see langword="true" /> if <paramref name="type"/> is compiler generated
        /// or <paramref name="name"/> represents a compiler generated identifier.</returns>
        internal static bool IsCompilerGenerated(string name, Type? type = null)
        {
            return (type is Type  && Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute)))
                || name.StartsWith("<", StringComparison.Ordinal)
                || (name.IndexOf('$') >= 0);
        }
    }
}