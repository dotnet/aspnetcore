// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace ModelBindingWebSite
{
    public class FromTestAttribute : Attribute, IBindingSourceMetadata
    {
        public static readonly BindingSource TestBindingSource = new BindingSource(
            "Test",
            displayName: null,
            isGreedy: true,
            isFromRequest: true);

        public BindingSource BindingSource { get { return TestBindingSource; } }

        public object Value { get; set; }
    }
}