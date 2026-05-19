// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// A one-way cursor for filters.
/// </summary>
/// <remarks>
/// This will iterate the filter collection once per-stage, and skip any filters that don't have
/// the one of interfaces that applies to the current stage.
///
/// Filters are always executed in the following order, but short circuiting plays a role.
///
/// Indentation reflects nesting.
///
/// 1. Exception Filters
///     2. Authorization Filters
///     3. Action Filters
///        Action
///
/// 4. Result Filters
///    Result
///
/// </remarks>
internal struct FilterCursor
{
    private readonly IFilterMetadata[] _filters;
    private int _index;

    public FilterCursor(IFilterMetadata[] filters)
    {
        _filters = filters;
        _index = 0;
    }

    public void Reset()
    {
        _index = 0;
    }

    public FilterCursorItem<TFilter?, TFilterAsync?> GetNextFilter<TFilter, TFilterAsync>()
        where TFilter : class
        where TFilterAsync : class
    {
        while (_index < _filters.Length)
        {
            var filter = _filters[_index] as TFilter;
            var filterAsync = _filters[_index] as TFilterAsync;

            _index += 1;

            if (filter != null || filterAsync != null)
            {
                return new FilterCursorItem<TFilter?, TFilterAsync?>(filter, filterAsync);
            }
        }

        return default(FilterCursorItem<TFilter?, TFilterAsync?>);
    }
}
