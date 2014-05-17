// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Routing
{
    public interface IRouteCollection : IRouter
    {
        IRouter DefaultHandler { get; set; }

        IInlineConstraintResolver InlineConstraintResolver { get; set; }

        void Add(IRouter router);
    }
}
