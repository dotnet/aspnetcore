// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Components.Test.Helpers
{
    public class TestServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, Func<object>> _factories
            = new Dictionary<Type, Func<object>>();

        public object GetService(Type serviceType)
            => _factories.TryGetValue(serviceType, out var factory)
                ? factory()
                : null;

        internal void AddService<T>(T value)
            => _factories.Add(typeof(T), () => value);
    }
}
