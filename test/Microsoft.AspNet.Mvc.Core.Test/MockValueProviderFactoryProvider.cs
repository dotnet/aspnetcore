// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class MockValueProviderFactoryProvider : IValueProviderFactoryProvider
    {
        public List<IValueProviderFactory> ValueProviderFactories { get; } = new List<IValueProviderFactory>();

        IReadOnlyList<IValueProviderFactory> IValueProviderFactoryProvider.ValueProviderFactories
        {
            get { return ValueProviderFactories; }
        }
    }
}