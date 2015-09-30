// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Server.Kestrel.Filter
{
    public class NoOpConnectionFilter : IConnectionFilter
    {
        private static Task _empty = Task.FromResult<object>(null);

        public Task OnConnection(ConnectionFilterContext context)
        {
            return _empty;
        }
    }
}
