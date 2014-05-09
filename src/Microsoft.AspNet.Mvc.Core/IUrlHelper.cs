// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc
{
    public interface IUrlHelper
    {
        string Action(string action, string controller, object values, string protocol, string host, string fragment);

        string Content(string contentPath);

        bool IsLocalUrl(string url);
        
        string RouteUrl(string routeName, object values, string protocol, string host, string fragment);
    }
}
