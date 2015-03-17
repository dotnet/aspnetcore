// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.OptionDescriptors
{
    /// <summary>
    /// Encapsulates information that describes an <see cref="IExcludeTypeValidationFilter"/>.
    /// </summary>
    public class ExcludeValidationDescriptor : OptionDescriptor<IExcludeTypeValidationFilter>
    {
        /// <summary>
        /// Creates a new instance of <see cref="ExcludeValidationDescriptor"/>.
        /// </summary>
        /// <param name="type">
        /// A <see cref="IExcludeTypeValidationFilter"/> type that the descriptor represents.
        /// </param>
        public ExcludeValidationDescriptor([NotNull] Type type)
            : base(type)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="ExcludeValidationDescriptor"/>.
        /// </summary>
        /// <param name="validationFilter">An instance of <see cref="IExcludeTypeValidationFilter"/>
        /// that the descriptor represents.</param>
        public ExcludeValidationDescriptor([NotNull] IExcludeTypeValidationFilter validationFilter)
            : base(validationFilter)
        {
        }
    }
}