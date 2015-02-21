// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.OptionDescriptors
{
    /// <summary>
    /// Encapsulates information that describes an <see cref="IValueProviderFactory"/>.
    /// </summary>
    public class ValueProviderFactoryDescriptor : OptionDescriptor<IValueProviderFactory>
    {
        /// <summary>
        /// Creates a new instance of <see cref="ValueProviderFactoryDescriptor"/>.
        /// </summary>
        /// <param name="type">The <see cref="IValueProviderFactory"/> type that the descriptor represents.</param>
        public ValueProviderFactoryDescriptor([NotNull] Type type)
            : base(type)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="ValueProviderFactoryDescriptor"/> using the specified type.
        /// </summary>
        /// <param name="valueProviderFactory">An instance of <see cref="IValueProviderFactory"/>
        /// that the descriptor represents.</param>
        public ValueProviderFactoryDescriptor([NotNull] IValueProviderFactory valueProviderFactory)
            : base(valueProviderFactory)
        {
        }
    }
}