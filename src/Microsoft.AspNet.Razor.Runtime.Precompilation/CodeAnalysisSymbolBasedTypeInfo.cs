// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.Runtime.Precompilation
{
    /// <summary>
    /// <see cref="ITypeInfo"/> implementation using Code Analysis symbols.
    /// </summary>
    [DebuggerDisplay("{Name}")]
    public class CodeAnalysisSymbolBasedTypeInfo : ITypeInfo
    {
        /// <summary>
        /// The <see cref="System.Reflection.TypeInfo"/> for <see cref="IDictionary{TKey, TValue}"/>.
        /// </summary>
        public static readonly System.Reflection.TypeInfo OpenGenericDictionaryTypeInfo =
            typeof(IDictionary<,>).GetTypeInfo();
        private readonly CodeAnalysisSymbolLookupCache _symbolLookup;
        private readonly ITypeSymbol _type;
        private readonly ITypeSymbol _underlyingType;
        private string _fullName;
        private List<IPropertyInfo> _properties;

        /// <summary>
        /// Initializes a new instance of <see cref="CodeAnalysisSymbolBasedTypeInfo"/>.
        /// </summary>
        /// <param name="propertySymbol">The <see cref="IPropertySymbol"/>.</param>
        /// <param name="symbolLookup">The <see cref="CodeAnalysisSymbolLookupCache"/>.</param>
        public CodeAnalysisSymbolBasedTypeInfo(
            [NotNull] ITypeSymbol type,
            [NotNull] CodeAnalysisSymbolLookupCache symbolLookup)
        {
            _symbolLookup = symbolLookup;
            _type = type;
            _underlyingType = UnwrapArrayType(type);
        }

        /// <inheritdoc />
        public string FullName
        {
            get
            {
                if (_fullName == null)
                {
                    _fullName = GetFullName(_type);
                }

                return _fullName;
            }
        }

        /// <summary>
        /// The <see cref="ITypeSymbol"/> instance.
        /// </summary>
        public ITypeSymbol TypeSymbol => _type;

        /// <inheritdoc />
        public bool IsAbstract => _type.IsAbstract;

        /// <inheritdoc />
        public bool IsGenericType
        {
            get
            {
                return _type.Kind == SymbolKind.NamedType &&
                    ((INamedTypeSymbol)_type).IsGenericType;
            }
        }

        /// <inheritdoc />
        public bool IsNested => _underlyingType.ContainingType != null;

        /// <inheritdoc />
        public bool IsPublic
        {
            get
            {
                return _type.DeclaredAccessibility == Accessibility.Public ||
                    _type.TypeKind == TypeKind.Array;
            }
        }

        /// <inheritdoc />
        public string Name
        {
            get
            {
                if (_type.TypeKind == TypeKind.Array)
                {
                    return _underlyingType.MetadataName + "[]";
                }

                return _type.MetadataName;
            }
        }

        /// <inheritdoc />
        public IEnumerable<IPropertyInfo> Properties
        {
            get
            {
                if (_properties == null)
                {
                    _properties = GetProperties(_type, _symbolLookup);
                }

                return _properties;
            }
        }

        /// <inheritdoc />
        public bool IsTagHelper
        {
            get
            {
                var interfaceSymbol = _symbolLookup.GetSymbol(typeof(ITagHelper).GetTypeInfo());
                return _type.AllInterfaces.Any(implementedInterface => implementedInterface == interfaceSymbol);
            }
        }

        /// <inheritdoc />
        public IEnumerable<TAttribute> GetCustomAttributes<TAttribute>()
            where TAttribute : Attribute
        {
            return CodeAnalysisAttributeUtilities.GetCustomAttributes<TAttribute>(_type, _symbolLookup);
        }

        /// <inheritdoc />
        public ITypeInfo[] GetGenericDictionaryParameters()
        {
            var dictionarySymbol = _symbolLookup.GetSymbol(OpenGenericDictionaryTypeInfo);

            INamedTypeSymbol dictionaryInterface;
            if (_type.Kind == SymbolKind.NamedType &&
                ((INamedTypeSymbol)_type).ConstructedFrom == dictionarySymbol)
            {
                dictionaryInterface = (INamedTypeSymbol)_type;
            }
            else
            {
                dictionaryInterface = _type
                    .AllInterfaces
                    .FirstOrDefault(implementedInterface => implementedInterface.ConstructedFrom == dictionarySymbol);
            }

            if (dictionaryInterface != null)
            {
                Debug.Assert(dictionaryInterface.TypeArguments.Length == 2);

                return new[]
                {
                    new CodeAnalysisSymbolBasedTypeInfo(dictionaryInterface.TypeArguments[0], _symbolLookup),
                    new CodeAnalysisSymbolBasedTypeInfo(dictionaryInterface.TypeArguments[1], _symbolLookup),
                };
            }

            return null;
        }

        /// <summary>
        /// Gets the assembly qualified named of the specified <paramref name="symbol"/>.
        /// </summary>
        /// <param name="symbol">The <see cref="ITypeSymbol" /> to generate the name for.</param>
        /// <returns>The assembly qualified name.</returns>
        public static string GetAssemblyQualifiedName([NotNull] ITypeSymbol symbol)
        {
            var builder = new StringBuilder();
            GetAssemblyQualifiedName(builder, symbol);

            return builder.ToString();
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return Equals(obj as ITypeInfo);
        }

        /// <inheritdoc />
        public bool Equals(ITypeInfo other)
        {
            if (other == null)
            {
                return false;
            }

            var otherSymbolBasedType = other as CodeAnalysisSymbolBasedTypeInfo;
            if (otherSymbolBasedType != null)
            {
                return otherSymbolBasedType.TypeSymbol == TypeSymbol;
            }

            return string.Equals(FullName, other.FullName, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override int GetHashCode() => FullName.GetHashCode();

        private static List<IPropertyInfo> GetProperties(
            ITypeSymbol typeSymbol,
            CodeAnalysisSymbolLookupCache symbolLookup)
        {
            var properties = new List<IPropertyInfo>();
            var overridenProperties = new HashSet<IPropertySymbol>();

            do
            {
                foreach (var member in typeSymbol.GetMembers().Where(m => m.Kind == SymbolKind.Property))
                {
                    var propertySymbol = (IPropertySymbol)member;
                    if (!propertySymbol.IsIndexer && !overridenProperties.Contains(propertySymbol))
                    {
                        var propertyInfo = new CodeAnalysisSymbolBasedPropertyInfo(propertySymbol, symbolLookup);
                        properties.Add(propertyInfo);
                    }

                    if (propertySymbol.IsOverride)
                    {
                        overridenProperties.Add(propertySymbol.OverriddenProperty);
                    }
                }

                typeSymbol = typeSymbol.BaseType;

            } while (typeSymbol != null);

            return properties;
        }

        private static string GetFullName(ITypeSymbol typeSymbol)
        {
            var nameBuilder = new StringBuilder();
            GetFullName(nameBuilder, typeSymbol);

            return nameBuilder.Length == 0 ? null : nameBuilder.ToString();
        }

        private static void GetFullName(StringBuilder nameBuilder, ITypeSymbol typeSymbol)
        {
            if (typeSymbol.Kind == SymbolKind.TypeParameter)
            {
                return;
            }

            var insertIndex = nameBuilder.Length;
            if (typeSymbol.TypeKind == TypeKind.Array)
            {
                var arrayType = (IArrayTypeSymbol)typeSymbol;
                GetFullName(nameBuilder, arrayType.ElementType);
                nameBuilder.Append("[]");
                return;
            }

            nameBuilder.Append(typeSymbol.MetadataName);
            if (typeSymbol.Kind == SymbolKind.NamedType)
            {
                var namedSymbol = (INamedTypeSymbol)typeSymbol;
                // The symbol represents a generic but not open generic type
                if (namedSymbol.IsGenericType &&
                    namedSymbol.ConstructedFrom != namedSymbol)
                {
                    nameBuilder.Append('[');
                    foreach (var typeArgument in namedSymbol.TypeArguments)
                    {
                        nameBuilder.Append('[');
                        GetFullName(nameBuilder, typeArgument);
                        nameBuilder.Append("],");
                    }

                    // Removing trailing slash
                    Debug.Assert(nameBuilder.Length > 0 && nameBuilder[nameBuilder.Length - 1] == ',');
                    nameBuilder.Length--;
                    nameBuilder.Append("]");
                }
            }

            var containingType = typeSymbol.ContainingType;
            while (containingType != null)
            {
                nameBuilder
                    .Insert(insertIndex, '+')
                    .Insert(insertIndex, containingType.MetadataName);

                containingType = containingType.ContainingType;
            }

            var containingNamespace = typeSymbol.ContainingNamespace;
            while (!containingNamespace.IsGlobalNamespace)
            {
                nameBuilder
                    .Insert(insertIndex, '.')
                    .Insert(insertIndex, containingNamespace.MetadataName);

                containingNamespace = containingNamespace.ContainingNamespace;
            }
        }

        private static void GetAssemblyQualifiedName(StringBuilder builder, ITypeSymbol typeSymbol)
        {
            GetFullName(builder, typeSymbol);
            typeSymbol = UnwrapArrayType(typeSymbol);

            builder
                .Append(", ")
                .Append(typeSymbol.ContainingAssembly.Identity);
        }

        private static ITypeSymbol UnwrapArrayType(ITypeSymbol type)
        {
            if (type.TypeKind == TypeKind.Array)
            {
                return ((IArrayTypeSymbol)type).ElementType;
            }

            return type;
        }
    }
}