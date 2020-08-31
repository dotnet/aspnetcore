// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    internal class OrderedEndpointsSequenceProvider
    {
        private object Lock = new object();

        // In traditional conventional routing setup, the routes defined by a user have a order
        // defined by how they are added into the list. We would like to maintain the same order when building
        // up the endpoints too.
        //
        // Start with an order of '1' for conventional routes as attribute routes have a default order of '0'.
        // This is for scenarios dealing with migrating existing Router based code to Endpoint Routing world.
        private int _current = 1;

        public int GetNext()
        {
            lock (Lock)
            {
                return _current++;
            }
        }
    }
}
