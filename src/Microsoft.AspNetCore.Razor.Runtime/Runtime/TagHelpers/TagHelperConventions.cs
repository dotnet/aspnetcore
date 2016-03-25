// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Default convention for determining if a type is a tag helper.
    /// </summary>
    public static class TagHelperConventions
    {
        private static readonly TypeInfo ITagHelperTypeInfo = typeof(ITagHelper).GetTypeInfo();

        /// <summary>
        /// Indicates whether or not the <see cref="TypeInfo"/> is a tag helper.
        /// </summary>
        /// <param name="typeInfo">The <see cref="TypeInfo"/>.</param>
        /// <returns>true if <paramref name="typeInfo"/> is a tag helper; false otherwise.</returns>
        public static bool IsTagHelper(TypeInfo typeInfo)
        {
            return !typeInfo.IsNested &&
                typeInfo.IsPublic &&
                !typeInfo.IsAbstract &&
                !typeInfo.IsGenericType &&
                ITagHelperTypeInfo.IsAssignableFrom(typeInfo);
        }
    }
}
