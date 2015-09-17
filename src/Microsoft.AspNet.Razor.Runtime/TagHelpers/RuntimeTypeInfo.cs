// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// <see cref="ITypeInfo"/> adapter for <see cref="System.Reflection.TypeInfo"/> instances.
    /// </summary>
    public class RuntimeTypeInfo : ITypeInfo
    {
        private static readonly Regex _fullNameSanitizer = new Regex(
            @", [A-Za-z\.]+, Version=\d+\.\d+\.\d+\.\d+, Culture=neutral, PublicKeyToken=\w+",
            RegexOptions.ExplicitCapture,
            Constants.RegexMatchTimeout);

        private static readonly TypeInfo TagHelperTypeInfo = typeof(ITagHelper).GetTypeInfo();
        private IEnumerable<IPropertyInfo> _properties;

        /// <summary>
        /// Initializes a new instance of <see cref="RuntimeTypeInfo"/>
        /// </summary>
        /// <param name="propertyInfo">The <see cref="System.Reflection.TypeInfo"/> instance to adapt.</param>
        public RuntimeTypeInfo(TypeInfo typeInfo)
        {
            if (typeInfo == null)
            {
                throw new ArgumentNullException(nameof(typeInfo));
            }

            TypeInfo = typeInfo;
        }

        /// <summary>
        /// The <see cref="System.Reflection.TypeInfo"/> instance.
        /// </summary>
        public TypeInfo TypeInfo { get; }

        /// <inheritdoc />
        public string Name => TypeInfo.Name;

        /// <inheritdoc />
        public string FullName => SanitizeFullName(TypeInfo.FullName);

        /// <inheritdoc />
        public bool IsAbstract => TypeInfo.IsAbstract;

        /// <inheritdoc />
        public bool IsGenericType => TypeInfo.IsGenericType;

        /// <inheritdoc />
        public bool IsPublic => TypeInfo.IsPublic;

        /// <inheritdoc />
        public IEnumerable<IPropertyInfo> Properties
        {
            get
            {
                if (_properties == null)
                {
                    _properties = TypeInfo
                        .AsType()
                        .GetRuntimeProperties()
                        .Where(property => property.GetIndexParameters().Length == 0)
                        .Select(property => new RuntimePropertyInfo(property));
                }

                return _properties;
            }
        }

        /// <inheritdoc />
        public bool IsTagHelper => TagHelperTypeInfo.IsAssignableFrom(TypeInfo);

        /// <inheritdoc />
        public IEnumerable<TAttribute> GetCustomAttributes<TAttribute>() where TAttribute : Attribute =>
            TypeInfo.GetCustomAttributes<TAttribute>(inherit: false);

        /// <inheritdoc />
        public ITypeInfo[] GetGenericDictionaryParameters()
        {
            return ClosedGenericMatcher.ExtractGenericInterface(
                    TypeInfo.AsType(),
                    typeof(IDictionary<,>))
                ?.GenericTypeArguments
                .Select(type => type.IsGenericParameter ? null : new RuntimeTypeInfo(type.GetTypeInfo()))
                .ToArray();
        }

        /// <inheritdoc />
        public override string ToString() => TypeInfo.ToString();

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

            var otherRuntimeType = other as RuntimeTypeInfo;
            if (otherRuntimeType != null)
            {
                return otherRuntimeType.TypeInfo == TypeInfo;
            }

            return string.Equals(FullName, other.FullName, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override int GetHashCode() => FullName.GetHashCode();

        // Internal for unit testing
        internal static string SanitizeFullName(string fullName)
        {
            // In CoreCLR, some types (such as System.String) are type forwarded from System.Runtime
            // to mscorlib at runtime. Type names of generic type parameters includes the assembly qualified name;
            // consequently the type name generated at precompilation differs from the one at runtime. We'll
            // avoid dealing with these inconsistencies by removing assembly information from TypeInfo.FullName.
            return _fullNameSanitizer.Replace(fullName, string.Empty);
        }
    }
}