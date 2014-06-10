// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Specifies that a property or parameter value should be initialized via the dependency injection
    /// container for activated types.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class ActivateAttribute : Attribute
    {
    }
}