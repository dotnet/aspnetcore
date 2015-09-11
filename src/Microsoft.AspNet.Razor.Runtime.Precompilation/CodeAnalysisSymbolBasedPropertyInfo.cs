// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.Runtime.Precompilation
{
    /// <summary>
    /// <see cref="IPropertyInfo"/> implementation using Code Analysis symbols.
    /// </summary>
    [DebuggerDisplay("{Name, PropertyType}")]
    public class CodeAnalysisSymbolBasedPropertyInfo : IPropertyInfo
    {
        private readonly IPropertySymbol _propertySymbol;

        /// <summary>
        /// Initializes a new instance of <see cref="CodeAnalysisSymbolBasedPropertyInfo"/>.
        /// </summary>
        /// <param name="propertySymbol">The <see cref="IPropertySymbol"/>.</param>
        public CodeAnalysisSymbolBasedPropertyInfo([NotNull] IPropertySymbol propertySymbol)
        {
            _propertySymbol = propertySymbol;
            PropertyType = new CodeAnalysisSymbolBasedTypeInfo(_propertySymbol.Type);
        }

        /// <inheritdoc />
        public bool HasPublicGetter
        {
            get
            {
                return _propertySymbol.GetMethod != null &&
                    _propertySymbol.GetMethod.DeclaredAccessibility == Accessibility.Public;
            }
        }

        /// <inheritdoc />
        public bool HasPublicSetter
        {
            get
            {
                return _propertySymbol.SetMethod != null &&
                    _propertySymbol.SetMethod.DeclaredAccessibility == Accessibility.Public;
            }
        }

        /// <inheritdoc />
        public string Name => _propertySymbol.MetadataName;

        /// <inheritdoc />
        public ITypeInfo PropertyType { get; }

        /// <inheritdoc />
        public IEnumerable<TAttribute> GetCustomAttributes<TAttribute>()
            where TAttribute : Attribute
        {
            return CodeAnalysisAttributeUtilities.GetCustomAttributes<TAttribute>(_propertySymbol);
        }
    }
}
