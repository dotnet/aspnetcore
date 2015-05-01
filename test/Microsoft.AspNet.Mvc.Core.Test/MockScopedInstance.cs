// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    public class MockScopedInstance<T> : IScopedInstance<T>
    {
        public T Value { get; set; }

        public void Dispose()
        {
        }
    }
}