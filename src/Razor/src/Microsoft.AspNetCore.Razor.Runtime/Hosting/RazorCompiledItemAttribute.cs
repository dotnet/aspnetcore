// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Hosting
{
    /// <summary>
    /// Specifies that an assembly contains a compiled Razor asset.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class RazorCompiledItemAttribute : Attribute
    {
        /// <summary>
        /// Creates a new <see cref="RazorCompiledItemAttribute"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of the compiled item.</param>
        /// <param name="kind">
        /// The kind of the compiled item. The kind is used programmatically to associate behaviors with the item.
        /// </param>
        /// <param name="identifier">
        /// The identifier associated with the item. The identifier is used programmatically to locate
        /// a specific item of a specific kind, and should be unique within the assembly.
        /// </param>
        public RazorCompiledItemAttribute(Type type, string kind, string identifier)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (kind == null)
            {
                throw new ArgumentNullException(nameof(kind));
            }

            Type = type;
            Kind = kind;
            Identifier = identifier;
        }

        /// <summary>
        /// Gets the kind of compiled item. The kind is used programmatically to associate behaviors and semantics
        /// with the item.
        /// </summary>
        public string Kind { get; }

        /// <summary>
        /// Gets the identifier associated with the compiled item. The identifier is used programmatically to locate
        /// a specific item of a specific kind and should be uniqure within the assembly.
        /// </summary>
        public string Identifier { get; }

        /// <summary>
        /// Gets the <see cref="Type"/> of the compiled item. The type should be contained in the assembly associated
        /// with this instance of <see cref="RazorCompiledItemAttribute"/>.
        /// </summary>
        public Type Type { get; }
    }
}
