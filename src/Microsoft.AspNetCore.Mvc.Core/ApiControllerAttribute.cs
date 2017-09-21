// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Internal;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Indicates that a type and all derived types are used to serve HTTP API responses. The presense of
    /// this attribute can be used to target conventions, filters and other behaviors based on the purpose
    /// of the controller.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ApiControllerAttribute : ControllerAttribute, IApiBehaviorMetadata
    {
    }
}
