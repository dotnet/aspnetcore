// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Mvc.Filters;

namespace Microsoft.AspNet.Mvc
{
    public class TestGlobalFilterProvider : IGlobalFilterProvider
    {
        public TestGlobalFilterProvider()
            : this(null)
        {
        }

        public TestGlobalFilterProvider(IEnumerable<IFilter> filters)
        {
            var filterList = new List<IFilter>();
            Filters = filterList;

            if (filters != null)
            {
                filterList.AddRange(filters);
            }
        }

        public IReadOnlyList<IFilter> Filters { get; private set; }
    }
}