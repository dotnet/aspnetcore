// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class HandlerParameterDescriptor : ParameterDescriptor, IParameterInfoParameterDescriptor
    {
        /// <summary>
        /// Gets or sets the <see cref="System.Reflection.ParameterInfo"/>.
        /// </summary>
        public ParameterInfo ParameterInfo { get; set; }
    }
}
