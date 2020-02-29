// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Filters
{
    internal readonly struct FilterCursorItem<TFilter, TFilterAsync>
    {
        public FilterCursorItem(TFilter filter, TFilterAsync filterAsync)
        {
            Filter = filter;
            FilterAsync = filterAsync;
        }

        public TFilter Filter { get; }

        public TFilterAsync FilterAsync { get; }
    }
}
