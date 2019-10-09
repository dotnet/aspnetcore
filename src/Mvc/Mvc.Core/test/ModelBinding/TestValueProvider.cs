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
            : base(TestBindingSource, new RouteValueDictionary(values))
        {
        }

        public TestValueProvider(BindingSource bindingSource, IDictionary<string, object> values)
            : base(bindingSource, new RouteValueDictionary(values))
        {
        }
    }
}