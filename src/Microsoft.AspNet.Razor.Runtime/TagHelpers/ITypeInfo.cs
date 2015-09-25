// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Contains type metadata.
    /// </summary>
    public interface ITypeInfo : IMemberInfo, IEquatable<ITypeInfo>
    {
        /// <summary>
        /// Fully qualified name of the type.
        /// </summary>
        /// <remarks>
        /// On CoreCLR, some BCL types get type forwarded to the full desktop framework implementations at
        /// runtime. For e.g. we compile against System.String in System.Runtime which is type forwarded to
        /// mscorlib at runtime. Consequently for generic types where the <see cref="FullName"/> includes the assembly
        /// qualified name of generic parameters, FullNames would not match.
        /// Use <see cref="IEquatable{ITypeInfo}.Equals(ITypeInfo)"/> to compare <see cref="ITypeInfo"/>s instead.
        /// </remarks>
        string FullName { get; }

        /// <summary>
        /// Gets <see cref="IPropertyInfo"/>s for all properties of the current type excluding indexers.
        /// </summary>
        /// <remarks>
        /// Indexers in this context refer to the CLR notion of an indexer (<c>this [string name]</c> 
        /// and does not overlap with the semantics of 
        /// <see cref="Razor.TagHelpers.TagHelperAttributeDescriptor.IsIndexer"/>.
        /// </remarks>
        IEnumerable<IPropertyInfo> Properties { get; }

        /// <summary>
        /// Gets a value indicating whether the type is public.
        /// </summary>
        bool IsPublic { get; }

        /// <summary>
        /// Gets a value indicating whether the type is abstract or an interface.
        /// </summary>
        bool IsAbstract { get; }

        /// <summary>
        /// Gets a value indicating whether the type is generic.
        /// </summary>
        bool IsGenericType { get; }

        /// <summary>
        /// Gets a value indicating whether the type implements the <param name="interfaceTypeInfo"/> interface.
        /// </summary>
        bool ImplementsInterface(ITypeInfo interfaceTypeInfo);

        /// <summary>
        /// Gets the <see cref="ITypeInfo[]"/> for the <c>TKey</c> and <c>TValue</c> parameters of
        /// <see cref="IDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <returns>
        /// The <see cref="ITypeInfo"/> of <c>TKey</c> and <c>TValue</c>
        /// parameters if the type implements <see cref="IDictionary{TKey, TValue}"/>, otherwise <c>null</c>.
        /// </returns>
        /// <remarks>
        /// For open generic types, <see cref="ITypeInfo" /> for generic type parameters is <c>null</c>. 
        /// </remarks>
        ITypeInfo[] GetGenericDictionaryParameters();
    }
}