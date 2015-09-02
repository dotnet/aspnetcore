// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;

namespace Microsoft.AspNet.Mvc.Actions
{
    /// <summary>
    /// A descriptor for method parameters of an action method.
    /// </summary>
    public class ControllerParameterDescriptor : ParameterDescriptor
    {
        /// <summary>
        /// Gets or sets the <see cref="ParameterInfo"/>.
        /// </summary>
        public ParameterInfo ParameterInfo { get; set; }
    }
}
