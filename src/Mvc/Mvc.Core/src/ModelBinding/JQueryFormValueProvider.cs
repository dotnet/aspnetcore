// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// An <see cref="IValueProvider"/> for jQuery formatted form data.
    /// </summary>
    public class JQueryFormValueProvider : JQueryValueProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JQueryFormValueProvider"/> class.
        /// </summary>
        /// <param name="bindingSource">The <see cref="BindingSource"/> of the data.</param>
        /// <param name="values">The values.</param>
        /// <param name="culture">The culture to return with ValueProviderResult instances.</param>
        public JQueryFormValueProvider(
            BindingSource bindingSource,
            IDictionary<string, StringValues> values,
            CultureInfo? culture)
            : base(bindingSource, values, culture)
        {
        }
    }
}
