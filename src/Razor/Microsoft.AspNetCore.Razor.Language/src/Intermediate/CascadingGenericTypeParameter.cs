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
        /// Gets or sets the type of the value expression, e.g., IDictionary[TOther, TItem]
        /// </summary>
        public string ValueExpressionType { get; set; }

        /// <summary>
        /// Gets or sets the value expression
        /// </summary>
        public string ValueExpression { get; set; }
    }
}
