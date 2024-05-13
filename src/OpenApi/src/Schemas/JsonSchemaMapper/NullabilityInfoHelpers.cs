// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;

namespace System.Reflection
{
    /// <summary>
    /// Polyfills for System.Private.CoreLib internals.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal static class NullabilityInfoHelpers
    {
        public static MemberInfo GetMemberWithSameMetadataDefinitionAs(Type type, MemberInfo member)
        {
            const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            foreach (var info in type.GetMembers(all))
            {
                if (info.HasSameMetadataDefinitionAs(member))
                {
                    return info;
                }
            }

            throw new MissingMemberException(type.FullName, member.Name);
        }

        // https://github.com/dotnet/runtime/blob/main/src/coreclr/System.Private.CoreLib/src/System/Reflection/MemberInfo.Internal.cs
        public static bool HasSameMetadataDefinitionAs(this MemberInfo target, MemberInfo other)
        {
            return target.MetadataToken == other.MetadataToken &&
                   target.Module.Equals(other.Module);
        }

        // https://github.com/dotnet/runtime/issues/23493
        public static bool IsGenericMethodParameter(this Type target)
        {
            return target.IsGenericParameter &&
                   target.DeclaringMethod != null;
        }
    }
}
#endif