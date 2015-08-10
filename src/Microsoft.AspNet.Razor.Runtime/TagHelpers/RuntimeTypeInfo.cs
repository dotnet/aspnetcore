// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// <see cref="ITypeInfo"/> adapter for <see cref="System.Reflection.TypeInfo"/> instances.
    /// </summary>
    public class RuntimeTypeInfo : ITypeInfo
    {
        private static readonly TypeInfo TagHelperTypeInfo = typeof(ITagHelper).GetTypeInfo();
        private IEnumerable<IPropertyInfo> _properties;

        /// <summary>
        /// Initializes a new instance of <see cref="RuntimeTypeInfo"/>
        /// </summary>
        /// <param name="propertyInfo">The <see cref="System.Reflection.TypeInfo"/> instance to adapt.</param>
        public RuntimeTypeInfo([NotNull] TypeInfo typeInfo)
        {
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
        public bool IsTagHelper => TagHelperTypeInfo.IsAssignableFrom(TypeInfo);

        /// <inheritdoc />
        public IEnumerable<TAttribute> GetCustomAttributes<TAttribute>() where TAttribute : Attribute =>
            TypeInfo.GetCustomAttributes<TAttribute>(inherit: false);

        /// <inheritdoc />
        public string[] GetGenericDictionaryParameterNames()
        {
            return ClosedGenericMatcher.ExtractGenericInterface(
                    TypeInfo.AsType(),
                    typeof(IDictionary<,>))
                ?.GenericTypeArguments
                .Select(type => type.FullName)
                .ToArray();
        }

        /// <inheritdoc />
        public override string ToString() => TypeInfo.ToString();
    }
}