// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Routing
{
    public interface IRouteCollection : IRouter
    {
        IRouter DefaultHandler { get; set; }

        void Add(IRouter router);
    }
}
