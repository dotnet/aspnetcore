// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.OptionDescriptors
{
    /// <summary>
    /// Encapsulates information that describes an <see cref="IModelValidatorProvider"/>.
    /// </summary>
    public class ModelValidatorProviderDescriptor : OptionDescriptor<IModelValidatorProvider>
    {
        /// <summary>
        /// Creates a new instance of <see cref="ModelValidatorProviderDescriptor"/>.
        /// </summary>
        /// <param name="type">A type that represents a <see cref="IModelValidatorProvider"/>.</param>
        public ModelValidatorProviderDescriptor([NotNull] Type type)
            : base(type)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="ModelValidatorProviderDescriptor"/> with the specified instance.
        /// </summary>
        /// <param name="option">An instance of <see cref="IModelValidatorProvider"/>.</param>
        public ModelValidatorProviderDescriptor([NotNull] IModelValidatorProvider validatorProvider)
            : base(validatorProvider)
        {
        }
    }
}