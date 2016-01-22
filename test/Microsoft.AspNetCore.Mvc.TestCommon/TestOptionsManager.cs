// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc
{
    public class TestOptionsManager<T> : OptionsManager<T>
        where T : class, new()
    {
        public TestOptionsManager()
            : base(Enumerable.Empty<IConfigureOptions<T>>())
        {
        }
    }
}
