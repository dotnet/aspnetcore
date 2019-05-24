// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public class FormValueProviderTest : EnumerableValueProviderTest
    {
        protected override IEnumerableValueProvider GetEnumerableValueProvider(
            BindingSource bindingSource,
            Dictionary<string, StringValues> values,
            CultureInfo culture)
        {
            var backingStore = new FormCollection(values);
            return new FormValueProvider(bindingSource, backingStore, culture);
        }
    }
}
