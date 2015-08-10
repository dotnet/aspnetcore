// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.Runtime
{
    /// <summary>
    /// <see cref="IPropertyInfo"/> adapter for <see cref="PropertyInfo"/> instances.
    /// </summary>
    public class RuntimePropertyInfo : IPropertyInfo
    {
        /// <summary>
        /// Initializes a new instance of <see cref="RuntimePropertyInfo"/>.
        /// </summary>
        /// <param name="propertyInfo">The <see cref="PropertyInfo"/> instance to adapt.</param>
        public RuntimePropertyInfo([NotNull] PropertyInfo propertyInfo)
        {
            Property = propertyInfo;
        }

        /// <summary>
        /// The <see cref="PropertyInfo"/> instance.
        /// </summary>
        public PropertyInfo Property { get; }

        /// <inheritdoc />
        public bool HasPublicGetter => Property.GetMethod != null && Property.GetMethod.IsPublic;

        /// <inheritdoc />
        public bool HasPublicSetter => Property.SetMethod != null && Property.SetMethod.IsPublic;

        /// <inheritdoc />
        public string Name => Property.Name;

        /// <inheritdoc />
        public ITypeInfo PropertyType => new RuntimeTypeInfo(Property.PropertyType.GetTypeInfo());

        /// <inheritdoc />
        public IEnumerable<TAttribute> GetCustomAttributes<TAttribute>() where TAttribute : Attribute
            => Property.GetCustomAttributes<TAttribute>(inherit: false);

        /// <inheritdoc />
        public override string ToString() =>
            Property.ToString();
    }
}
