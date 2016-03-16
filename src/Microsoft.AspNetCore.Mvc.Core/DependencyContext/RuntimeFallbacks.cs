// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.DependencyModel
{
    internal class RuntimeFallbacks
    {
        public string Runtime { get; set; }
        public IReadOnlyList<string> Fallbacks { get; set; }

        public RuntimeFallbacks(string runtime, IEnumerable<string> fallbacks)
        {
            if (runtime == null)
            {
                throw new ArgumentNullException(nameof(runtime));
            }
            if (fallbacks == null)
            {
                throw new ArgumentNullException(nameof(fallbacks));
            }
            Runtime = runtime;
            Fallbacks = fallbacks.ToArray();
        }
    }
}