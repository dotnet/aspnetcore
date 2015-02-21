// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    public class ViewComponentInvokerProviderContext
    {
        public ViewComponentInvokerProviderContext([NotNull] TypeInfo componentType, object[] arguments)
        {
            ComponentType = componentType;
            Arguments = arguments;
        }

        public object[] Arguments { get; private set; }

        public TypeInfo ComponentType { get; private set; }

        public IViewComponentInvoker Result { get; set; }
    }
}
