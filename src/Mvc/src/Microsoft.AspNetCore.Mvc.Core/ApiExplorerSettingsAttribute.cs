// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Controls the visibility and group name for an <c>ApiDescription</c>
    /// of the associated controller class or action method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ApiExplorerSettingsAttribute :
        Attribute,
        IApiDescriptionGroupNameProvider,
        IApiDescriptionVisibilityProvider
    {
        /// <inheritdoc />
        public string GroupName { get; set; }

        /// <inheritdoc />
        public bool IgnoreApi { get; set; }
    }
}