// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public class TestValueProvider : RouteValueProvider
    {
        public static readonly BindingSource TestBindingSource = new BindingSource(
            id: "Test",
            displayName: "Test",
            isGreedy: false,
            isFromRequest: true);

        public TestValueProvider(IDictionary<string, object> values)
#pragma warning disable CA1304 // Specify CultureInfo
            : base(TestBindingSource, new RouteValueDictionary(values))
#pragma warning restore CA1304 // Specify CultureInfo
        {
        }

        public TestValueProvider(BindingSource bindingSource, IDictionary<string, object> values)
#pragma warning disable CA1304 // Specify CultureInfo
            : base(bindingSource, new RouteValueDictionary(values))
#pragma warning restore CA1304 // Specify CultureInfo
        {
        }
    }
}
