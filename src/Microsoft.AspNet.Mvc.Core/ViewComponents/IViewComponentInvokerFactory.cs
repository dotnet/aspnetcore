// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    public interface IViewComponentInvokerFactory
    {
        IViewComponentInvoker CreateInstance([NotNull] TypeInfo componentType, object[] args);
    }
}
