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
        private string _sanitizedFullName;

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
        public string FullName => TypeInfo.FullName;

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
        public bool ImplementsInterface(ITypeInfo interfaceTypeInfo)
        {
            if (interfaceTypeInfo == null)
            {
                throw new ArgumentNullException(nameof(interfaceTypeInfo));
            }

            var runtimeTypeInfo = interfaceTypeInfo as RuntimeTypeInfo;
            if (runtimeTypeInfo == null)
            {
                throw new ArgumentException(
                    Resources.FormatArgumentMustBeAnInstanceOf(typeof(RuntimeTypeInfo).FullName),
                    nameof(interfaceTypeInfo));
            }

            return runtimeTypeInfo.TypeInfo.IsInterface && runtimeTypeInfo.TypeInfo.IsAssignableFrom(TypeInfo);
        }

        private string SanitizedFullName
        {
            get
            {
                if (_sanitizedFullName == null)
                {
                    _sanitizedFullName = SanitizeFullName(FullName);
                }

                return _sanitizedFullName;
            }
        }

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

            return string.Equals(
                SanitizedFullName,
                SanitizeFullName(other.FullName),
                StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override int GetHashCode() => SanitizedFullName.GetHashCode();

        /// <summary>
        /// Removes assembly qualification from generic type parameters for the specified <paramref name="fullName"/>.
        /// </summary>
        /// <param name="fullName">Full name.</param>
        /// <returns>Full name without fully qualified generic parameters.</returns>
        /// <example>
        /// <c>typeof(<see cref="List{string}"/>).FullName</c> is
        /// List`1[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]
        /// <c>Sanitize(typeof(<see cref="List{string}"/>.FullName</c> returns
        /// List`1[[System.String]
        /// </example>
        public static string SanitizeFullName(string fullName)
        {
            // In CoreCLR, some types (such as System.String) are type forwarded from System.Runtime
            // to mscorlib at runtime. Type names of generic type parameters includes the assembly qualified name;
            // consequently the type name generated at precompilation differs from the one at runtime. We'll
            // avoid dealing with these inconsistencies by removing assembly information from TypeInfo.FullName.
            return _fullNameSanitizer.Replace(fullName, string.Empty);
        }
    }
}