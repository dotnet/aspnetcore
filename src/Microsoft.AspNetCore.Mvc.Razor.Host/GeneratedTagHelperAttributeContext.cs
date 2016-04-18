// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Razor
{
    /// <summary>
    /// Contains information for the <see cref="AspNetCore.Razor.TagHelpers.ITagHelper"/> attribute code
    /// generation process.
    /// </summary>
    public class GeneratedTagHelperAttributeContext
    {
        /// <summary>
        /// Name of the model expression type.
        /// </summary>
        public string ModelExpressionTypeName { get; set; }

        /// <summary>
        /// Name the method to create <c>ModelExpression</c>s.
        /// </summary>
        public string CreateModelExpressionMethodName { get; set; }

        /// <summary>
        /// Gets or sets the name of the <c>IModelExpressionProvider</c>.
        /// </summary>
        public string ModelExpressionProviderPropertyName { get; set; }

        /// <summary>
        /// Gets or sets the property name of the <c>ViewDataDictionary</c>.
        /// </summary>
        public string ViewDataPropertyName { get; set; }
    }
}