// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc.Rendering
{
    /// <summary>
    /// Describes an <see cref="System.Linq.Expressions.Expression"/> passed to a tag helper.
    /// </summary>
    public sealed class ModelExpression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModelExpression"/> class.
        /// </summary>
        /// <param name="name">
        /// String representation of the <see cref="System.Linq.Expressions.Expression"/> of interest.
        /// </param>
        /// <param name="metadata">
        /// Metadata about the <see cref="System.Linq.Expressions.Expression"/> of interest.
        /// </param>
        public ModelExpression(string name, [NotNull] ModelMetadata metadata)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(name));
            }

            Name = name;
            Metadata = metadata;
        }

        /// <summary>
        /// String representation of the <see cref="System.Linq.Expressions.Expression"/> of interest.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Metadata about the <see cref="System.Linq.Expressions.Expression"/> of interest.
        /// </summary>
        /// <remarks>
        /// Getting <see cref="ModelMetadata.Model"/> will evaluate a compiled version of the original
        /// <see cref="System.Linq.Expressions.Expression"/>.
        /// </remarks>
        public ModelMetadata Metadata { get; private set; }
    }
}