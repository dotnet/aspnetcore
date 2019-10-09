// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Core;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    /// <summary>
    /// Provides a <see cref="ApplicationPartFactory"/> type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class ProvideApplicationPartFactoryAttribute : Attribute
    {
        private readonly Type _applicationPartFactoryType;
        private readonly string _applicationPartFactoryTypeName;

        /// <summary>
        /// Creates a new instance of <see cref="ProvideApplicationPartFactoryAttribute"/> with the specified type.
        /// </summary>
        /// <param name="factoryType">The factory type.</param>
        public ProvideApplicationPartFactoryAttribute(Type factoryType)
        {
            _applicationPartFactoryType = factoryType ?? throw new ArgumentNullException(nameof(factoryType));
        }

        /// <summary>
        /// Creates a new instance of <see cref="ProvideApplicationPartFactoryAttribute"/> with the specified type name.
        /// </summary>
        /// <param name="factoryTypeName">The assembly qualified type name.</param>
        public ProvideApplicationPartFactoryAttribute(string factoryTypeName)
        {
            if (string.IsNullOrEmpty(factoryTypeName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(factoryTypeName));
            }

            _applicationPartFactoryTypeName = factoryTypeName;
        }

        /// <summary>
        /// Gets the factory type.
        /// </summary>
        /// <returns></returns>
        public Type GetFactoryType()
        {
            return _applicationPartFactoryType ??
                Type.GetType(_applicationPartFactoryTypeName, throwOnError: true);
        }
    }
}
