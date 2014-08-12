// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Routing;

namespace Microsoft.AspNet.Mvc
{
    public class ActionInfo
    {
        public string ActionName { get; set; }

        public string[] HttpMethods { get; set; }

        public IRouteTemplateProvider AttributeRoute { get; set; }

        public object[] Attributes { get; set; }

        public bool RequireActionNameMatch { get; set; }
    }
}
