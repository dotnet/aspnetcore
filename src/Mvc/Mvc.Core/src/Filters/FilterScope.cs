// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Filters
{
    /// <summary>
    /// <para>
    /// Contains constant values for known filter scopes.
    /// </para>
    /// <para>
    /// Scope defines the ordering of filters that have the same order. Scope is by-default
    /// defined by how a filter is registered.
    /// </para>
    /// </summary>
    public static class FilterScope
    {
        public static readonly int First = 0;
        public static readonly int Global = 10;
        public static readonly int Controller = 20;
        public static readonly int Action = 30;
        public static readonly int Last = 100;
    }
}
