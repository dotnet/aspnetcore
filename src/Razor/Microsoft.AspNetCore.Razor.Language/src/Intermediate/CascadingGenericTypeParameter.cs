// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public sealed class CascadingGenericTypeParameter
    {
        /// <summary>
        /// Gets or sets the type parameter name, e.g., TItem
        /// </summary>
        public string GenericTypeName { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="ComponentAttributeIntermediateNode"/> that supplies content for
        /// <see cref="ValueExpression"/>. In the case of explicitly-specified generic parameters, this
        /// will be null.
        /// </summary>
        public ComponentAttributeIntermediateNode ValueSourceNode { get; set; }

        /// <summary>
        /// Gets or sets the type of <see cref="ValueExpression"/>, e.g., List[TItem].
        /// </summary>
        public string ValueType { get; set; }

        /// <summary>
        /// Gets or sets an expression defining the type of the generic parameter. In the case of inferred
        /// generic parameters, this will only be populated once a variable is emitted corresponding to
        /// <see cref="ValueSourceNode"/>.
        /// </summary>
        public string ValueExpression { get; set; }
    }
}
