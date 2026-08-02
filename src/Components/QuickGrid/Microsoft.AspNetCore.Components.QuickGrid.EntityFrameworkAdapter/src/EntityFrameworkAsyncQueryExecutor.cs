// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Microsoft.AspNetCore.Components.QuickGrid.EntityFrameworkAdapter;

internal sealed class EntityFrameworkAsyncQueryExecutor : IAsyncQueryExecutor
{
    public bool IsSupported<T>(IQueryable<T> queryable)
        => queryable.Provider is IAsyncQueryProvider;

    public Task<int> CountAsync<T>(IQueryable<T> queryable)
        => queryable.CountAsync();

    public Task<T[]> ToArrayAsync<T>(IQueryable<T> queryable)
        => queryable.ToArrayAsync();
}
