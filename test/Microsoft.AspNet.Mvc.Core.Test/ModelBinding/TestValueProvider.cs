// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class TestValueProvider : DictionaryBasedValueProvider
    {
        public static readonly BindingSource TestBindingSource = new BindingSource(
            id: "Test",
            displayName: "Test",
            isGreedy: false,
            isFromRequest: true);

        public TestValueProvider(IDictionary<string, object> values)
            : base(TestBindingSource, values)
        {
        }

        public TestValueProvider(BindingSource bindingSource, IDictionary<string, object> values)
            : base(bindingSource, values)
        {
        }
    }
}